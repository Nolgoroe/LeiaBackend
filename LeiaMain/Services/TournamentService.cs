using System.Diagnostics;
using System.Text.Json;
using System.Timers;
using DataObjects;
using Microsoft.EntityFrameworkCore;
using DAL;
using Microsoft.Extensions.DependencyInjection;
using Azure.Core;

namespace Services
{
    public interface ITournamentService
    {
        public event EventHandler PlayerAddedToTournament;
        public Dictionary<Guid?, int?[]> PlayersSeeds { get; set; }
        //public Task CheckTournamentStatus(ISuikaDbService dbService, int tournamentId, PlayerTournamentSession playerTournamentSession);
        public Task CheckTournamentStatus(List<PlayerTournamentSession> sortedDataForFinalTournamentCalc, TournamentType tournamentType, ISuikaDbService dbService, int tournamentId, Player callingPlaeyr);
        public Task<PlayerCurrencies?> ChargePlayer(Guid playerId, int? tournamentId);
    }

    public class TournamentService : ITournamentService
    {
        private const int MAX_RATING_DRIFT = 300; 
        private const int MATCH_MAKER_INTERVAL = 500; // get these numbers from tournament DB or config file
        private readonly IPostTournamentService _postTournamentService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SemaphoreSlim _createMatchesFromQueueSemaphore = new SemaphoreSlim(1, 1);
        //private readonly IConfiguration _configuration;
        private JsonSerializerOptions _jsonOptions;
        public System.Timers.Timer MatchTimer { get; set; }
        private bool _isCreatingMatchesAllowed = true;
        public event EventHandler PlayerAddedToTournament;
        public Random TournamentSeedRandom = new Random();

        public Dictionary<Guid?, int?[]> PlayersSeeds { get; set; } = [];

        public TournamentService(IServiceScopeFactory scopeFactory)
        {
            //_configuration = configuration;
            _scopeFactory = scopeFactory;
            _postTournamentService = new PostTournamentService();
            var process = Process.GetCurrentProcess();
            _jsonOptions = new()
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                WriteIndented = true,
            };
            MatchTimer = new System.Timers.Timer()
            {
                Interval = MATCH_MAKER_INTERVAL,
            };
            MatchTimer.Elapsed += MatchTimer_Elapsed;
            MatchTimer.AutoReset = true;
            MatchTimer.Start();
            #region How to hash strings
            //  var byteKey = Encoding.UTF8.GetBytes("some-secret-string");
            // var hashedKey = SHA3_512.HashData(byteKey);
            #endregion

        }


        private async void MatchTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            await CreateMatchesFromQueue();
        }

        /// <summary>
        /// Formerly known as 'GetMatch'
        /// </summary>
        public async Task CreateMatchesFromQueue()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbService = scope.ServiceProvider.GetRequiredService<ISuikaDbService>();
                if (!await _createMatchesFromQueueSemaphore.WaitAsync(0))
                {
                    return;
                }
                try
                {
                    var playersByTimeWaiting = await dbService.GetPlayersWaitingForMatch(50);
                    foreach (var waitingPlayer in playersByTimeWaiting)
                    {
                        await MatchPlayerIntoBestTournament(dbService, waitingPlayer);
                    }
                }
                catch (Exception ex)
                {
                    await dbService.Log(ex);
                }
                finally
                {
                    // Ensure the semaphore is released even if an exception occurs
                    _createMatchesFromQueueSemaphore.Release();
                }
            }
        }

        private async Task<TournamentSession> MatchPlayerIntoBestTournament(ISuikaDbService dbService, MatchQueueEntry waitingPlayer)
        {
            //var playerBalance = await dbService.GetPlayerBalance(waitingPlayer.Player.PlayerId, waitingPlayer.QueueEntry.CurrencyId);
            //if (playerBalance == null)
            //{
            //    await dbService.RemovePlayerFromActiveMatchMaking(waitingPlayer.Player.PlayerId);
            //    await dbService.Log("Player is waiting for tournament but has no balance", waitingPlayer.Player.PlayerId);
            //    return null;
            //}
  

            try
            {
                var playerId = waitingPlayer.Player.PlayerId;
                var gameRating = await dbService.GetPlayerGameRating(playerId, waitingPlayer.QueueEntry.GameTypeId);
                int playerGameRating = (int)(gameRating?.Rating ?? 1500);


                var suitableTournaments = await dbService.FindSuitableTournamentForRating(
                    playerId,
                    waitingPlayer.QueueEntry.GameTypeId,
                    playerGameRating,
                    MAX_RATING_DRIFT,
                    waitingPlayer.QueueEntry.TournamentTypeId,
                    waitingPlayer.QueueEntry.CurrencyId,
                    //null,
                    1
                );
                //var playerId = waitingPlayer.Player.PlayerId;
                // ADD PLAYER TO TOURNAMENT
                ///////////////////////////////
                if (suitableTournaments.Any())
                {
                    return await AddToExistingTournament(dbService, waitingPlayer,suitableTournaments.First()
                        );
                }
                else
                {
                    return await CreateNewTournament(dbService, waitingPlayer);
                }
            }
            catch (Exception ex)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbServiceForException = scope.ServiceProvider.GetRequiredService<ISuikaDbService>();
                    await dbServiceForException.Log(ex, waitingPlayer.Player.PlayerId);
                    await dbServiceForException.RemovePlayerFromActiveMatchMaking(waitingPlayer.Player.PlayerId);
                }
                return null;
            }
        }

        public async Task<TournamentSession> CreateNewTournament(ISuikaDbService dbService, MatchQueueEntry queueEntry)
        {
            if (queueEntry != null)
            {
                var playerId = queueEntry.Player.PlayerId;
                var gameRating = await dbService.GetPlayerGameRating(playerId, queueEntry.QueueEntry.GameTypeId);
                int playerGameRating = (int)(gameRating?.Rating ?? 1500);


                var tournamentType = queueEntry.LoadTournamentType(dbService.LeiaContext);
                var currency = queueEntry.LoadCurrency(dbService.LeiaContext);
                var tournament = await SaveNewTournament(dbService, queueEntry.QueueEntry.GameTypeId, tournamentType.EntryFee.Value, currency.CurrencyId, tournamentType.TournamentTypeId, queueEntry.Player.PlayerId);

                await dbService.Log($"Player: {queueEntry.Player?.PlayerId}, rating: {playerGameRating}\n was added to tournament: {tournament.TournamentSessionId}.");

                return tournament;
            }
            else {
                await dbService.Log("Got null match request");
                return null;
            }
        }

        public async Task<TournamentSession> AddToExistingTournament(ISuikaDbService dbService, MatchQueueEntry queueEntry, TournamentSession? matchingTournament)
        {
            if (matchingTournament == null) return null;
            var tournamentType = queueEntry.LoadTournamentType(dbService.LeiaContext);
            var maxPlayers = tournamentType.NumberOfPlayers;

            var dbTournament = await dbService.LeiaContext.Tournaments
                   .Include(t => t.Players)
                   .Include(t => t.PlayerTournamentSessions)
                   .FirstOrDefaultAsync(t => t.TournamentSessionId == matchingTournament.TournamentSessionId);

            if (dbTournament == null) return null;
            //if (dbTournament.Players.Count >= maxPlayers)
            //{
            //    await dbService.Log($"Tournament is full: {dbTournament.TournamentSessionId}", queueEntry.Player.PlayerId);
            //    return null;
            //}

            var dbPlayer = await dbService.LeiaContext.Players.FindAsync(queueEntry.Player.PlayerId);
            if (dbPlayer == null)
            {
                await dbService.Log($"Player not found: {queueEntry.Player.PlayerId}", queueEntry.Player.PlayerId);
                return null;
            }

            var canJoin = await dbService.SetPlayerActiveTournament(dbPlayer.PlayerId, dbTournament.TournamentSessionId);
            if (!canJoin)
            {
                await dbService.Log($"Player not in matchmaking: {dbPlayer.PlayerId}", dbPlayer.PlayerId);
                return null;
            }

            try
            {
                var gameRating = await dbService.GetPlayerGameRating(dbPlayer.PlayerId, queueEntry.QueueEntry.GameTypeId);
                int playerGameRating = (int)(gameRating?.Rating ?? 1500);

                dbTournament.Players.Add(dbPlayer);
                dbTournament.PlayerTournamentSessions.Add(new PlayerTournamentSession
                {
                    PlayerId = dbPlayer.PlayerId,
                    TournamentSession = dbTournament,
                    JoinTime = DateTime.UtcNow,
                    DidClaim = null,
                    Position = 0,
                    TournamentTypeId = tournamentType.TournamentTypeId,
                    PlayerScore = null
                });

                await dbService.Log($"Player {dbPlayer.PlayerId} added to tournament {dbTournament.TournamentSessionId}", dbPlayer.PlayerId);
                await dbService.LeiaContext.SaveChangesAsync();

                return dbTournament;
            }
            catch (Exception ex)
            {
                await dbService.Log(ex, dbPlayer.PlayerId);
                await dbService.RemovePlayerFromActiveTournament(dbPlayer.PlayerId, dbTournament.TournamentSessionId);
                return null;
            }            
        }

        public async Task<TournamentSession?> SaveNewTournament(ISuikaDbService dbService, int gameTypeId, double matchFee, int? currencyId, int? tournamentTypeId, params Guid?[]? playerIds)
        {

            if (tournamentTypeId == null)
                throw new Exception("SaveNewTournamentReceived null `tournamentTypeId`");
            
            var currency = await dbService.LeiaContext.Currencies.FindAsync(currencyId);

            List<Player> dbPlayers = new();

            foreach (var id in playerIds.Where(id => id.HasValue))
            {
                if (!await dbService.IsPlayerMatchMaking(id.Value))
                {
                    await dbService.Log($"SaveNewTournament: Player {id} is not in matchmaking", id.Value);
                    continue;
                }

                var player = await dbService.LeiaContext.Players.FindAsync(id);
                if (player != null)
                    dbPlayers.Add(player);
            }


            if (dbPlayers.Count == 0)
                return null;

            var gameRating = await dbService.LeiaContext.PlayerGameRatings
                .FirstOrDefaultAsync(r => r.PlayerId == dbPlayers[0].PlayerId && r.GameId == gameTypeId);

            var tournament = new TournamentSession
            {
                TournamentSeed = TournamentSeedRandom.Next(),
                StartTime = DateTime.UtcNow,
                Rating = (int)Math.Round(gameRating?.Rating ?? 1500),
                GameTypeId = gameTypeId,
                Players = new List<Player>(),
                PlayerTournamentSessions = new List<PlayerTournamentSession>()
            };

            dbService.LeiaContext.Tournaments.Add(tournament);

            // Register players to the tournament
            foreach (var player in dbPlayers)
            {
                tournament.Players.Add(player);
                tournament.PlayerTournamentSessions.Add(new PlayerTournamentSession
                {
                    TournamentSession = tournament,
                    PlayerId = player.PlayerId,
                    DidClaim = null,
                    JoinTime = DateTime.UtcNow,
                    PlayerScore = null,
                    Position = 0,
                    TournamentTypeId = tournamentTypeId.Value
                });
            }

            await dbService.Log("SaveNewTournament: Creating tournament", dbPlayers[0].PlayerId);
            await dbService.LeiaContext.SaveChangesAsync();

            foreach (var playerId in dbPlayers.Select(p => p.PlayerId))
            {
                var canCreate = await dbService.SetPlayerActiveTournament(playerId, tournament.TournamentSessionId);
                if (!canCreate)
                {
                    await dbService.Log($"SaveNewTournament: Player {playerId} could not join — rolling back", playerId);
                    foreach (var p in dbPlayers.Select(p => p.PlayerId))
                        await dbService.RemovePlayerFromAnyActiveTournament(p);
                    return null;
                }
            }


            var playerGUIDs = dbPlayers.Select(p => (Guid?)p.PlayerId).ToArray();
            SendPlayerAndSeed(tournament.TournamentSeed, tournament.TournamentSessionId, playerGUIDs);
            return tournament;
        }

        private void SendPlayerAndSeed(int? seed, int? tournamentId, params Guid?[]? playerIds)
        {
            var subscribers = PlayerAddedToTournament?.GetInvocationList().Length ?? 0;
            Trace.WriteLine($"Attempting to raise event. Number of subscribers: {subscribers}");
            /// example how to use ValueTuple types 👇🏻
            ///var data = (Seed: seed, TournamentId: tournamentId, Ids: playerIds);
            SeedData data = new() { Seed = seed, TournamentId = tournamentId, Ids = playerIds };
            PlayerAddedToTournament?.Invoke(data, new EventArgs());
        }

        /// <summary>
        /// Container class to hold data to send to the player/seed list
        /// </summary>
        public class SeedData
        {
            public int? Seed { get; set; }
            public int? TournamentId { get; set; }
            public Guid?[]? Ids { get; set; }

        }

        public async Task CloseTimedOutMatches()
        {
            //todo check for sessions who are older than 12 hours and close them
            //todo send event telling the tournament was closed 
            throw new NotImplementedException();
        }

        public async Task CheckTournamentStatus(List<PlayerTournamentSession> sortedDataForFinalTournamentCalc, TournamentType tournamentType, ISuikaDbService dbService, int tournamentId, Player callingPlayer/*, PlayerTournamentSession playerTournamentSession*/)
        {
            var context = dbService.LeiaContext;
            try
            {
                //var tournament = context.Tournaments

                //    .Include(t => t.PlayerTournamentSessions)
                //    .Include(t => t.Players)
                //    .FirstOrDefault(t => t.TournamentSessionId == tournamentId);

                //if (tournament != null)
                //{
                    var scores = context.PlayerTournamentSession.Where(p => sortedDataForFinalTournamentCalc.Select(x => x.PlayerId).Contains(p.PlayerId)).Select(pt => pt.PlayerScore).ToList();

                    if (/*scores.All(s => s != null) &&*/ scores.Count >= tournamentType.NumberOfPlayers) 
                        await _postTournamentService.CloseTournament(sortedDataForFinalTournamentCalc, tournamentId/*, tournament*/); // close tournament

                    //if (/*scores.All(s => s != null) &&*/ scores.Count >= tournamentType.NumberOfPlayers) 
                    //    await _postTournamentService.CloseTournament(tournament); // close tournament

                //}
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }
        }

        public async Task<PlayerCurrencies?> ChargePlayer(Guid playerId, int? tournamentId)
        { // this 👇🏻 gets the current scope of the services that are Scoped.
            // through it, we can get the actual service instance that is assigned in the scope. for example the suikaDbService instance that
            // is associated with current HTTP call in the controller . this prevents collisions between scopes. for example if suikaDbService is
            // called from PlayersController, and from MatchingController - each creates a different instance of suikaDbService. each of them is
            // also  injected into the TournamentService in each Controller. which may result in 2 different suikaDbServices being injected into 
            // TournamentService, depending on the Controller that makes the call to TournamentService. this results in 2 different Contexts for
            // the 2 Controllers. which of course creates problems when a method in TournamentService that was called from PlayersController and
            // then another method was called from MatchingController, cause the 2 different instances of the Context to collide.
            // 
            using (var scope = _scopeFactory.CreateScope())
            {
                var suikaDbService = scope.ServiceProvider.GetRequiredService<ISuikaDbService>();
                var dbPlayer = await suikaDbService.GetPlayerById(playerId);
                if (dbPlayer == null)
                {
                    var ex = new Exception($"ChargePlayer: Could not load player {playerId}");
                    await suikaDbService.Log(ex, playerId);
                    throw ex;
                }

                var playerTournamentSession = await suikaDbService.LeiaContext.PlayerTournamentSession
                    .Include(pt => pt.TournamentType)
                    .FirstOrDefaultAsync(s => s.PlayerId == playerId && s.TournamentSession.TournamentSessionId == tournamentId);
                if (playerTournamentSession == null)
                {
                    var ex = new Exception($"ChargePlayer: Could not load player session {playerId} for tournament {tournamentId}");
                    await suikaDbService.Log(ex, playerId);
                    throw ex;
                }

                var dbTournament = suikaDbService.LeiaContext.Tournaments
                        .FirstOrDefault(t => t.TournamentSessionId == tournamentId);

                if (dbTournament == null)
                {
                    var ex = new Exception($"ChargePlayer: Could not charge player {playerId}, tournament '{tournamentId}' not found!");
                    await suikaDbService.Log(ex, playerId);
                    throw ex;
                }

                var tournamentType = playerTournamentSession.TournamentType;

                if (tournamentType.CurrenciesId == 10)
                {
                    var fee = -tournamentType.EntryFee;

                    var updatedBalance = await suikaDbService.UpdatePlayerBalance(playerId, tournamentType.CurrenciesId, fee);

                    return updatedBalance;
                }
                else
                {
                    double? idealCash = tournamentType?.EntryFee * 0.8;
                    double? idealBonus = tournamentType?.EntryFee - idealCash;

                    var currentCashBalance = await suikaDbService.GetPlayerBalance(playerId, 6); //flag hardcoded
                    var currentBonusCashBalance = await suikaDbService.GetPlayerBalance(playerId, 13); //flag hardcoded

                    // Rule 4: Check that the player has enough overall.
                    if ((currentCashBalance ==null && currentBonusCashBalance == null) || currentCashBalance + currentBonusCashBalance < tournamentType?.EntryFee)
                    {
                        var ex = new Exception($"ChargePlayer: Insufficient funds for player {playerId}");
                        await suikaDbService.Log(ex, playerId);
                        throw ex;
                    }

                    double? cashToDeduct = idealCash;
                    double? bonusToDeduct = idealBonus;

                    // Rule 2 & 3: If one account lacks enough funds, use as much as possible and cover the rest from the other.
                    if (currentCashBalance < idealCash)
                    {
                        cashToDeduct = currentCashBalance;
                        bonusToDeduct = tournamentType?.EntryFee - cashToDeduct;
                    }
                    else if (currentBonusCashBalance < idealBonus)
                    {
                        bonusToDeduct = currentBonusCashBalance;
                        cashToDeduct = tournamentType?.EntryFee - bonusToDeduct;
                    }

                    var transaction = await suikaDbService.AddTransactionRecordAsync(playerId, tournamentType.CurrenciesId, (decimal)-tournamentType.EntryFee, "Tournament Entry Fee");
                    
                    var updatedBalance = await suikaDbService.UpdatePlayerBalance(playerId, 6, -cashToDeduct);
                    updatedBalance = await suikaDbService.UpdatePlayerBalance(playerId, 13, -bonusToDeduct);

                    return updatedBalance;
                }
            }
        }
        //public async Task<PlayerCurrencies?> ChargePlayer(Guid playerId, int? tournamentId)
        //{ // this 👇🏻 gets the current scope of the services that are Scoped.
        //    // through it, we can get the actual service instance that is assigned in the scope. for example the suikaDbService instance that
        //    // is associated with current HTTP call in the controller . this prevents collisions between scopes. for example if suikaDbService is
        //    // called from PlayersController, and from MatchingController - each creates a different instance of suikaDbService. each of them is
        //    // also  injected into the TournamentService in each Controller. which may result in 2 different suikaDbServices being injected into 
        //    // TournamentService, depending on the Controller that makes the call to TournamentService. this results in 2 different Contexts for
        //    // the 2 Controllers. which of course creates problems when a method in TournamentService that was called from PlayersController and
        //    // then another method was called from MatchingController, cause the 2 different instances of the Context to collide.
        //    // 
        //    using (var scope = _scopeFactory.CreateScope())
        //    {
        //        var suikaDbService = scope.ServiceProvider.GetRequiredService<ISuikaDbService>();
        //        var dbPlayer = await suikaDbService.GetPlayerById(playerId);
        //        if (dbPlayer == null)
        //        {
        //            var ex = new Exception($"ChargePlayer: Could not load player {playerId}");
        //            await suikaDbService.Log(ex, playerId);
        //            throw ex;
        //        }

        //        var playerTournamentSession = await suikaDbService.LeiaContext.PlayerTournamentSession
        //            .Include(pt => pt.TournamentType)
        //            .FirstOrDefaultAsync(s => s.PlayerId == playerId && s.TournamentSession.TournamentSessionId == tournamentId);
        //        if (playerTournamentSession == null)
        //        {
        //            var ex = new Exception($"ChargePlayer: Could not load player session {playerId} for tournament {tournamentId}");
        //            await suikaDbService.Log(ex, playerId);
        //            throw ex;
        //        }

        //        var dbTournament = suikaDbService.LeiaContext.Tournaments
        //                .FirstOrDefault(t => t.TournamentSessionId == tournamentId);

        //        if (dbTournament == null)
        //        {
        //            var ex = new Exception($"ChargePlayer: Could not charge player {playerId}, tournament '{tournamentId}' not found!");
        //            await suikaDbService.Log(ex, playerId);
        //            throw ex;
        //        }

        //        var tournamentType = playerTournamentSession.TournamentType;

        //        var currencyId = tournamentType.CurrenciesId;
        //        var fee = -tournamentType.EntryFee;

        //        var updatedBalance = await suikaDbService.UpdatePlayerBalance(playerId, currencyId, fee);
        //        return updatedBalance;
        //    }
        //}
    }
}
