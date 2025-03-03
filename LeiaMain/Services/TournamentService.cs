using System.Diagnostics;
using System.Text.Json;
using System.Timers;
using DataObjects;
using Microsoft.EntityFrameworkCore;
using DAL;
using Microsoft.Extensions.DependencyInjection;

namespace Services
{
    public interface ITournamentService
    {
        public event EventHandler PlayerAddedToTournament;
        public Dictionary<Guid?, int?[]> PlayersSeeds { get; set; }
        public Task CheckTournamentStatus(ISuikaDbService dbService, int tournamentId, PlayerTournamentSession playerTournamentSession);
        public Task<PlayerCurrencies?> ChargePlayer(Guid playerId, int? tournamentId);
    }

    public class TournamentService : ITournamentService
    {
        private const int MAX_RATING_DRIFT = 5000;
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
            var playerBalance = await dbService.GetPlayerBalance(waitingPlayer.Player.PlayerId, waitingPlayer.QueueEntry.CurrencyId);
            if (playerBalance == null)
            {
                await dbService.RemovePlayerFromActiveMatchMaking(waitingPlayer.Player.PlayerId);
                await dbService.Log("Player is waiting for tournament but has no balance", waitingPlayer.Player.PlayerId);
                return null;
            }



            try
            {
                var suitableTournaments = await dbService.FindSuitableTournamentForRating(
                    waitingPlayer.Player.PlayerId,
                    waitingPlayer.Player.Rating,
                    MAX_RATING_DRIFT,
                    waitingPlayer.QueueEntry.TournamentTypeId,
                    waitingPlayer.QueueEntry.CurrencyId,
                    playerBalance.Value,
                    1
                );
                var playerId = waitingPlayer.Player.PlayerId;
                // ADD PLAYER TO TOURNAMENT
                ///////////////////////////////
                if (suitableTournaments.Any())
                {
                    return await AddToExistingTournament(dbService, waitingPlayer.ConvertToLegacyMatchRequest(
                        dbService.LeiaContext),
                        suitableTournaments.First()
                        );
                }
                else
                {
                    return await CreateNewTournament(dbService, waitingPlayer.ConvertToLegacyMatchRequest(dbService.LeiaContext));
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

        public async Task<TournamentSession> CreateNewTournament(ISuikaDbService dbService, MatchRequest? matchedRequest)
        {

            if (matchedRequest != null)
            {
                var tournament = await SaveNewTournament(dbService, matchedRequest.MatchFee, matchedRequest?.MatchFeeCurrency?.CurrencyId, matchedRequest?.TournamentType?.TournamentTypeId, matchedRequest?.Player?.PlayerId);

                await dbService.Log($"Player: {matchedRequest?.Player?.PlayerId}, rating: {matchedRequest?.Player?.Rating}\n was added to tournament: {tournament.TournamentSessionId}.");

                return tournament;
            }
            else
            {
                await dbService.Log("Got null match request");
                return null;
            }
        }

        public async Task<TournamentSession> AddToExistingTournament(ISuikaDbService dbService, MatchRequest request, TournamentSession? matchingTournament)
        {
            if (matchingTournament?.Players?.Count >= request?.TournamentType?.NumberOfPlayers /*_maxNumPlayers*/) // if tournament has room in it, add the current request player
            {
                await dbService.Log($"AddToExistingTournament: Error: Tournament is full: {matchingTournament.TournamentSessionId}", request.Player.PlayerId);
                return null;
            }
            var dbTournament = dbService.LeiaContext.Tournaments.Find(matchingTournament?.TournamentSessionId);

            Player dbPlayer;
            if (request != null)
            {
                dbPlayer = dbService.LeiaContext.Players.Find(request?.Player?.PlayerId);
            }
            else
            {
                var message = "AddToExistingTournament: Got null request AND null MatchRequest";
                await dbService.Log(message);
                return null; // Don't change anything
            }
            if (dbPlayer == null)
            {
                var message = $"AddToExistingTournament: Got null player while attempting to join tournament {dbTournament.TournamentSessionId}";
                await dbService.Log(message);
                return null; // Don't change anything
            }
            //dbTournament?.Players?.Add(dbPlayer);


            var canJoinTournament = await dbService.SetPlayerActiveTournament(dbPlayer.PlayerId, dbTournament.TournamentSessionId);
            if (!canJoinTournament)
            {
                var message = $"AddToExistingTournament: Player {dbPlayer.PlayerId} attempted to join tournament {dbTournament.TournamentSessionId}, but was not in matchmaking state!";
                await dbService.Log(message, dbPlayer.PlayerId);
                return null;
            }

            // try to update the tournament in the database
            try
            {
                dbService.LeiaContext.Entry(dbTournament).State = EntityState.Detached;

                var savedTournament = dbService?.LeiaContext?.Tournaments?.Update(dbTournament);


                await dbService.Log($"AddToExistingTournament: Player: {request?.Player?.Rating}, rating: {request?.Player?.Rating}, \n were added to tournament: {savedTournament?.Entity?.TournamentSessionId}.", dbPlayer.PlayerId);

                dbTournament?.Players.Add(request.Player);
                dbTournament?.PlayerTournamentSessions?.Add(new PlayerTournamentSession
                {
                    PlayerId = request.Player.PlayerId,
                    //TournamentSessionId = matchingTournament.TournamentSessionId,
                    TournamentSession = matchingTournament,
                    JoinTime = DateTime.UtcNow,
                    DidClaim = null,
                    Position = 0,
                    TournamentTypeId = request.TournamentType.TournamentTypeId,
                    PlayerScore = null,
                }
                );
                // deprecate the old seed sending 
                // SendPlayerAndSeed(savedTournament?.Entity?.TournamentSeed, savedTournament?.Entity?.TournamentSessionId, request?.Player?.PlayerId);
                dbService.LeiaContext.Update(dbTournament);
                var message = $"AddToExistingTournament: Player {dbPlayer.PlayerId} joined tournament {dbTournament.TournamentSessionId}";
                await dbService.Log(message, dbPlayer.PlayerId);
                var saved = await dbService?.LeiaContext?.SaveChangesAsync();
                return dbTournament;

                // throw new Exception($"AddToExistingTournament: Tournament {dbTournament.TournamentSessionId} was not saved to database!");

            }
            catch (Exception ex)
            {
                await dbService.Log(ex, dbPlayer.PlayerId);
                await dbService.RemovePlayerFromActiveTournament(dbPlayer.PlayerId, dbTournament.TournamentSessionId);
            }
            return null;

        }

        public async Task<TournamentSession?> SaveNewTournament(ISuikaDbService dbService, double matchFee, int? currencyId, int? tournamentTypeId, params Guid?[]? playerIds)
        {

            if (tournamentTypeId == null)
            {
                throw new Exception("SaveNewTournamentReceived null `tournamentTypeId`");
            }
            Debug.WriteLine($"=====> Inside SaveNewTournament, with players: {string.Join(", ", playerIds)}");
            var currency = dbService.LeiaContext.Currencies.Find(currencyId);

            List<Player> dbPlayers = new();
            foreach (var id in playerIds)
            {
                try
                {
                    if (id == null)
                    {
                        continue;
                    }
                    if (!await dbService.IsPlayerMatchMaking(id.Value))
                    {
                        await dbService.Log($"SaveNewTournament: Player {id} cannot join new tournament because the player is not in matchmake state", id.Value);
                        continue;
                    }
                    var player = dbService.LeiaContext.Players.Find(id);
                    if (player != null) dbPlayers.Add(player);
                }
                catch (Exception ex)
                {
                    await dbService.Log(ex.Message + "\n" + ex.InnerException?.Message);
                    throw;
                }
            }
            if (dbPlayers.Count == 0)
            {
                return null;
            }


            dbService.LeiaContext.Entry(currency).State = EntityState.Detached;

            // Create the tournament
            var tournament = new TournamentSession
            {
                TournamentSeed = TournamentSeedRandom.Next(),
                IsOpen = true,
                StartTime = DateTime.UtcNow,
                Rating = dbPlayers[0].Rating,
            };
            var savedTournament = dbService?.LeiaContext?.Tournaments?.Add(tournament);

            // Register players to the tournament
            foreach (var player in dbPlayers)
            {
                var playerTournamentSession = new PlayerTournamentSession
                {
                    TournamentSession = tournament,
                    PlayerId = player.PlayerId,
                    DidClaim = null,
                    JoinTime = DateTime.UtcNow,
                    PlayerScore = null,
                    Position = 0,
                    TournamentTypeId = tournamentTypeId.Value,
                };
                var savedPlayerTSession = dbService.LeiaContext.PlayerTournamentSession.Add(playerTournamentSession);
            }


            await dbService.Log("SaveNewTournament: Going to create new tournament", dbPlayers[0].PlayerId);
            var saved = await dbService?.LeiaContext?.SaveChangesAsync();

            var idsArray = dbPlayers?.Select(p => p?.PlayerId).ToArray();
            if (saved > 0)
            {
                SendPlayerAndSeed(savedTournament?.Entity?.TournamentSeed, savedTournament?.Entity?.TournamentSessionId, idsArray);
                var addedPlayerIds = new List<Guid>();
                foreach (var playerId in idsArray)
                {
                    if (playerId == null)
                    {
                        continue;
                    }
                    await dbService.Log($"SaveNewTournament: Player {playerId} will create tournament {savedTournament.Entity.TournamentSessionId}", playerId.Value);
                    var canCreateTournament = await dbService.SetPlayerActiveTournament(playerId.Value, savedTournament.Entity.TournamentSessionId);
                    if (!canCreateTournament)
                    {
                        var message = $"SaveNewTournament: Player {playerId} attempted to create a new tournament, but was not in matchmaking state!";
                        await dbService.Log(message, playerId.Value);
                        foreach (var alreadyAddedPlayerId in addedPlayerIds)
                        {
                            var message2 = $"SaveNewTournament: Player {playerId} attempted to create a new tournament, but is removed because another player {playerId.Value} could not join";
                            await dbService.Log(message2, alreadyAddedPlayerId);
                            await dbService.RemovePlayerFromAnyActiveTournament(alreadyAddedPlayerId);
                        }
                        savedTournament.Entity.IsOpen = false;
                        await dbService?.LeiaContext?.SaveChangesAsync();
                        return null;
                    }
                    else
                    {
                        addedPlayerIds.Add(playerId.Value);
                    }
                }
            }
            return savedTournament?.Entity;

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

        public async Task CheckTournamentStatus(ISuikaDbService dbService, int tournamentId, PlayerTournamentSession playerTournamentSession)
        {
            var context = dbService.LeiaContext;
            try
            {
                var tournament = context.Tournaments

                    .Include(t => t.PlayerTournamentSessions)
                    .Include(t => t.Players)
                    .FirstOrDefault(t => t.TournamentSessionId == tournamentId);
                if (tournament != null)
                {
                    var scores = context.PlayerTournamentSession.Where(pt => pt.TournamentSession.TournamentSessionId == tournamentId).Select(pt => pt.PlayerScore).ToList();
                    if (scores.All(s => s != null) && scores.Count >= playerTournamentSession.TournamentType.NumberOfPlayers) await _postTournamentService.CloseTournament(tournament); // close tournament

                }
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

                var currencyId = tournamentType.CurrenciesId;
                var fee = -tournamentType.EntryFee;

                var updatedBalance = await suikaDbService.UpdatePlayerBalance(playerId, currencyId, fee);
                return updatedBalance;
            }
        }
    }
}
