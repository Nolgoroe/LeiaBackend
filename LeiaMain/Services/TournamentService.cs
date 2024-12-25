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
        public Task CheckTournamentStatus(int tournamentId);
        public Task<PlayerCurrencies?> ChargePlayer(Guid playerId, int? tournamentId);
    }

    public class TournamentService : ITournamentService
    {
        private const int MAX_RATING_DRIFT = 500; 
        private const int MATCH_MAKER_INTERVAL = 500; // get these numbers from tournament DB or config file
        private readonly ISuikaDbService _suikaDbService;
        private readonly IPostTournamentService _postTournamentService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SemaphoreSlim _createMatchesFromQueueSemaphore = new SemaphoreSlim(1, 1);
        //private readonly IConfiguration _configuration;
        private JsonSerializerOptions _jsonOptions;
        public System.Timers.Timer MatchTimer { get; set; }
        private bool _isCreatingMatchesAllowed = true;
        public event EventHandler PlayerAddedToTournament;

        public Dictionary<Guid?, int?[]> PlayersSeeds { get; set; } = [];

        public TournamentService(IServiceScopeFactory scopeFactory)
        {
            //_configuration = configuration;
            _scopeFactory = scopeFactory;
            _suikaDbService = new SuikaDbService(new LeiaContext());
            _postTournamentService = new PostTournamentService(_suikaDbService);
            
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

        private async Task MatchPlayerIntoBestTournament(ISuikaDbService dbService, MatchQueueEntry waitingPlayer)
        {
            var playerBalance = await dbService.GetPlayerBalance(waitingPlayer.Player.PlayerId, waitingPlayer.QueueEntry.CurrencyId);
            if (playerBalance == null)
            {
                await dbService.RemovePlayerFromActiveMatchMaking(waitingPlayer.Player.PlayerId);
                return;
            }
            var suitableTournaments = await dbService.FindSuitableTournamentForRating(
                waitingPlayer.Player.Rating,
                MAX_RATING_DRIFT,
                waitingPlayer.QueueEntry.TournamentTypeId,
                waitingPlayer.QueueEntry.CurrencyId,
                playerBalance.Value,
                1
                );
            try
            {
                if (suitableTournaments.Any())
                {
                    await AddToExistingTournament(waitingPlayer.ConvertToLegacyMatchRequest(
                        dbService.LeiaContext),
                        suitableTournaments.First()
                        );
                }
                else
                {
                    await CreateNewTournament(waitingPlayer.ConvertToLegacyMatchRequest(dbService.LeiaContext));
                }
            }
            catch (Exception ex)
            {
                await dbService.RemovePlayerFromActiveMatchMaking(waitingPlayer.Player.PlayerId);
                await dbService.Log(ex, waitingPlayer.Player.PlayerId);
            }
        }

        public async Task CreateNewTournament(MatchRequest? matchedRequest)
        {

            if (matchedRequest != null)
            {
                var tournament = await SaveNewTournament(matchedRequest.MatchFee, matchedRequest?.MatchFeeCurrency?.CurrencyId, matchedRequest?.TournamentType?.TournamentTypeId, matchedRequest?.Player?.PlayerId);

                Trace.WriteLine($"Player: {matchedRequest?.Player?.PlayerId}, rating: {matchedRequest?.Player?.Rating}\n was added to tournament: {tournament?.TournamentDataId}.");

                // make sure the tournament isn't already full
                if (tournament != null)
                {
                    var dbTournament = _suikaDbService.LeiaContext.Tournaments
                            .Include(t => t.TournamentData)
                                .ThenInclude(td => td.TournamentType)
                            .FirstOrDefault(t => t.TournamentSessionId == tournament.TournamentSessionId);
                }
            }
            else {
                await _suikaDbService.Log("Got null match request");
            }
        }

        public async Task AddToExistingTournament(MatchRequest request, TournamentSession? matchingTournament)
        {
            if (matchingTournament?.Players?.Count < request?.TournamentType?.NumberOfPlayers /*_maxNumPlayers*/) // if tournament has room in it, add the current request player
            {
                var dbTournament = _suikaDbService.LeiaContext.Tournaments.Find(matchingTournament?.TournamentSessionId);

                Player dbPlayer;
                if (request != null)
                {
                    dbPlayer = _suikaDbService.LeiaContext.Players.Find(request?.Player?.PlayerId);
                    //MatchesQueue.Remove(request);
                }
                else
                {
                    var message = "AddToExistingTournament: Got null request AND null MatchRequest";
                    await _suikaDbService.Log(message);
                    return; // Don't change anything
                }
                if (dbPlayer == null)
                {
                    var message = $"AddToExistingTournament: Got null player while attempting to join tournament {dbTournament.TournamentSessionId}";
                    await _suikaDbService.Log(message);
                    return; // Don't change anything
                }
                dbTournament?.Players?.Add(dbPlayer);
                

                var canJoinTournament = await _suikaDbService.SetPlayerActiveTournament(dbPlayer.PlayerId, dbTournament.TournamentSessionId);
                if (!canJoinTournament)
                {
                    var message = $"AddToExistingTournament: Player {dbPlayer.PlayerId} attempted to join tournament {dbTournament.TournamentSessionId}, but was not in matchmaking state!";                    
                    await _suikaDbService.Log(message, dbPlayer.PlayerId);
                    return;
                }

                // try to update the tournament in the database
                try
                {
                    _suikaDbService.LeiaContext.Entry(dbTournament).State = EntityState.Modified;

                    var savedTournament = _suikaDbService?.LeiaContext?.Tournaments?.Update(dbTournament);
                    var saved = await _suikaDbService?.LeiaContext?.SaveChangesAsync();

                    Trace.WriteLine($"AddToExistingTournament: Player: {request?.Player?.Rating}, rating: {request?.Player?.Rating}, \n were added to tournament: {savedTournament?.Entity?.TournamentSessionId}.");

                    if (saved > 0)//if the tournament was saved to the DB -
                    {
                        matchingTournament?.Players.Add(request?.Player);
                        matchingTournament?.PlayerTournamentSessions?.Add(new PlayerTournamentSession
                        {
                            PlayerId = request.Player.PlayerId,
                            TournamentSessionId = matchingTournament.TournamentSessionId,
                            PlayerScore = null,
                        }
                        );
                        // send the tournament seed to controller list
                        SendPlayerAndSeed(savedTournament?.Entity?.TournamentSeed, savedTournament?.Entity?.TournamentSessionId, request?.Player?.PlayerId);
                        var message = $"AddToExistingTournament: Player {dbPlayer.PlayerId} joined tournament {dbTournament.TournamentSessionId}";
                        await _suikaDbService.Log(message, dbPlayer.PlayerId);
                    }
                }
                catch (Exception ex)
                {
                    await _suikaDbService.Log(ex, dbPlayer.PlayerId);
                    await _suikaDbService.RemovePlayerFromActiveTournament(dbPlayer.PlayerId, dbTournament.TournamentSessionId);
                }
            }
        }

        public async Task<TournamentSession?> SaveNewTournament(double matchFee, int? currencyId, int? tournamentTypeId, params Guid?[]? playerIds)
        {

            Debug.WriteLine($"=====> Inside SaveNewTournament, with players: {string.Join(", ", playerIds)}");
            var currency = _suikaDbService.LeiaContext.Currencies.Find(currencyId);

            List<Player> dbPlayers = new();
            foreach (var id in playerIds)
            {
                try
                {
                    if (id == null)
                    {
                        continue;
                    }
                    if (!await _suikaDbService.IsPlayerMatchMaking(id.Value))
                    {
                        await _suikaDbService.Log($"SaveNewTournament: Player {id} cannot join new tournmanet because the player is not in matchmake state", id.Value);
                        continue;
                    }
                    var player = _suikaDbService.LeiaContext.Players.Find(id);
                    if (player != null) dbPlayers.Add(player);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                    throw;
                }
            }
            if (dbPlayers.Count == 0)
            {
                return null;
            }

            var tournament = new TournamentSession
            {
                TournamentSeed = new Random().Next(),
                IsOpen = true,
                TournamentData = new TournamentData
                {
                    EntryFee = matchFee,
                    EntryFeeCurrencyId = currency.CurrencyId,
                    EarningCurrencyId = currency.CurrencyId,
                    TournamentTypeId = (int)tournamentTypeId,
                    TournamentStart = DateTime.Now,
                    TournamentEnd = default,

                },
                Rating = dbPlayers[0].Rating,
            };
            tournament.Players?.AddRange(dbPlayers);
            _suikaDbService.LeiaContext.Entry(currency).State = EntityState.Detached;

            try
            {
                var savedTournament = _suikaDbService?.LeiaContext?.Tournaments?.Add(tournament);
                var saved = await _suikaDbService?.LeiaContext?.SaveChangesAsync();

                var idsArray = dbPlayers?.Select(p => p?.PlayerId).ToArray();
                if (saved > 0) {
                    SendPlayerAndSeed(savedTournament?.Entity?.TournamentSeed, savedTournament?.Entity?.TournamentSessionId, idsArray);
                    var addedPlayerIds = new List<Guid>();
                    foreach (var playerId in idsArray)
                    {
                        if (playerId == null) 
                        {
                            continue; 
                        }
                        await _suikaDbService.Log($"SaveNewTournament: Player {playerId} will create tournament {savedTournament.Entity.TournamentSessionId}");
                        var canCreateTournament = await _suikaDbService.SetPlayerActiveTournament(playerId.Value, savedTournament.Entity.TournamentSessionId);
                        if (!canCreateTournament)
                        {
                            var message = $"SaveNewTournament: Player {playerId} attempted to create a new tournament, but was not in matchmaking state!";
                            await _suikaDbService.Log(message, playerId.Value);
                            foreach (var alreadyAddedPlayerId in addedPlayerIds)
                            {
                                var message2 = $"SaveNewTournament: Player {playerId} attempted to create a new tournament, but is removed because another player {playerId.Value} could not join";
                                await _suikaDbService.Log(message2, alreadyAddedPlayerId);
                                await _suikaDbService.RemovePlayerFromAnyActiveTournament(alreadyAddedPlayerId);
                            }
                            savedTournament.Entity.IsOpen = false;
                            await _suikaDbService?.LeiaContext?.SaveChangesAsync();
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
            catch (Exception ex)
            {
                throw;
            }
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

        public async Task CheckTournamentStatus(int tournamentId)
        {
            var context = _suikaDbService.LeiaContext;
            try
            {
                var tournament = context.Tournaments
                    .Include(t => t.TournamentData)
                        .ThenInclude(td => td.TournamentType)
                    .Include(t => t.PlayerTournamentSessions)
                    .Include(t => t.Players)
                    .FirstOrDefault(t => t.TournamentSessionId == tournamentId);
                if (tournament != null)
                {
                    var scores = context.PlayerTournamentSession.Where(pt => pt.TournamentSessionId == tournamentId).Select(pt => pt.PlayerScore).ToList();
                    if (scores.All(s => s != null) && scores.Count >= tournament.TournamentData.TournamentType.NumberOfPlayers) await _postTournamentService.CloseTournament(tournament); // close tournament

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
                try
                {
                    var dbPlayer = await /*_*/suikaDbService.GetPlayerById(playerId);
                    if (dbPlayer == null) return null;

                    var dbTournament =/* context_*/suikaDbService.LeiaContext.Tournaments
                        .Include(t => t.TournamentData)
                            .ThenInclude(td => td.TournamentType)
                            .FirstOrDefault(t => t.TournamentSessionId == tournamentId);

                    if (dbTournament == null) return null;

                    var currencyId = dbTournament?.TournamentData?.TournamentType?.CurrenciesId;
                    var fee = -dbTournament?.TournamentData?.TournamentType?.EntryFee;

                    var updatedBalance = await /*_*/suikaDbService.UpdatePlayerBalance(playerId, currencyId, fee);
                    return updatedBalance;

                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                    throw;
                }
            }
        }
    }
}
