
using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.Text.Json;
using System.Timers;
using Microsoft.AspNetCore.Components;
using DataObjects;
using System.Security.Cryptography;
using System.Text;
using Azure.Core;
using Services.MatchMakerStrategies;
using Microsoft.EntityFrameworkCore;
using DAL;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;

namespace Services
{
    public interface ITournamentService
    {
        public List<MatchRequest> MatchesQueue { get; set; }
        public List<MatchRequest> WaitingRequests { get; }
        public List<TournamentSession> OngoingTournaments { get; set; }
        //public System.Timers.Timer MatchTimer { get; set; }
        public event EventHandler PlayerAddedToTournament;
        public Dictionary<Guid?, int?[]> PlayersSeeds { get; set; }
        public int NumMilliseconds { get; set; }

        public Task<bool> FindMatchingTournament(TournamentSession? tournament, params MatchRequest?[] requests);
        public Task CreateNewTournament(MatchRequest? matchedRequest, MatchRequest? request);
        public Task<bool> CheckRequestsMatch(MatchRequest r, MatchRequest request);
        public Task<bool> ProcessFirstRequest(MatchRequest firstRequest);
        public Task AddToExistingTournament(MatchRequest? request, MatchRequest? matchedRequest, TournamentSession? matchingTournament);
        public Task<int?> GetTournamentTypeByCurrency(int? currencyId);
        public Task CheckTournamentStatus(int tournamentId);
        public Task<PlayerCurrencies?> ChargePlayer(Guid playerId, int? tournamentId);
    }

    public class TournamentService : ITournamentService
    {
        private int _timerCycles = 0;

        public int NumMilliseconds { get; set; } = 500; // get these numbers from tournament DB or config file
        private int _maxNumPlayers = 2; // get these numbers from tournament DB or config file
        private int _scoreVariance = 200; // get these numbers from tournament DB or config file 
        private int _scoreVarianceSteps = 400; // get these numbers from tournament DB or config file 
        private IMatchingStrategy? _currentMatchingStrategy;
        private readonly ISuikaDbService _suikaDbService;
        private readonly IPostTournamentService _postTournamentService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SemaphoreSlim _createMatchesFromQueueSemaphore = new SemaphoreSlim(1, 1);
        //private readonly IConfiguration _configuration;
        private JsonSerializerOptions _jsonOptions;
        private Task _createMatchesFromQueueTask;
        private bool _isCreatingMatchesAllowed = true;
        public event EventHandler PlayerAddedToTournament;

        public Dictionary<Guid?, int?[]> PlayersSeeds { get; set; } = [];
        public System.Timers.Timer MatchTimer { get; set; }
        public List<MatchRequest> MatchesQueue { get; set; }
        public List<MatchRequest> WaitingRequests { get; private set; }
        public List<TournamentSession> OngoingTournaments { get; set; }

        public TournamentService(/*IConfiguration configuration */ IServiceScopeFactory scopeFactory)
        {
            MatchesQueue = new List<MatchRequest>();
            OngoingTournaments = new List<TournamentSession>();
            WaitingRequests = new List<MatchRequest>();
            //_configuration = configuration;
            _scopeFactory = scopeFactory;
            _suikaDbService = new SuikaDbService(new LeiaContext());
            _postTournamentService = new PostTournamentService(_suikaDbService);
            //_suikaDbService = new SuikaDbService(new LeiaContext(configuration.GetConnectionString("SuikaDb"))) /*suikaDbService*/;
            _jsonOptions = new()
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                WriteIndented = true,
            };
            _createMatchesFromQueueTask = CreateMatchesFromQueue();

            #region How to hash strings
            //  var byteKey = Encoding.UTF8.GetBytes("some-secret-string");
            // var hashedKey = SHA3_512.HashData(byteKey);
            #endregion

        }



        public async Task CreateMatchesFromQueue()
        {
            // Wait for context
            // Lital: Possibly this is not needed, but research must be done to prove that.
            // Most likely it is not needed since SuikaDbService is already getting a context in its constructor
            // TODO: Check later if this can be removed
            while (_suikaDbService.LeiaContext == null)
            {
                await Task.Delay(NumMilliseconds);
            }
            var context = _suikaDbService.LeiaContext;
            while (true)
            {
            // Create tournament new DbContext instance for this operation

                await Task.Delay(NumMilliseconds);
                await _createMatchesFromQueueSemaphore.WaitAsync(NumMilliseconds);
                try
                {
                    // TODO: After verifying this keeps returning the same context id, there's no need for this debug log to 
                    // contain the context id
                    Debug.WriteLine($"=====> Inside GetMatch. Semaphore was entered with context {context.ContextId}");
                    if (MatchesQueue.Count > 0)
                    {
                        //_strategiesHandler.InitiateStrategies();
                        _currentMatchingStrategy = new CheckFirstRequestStrategy(_suikaDbService, this);
                        while (_currentMatchingStrategy != null)
                        {
                            _currentMatchingStrategy = await _currentMatchingStrategy.RunStrategy();
                        }
                    }
                    // check for waiting requests that are in the list for too long without tournament match
                    else if (WaitingRequests.Count > 0)
                    {
                        _currentMatchingStrategy = new CheckPendingWaitingRequestsStrategy(_suikaDbService, this);
                        while (_currentMatchingStrategy != null)
                        {
                            _currentMatchingStrategy = await _currentMatchingStrategy.RunStrategy();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                    throw;
                }
                finally
                {
                    // Ensure the semaphore is released even if an exception occurs
                    _createMatchesFromQueueSemaphore.Release();
                }
            }
            
        }


        // Matching Strategies driver code
       

        public async Task<bool> FindMatchingTournament(TournamentSession? tournament, params MatchRequest?[] requests)
        {
            var results = requests.Select(async request =>
            {
                if (request != null)
                {
                    var requestBalance = await _suikaDbService.GetPlayerBalance(request?.Player?.PlayerId, request?.MatchFeeCurrency?.CurrencyId);

                    // we check t.Players[0] because the first player in the tournament determines the score that other players should match in that tournament
                    var range = Enumerable.Range(tournament.Players[0].Rating - _scoreVariance, _scoreVarianceSteps).ToList();
                    var doesThePlayerMatch = range.Contains(request.Player.Rating)
                        && request?.MatchFeeCurrency?.CurrencyId == tournament.TournamentData.EntryFeeCurrencyId
                        && requestBalance >= tournament.TournamentData.EntryFee  // check if the matchedRequest.Player't score fits the first player in the ongoing tournament
                        && tournament?.Players?.Any(p => p?.PlayerId == request?.Player?.PlayerId) == false; // makes sure that the player is not already in the tournament
                    Trace.WriteLine($"=====> Inside FindMatchingTournament, doesThePlayerMatch: {doesThePlayerMatch}");
                    return doesThePlayerMatch;
                }
                else return false;
            });

            return results.All(r => r.Result == true);
        }

        public async Task CreateNewTournament(MatchRequest? matchedRequest, MatchRequest? request)
        {

            if (matchedRequest != null)
            {
                var tournament = await SaveNewTournament(matchedRequest.MatchFee, matchedRequest?.MatchFeeCurrency?.CurrencyId, matchedRequest?.TournamentType?.TournamentTypeId, matchedRequest?.Player?.PlayerId, request?.Player?.PlayerId);



                Trace.WriteLine($"Player: {matchedRequest?.Player?.PlayerId}, rating: {matchedRequest?.Player?.Rating},and second player: {request?.Player?.PlayerId}, rating: {request?.Player?.Rating}, \n were added to tournament: {tournament?.TournamentDataId}.");

                MatchesQueue.Remove(request);
                WaitingRequests?.Remove(matchedRequest);

                // make sure the tournament isn't already full
                if (tournament != null)
                {
                    var dbTournament = _suikaDbService.LeiaContext.Tournaments
                            .Include(t => t.TournamentData)
                                .ThenInclude(td => td.TournamentType)
                            .FirstOrDefault(t => t.TournamentSessionId == tournament.TournamentSessionId);

                    if (tournament?.Players?.Count < tournament?.TournamentData?.TournamentType?.NumberOfPlayers)
                    {
                        OngoingTournaments.Add(tournament);
                    }
                }
            }
            else {
                await _suikaDbService.Log("Got null match request");
            }
        }

        public async Task<bool> CheckRequestsMatch(MatchRequest r, MatchRequest request)
        {
            var rBalance = await _suikaDbService.GetPlayerBalance(r?.Player?.PlayerId, r?.MatchFeeCurrency?.CurrencyId);

            var requestBalance = await _suikaDbService.GetPlayerBalance(request?.Player?.PlayerId, request?.MatchFeeCurrency?.CurrencyId);

            var range = Enumerable.Range(r.Player!.Rating - _scoreVariance, _scoreVarianceSteps).ToList();
            var isMatch = range.Contains(request!.Player!.Rating)

                    //  && request?.MatchFee == r.MatchFee // check that both players entered a match on the same fee (e.g. both entered a match on 3$)

                    && rBalance >= r?.MatchFee //! make sure the player has enough money to enter the match. even though a player should not be able to select a match type on the client that he doest have enough    money for
                    && requestBalance >= r.MatchFee // check if the player of the current request has enough money to join the match
                                                    //&& r?.MatchFeeCurrency?.CurrencyId == request?.MatchFeeCurrency?.CurrencyId // check that both players entered with same type of currency
                    && r?.Player?.PlayerId != request?.Player?.PlayerId; // makes sure that the requests are not from the same player

            Trace.WriteLine($"=====> Inside CheckRequestsMatch, isMatch: {isMatch}");
            return isMatch;
        }


        public async Task<bool> ProcessFirstRequest(MatchRequest firstRequest)
        {
            var playerBalance = await _suikaDbService.GetPlayerBalance(firstRequest.Player.PlayerId, firstRequest.MatchFeeCurrency.CurrencyId);
            if (playerBalance >= firstRequest?.MatchFee)//! make sure the player has enough money to create the request. even though a player should not be able to select a match type in the client that he doest have enough money for
            {

                // WaitingRequests?.Add(firstRequest);
                //MatchesQueue.Remove(firstRequest);

                if (OngoingTournaments.Count <= 0) // if there are no open sessions, create a new one
                {
                    var tournament = await SaveNewTournament(firstRequest.MatchFee, firstRequest.MatchFeeCurrency.CurrencyId, firstRequest?.TournamentType?.TournamentTypeId, firstRequest.Player.PlayerId);

                    Debug.WriteLine($"Player: {firstRequest?.Player?.PlayerId}, rating: {firstRequest?.Player?.Rating}, was added to tournament: {tournament?.TournamentSessionId}.");

                    //_session.Players?.Add(request!.Player);
                    OngoingTournaments.Add(tournament);
                    // WaitingRequests.RemoveAt(0); // remove request from the waiting list after moving it into a tournament
                    MatchesQueue.Remove(firstRequest);
                    return true;
                }
                else return false;
            }
            else return false;
        }

        public async Task AddToExistingTournament(MatchRequest? request, MatchRequest? matchedRequest, TournamentSession? matchingTournament)
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
                else if (matchedRequest != null)
                {
                    dbPlayer = _suikaDbService.LeiaContext.Players.Find(matchedRequest?.Player?.PlayerId);

                    
                    //WaitingRequests?.Remove(matchedRequest);
                }
                else
                {
                    var message = "AddToExistingTournament: Got null request AND null MatchRequest";
                    await _suikaDbService.Log(message);
                    Trace.WriteLine(message);
                    return; // Don't change anything
                }
                if (dbPlayer == null)
                {
                    var message = $"AddToExistingTournament: Got null player while attempting to join tournament {dbTournament.TournamentSessionId}";
                    await _suikaDbService.Log(message);
                    Trace.WriteLine(message);
                    return; // Don't change anything
                }
                dbTournament?.Players?.Add(dbPlayer);
                

                var canJoinTournament = await _suikaDbService.SetPlayerActiveTournament(dbPlayer.PlayerId, dbTournament.TournamentSessionId);
                if (!canJoinTournament)
                {
                    var message = $"AddToExistingTournament: Player {dbPlayer.PlayerId} attempted to join tournament {dbTournament.TournamentSessionId}, but was not in matchmaking state!";
                    await _suikaDbService.Log(message, dbPlayer.PlayerId);
                    Trace.WriteLine(message);
                    return;
                }

                // try to update the tournament in the database
                try
                {
                    _suikaDbService.LeiaContext.Entry(dbTournament).State = EntityState.Modified;

                    var savedTournament = _suikaDbService?.LeiaContext?.Tournaments?.Update(dbTournament);
                    var saved = await _suikaDbService?.LeiaContext?.SaveChangesAsync();

                    Trace.WriteLine($"AddToExistingTournament: Player: {matchedRequest?.Player?.Rating}, rating: {matchedRequest?.Player?.Rating}, and second player: {request?.Player?.PlayerId}, rating: {request?.Player?.Rating}, \n were added to tournament: {savedTournament?.Entity?.TournamentSessionId}.");

                    if (saved > 0)//if the tournament was saved to the DB -
                    {
                        // update the tournament in the OngoingTournaments list
                        if (matchedRequest != null)
                        {
                            matchingTournament?.Players.Add(matchedRequest?.Player);
                            matchingTournament?.PlayerTournamentSessions?.Add(new PlayerTournamentSession
                            {
                                PlayerId = matchedRequest.Player.PlayerId,
                                TournamentSessionId = matchingTournament.TournamentSessionId,
                                PlayerScore = null,
                            }
                            );
                            WaitingRequests?.Remove(matchedRequest);
                        }

                        if (request != null)
                        {
                            matchingTournament?.Players.Add(request?.Player);
                            matchingTournament?.PlayerTournamentSessions?.Add(new PlayerTournamentSession
                            {
                                PlayerId = request.Player.PlayerId,
                                TournamentSessionId = matchingTournament.TournamentSessionId,
                                PlayerScore = null,
                            }
                          );
                            MatchesQueue.Remove(request);
                        }
                        // send the tournament seed to controller list
                        SendPlayerAndSeed(savedTournament?.Entity?.TournamentSeed, savedTournament?.Entity?.TournamentSessionId, matchedRequest?.Player?.PlayerId, request?.Player?.PlayerId);
                    }
                }
                catch (Exception ex)
                {
                    var message = $"AddToExistingTournament: Error during attempt of player {dbPlayer.PlayerId} to join tournament {dbTournament.TournamentSessionId}: {ex.Message}";
                    await _suikaDbService.Log(message, dbPlayer.PlayerId);
                    Trace.WriteLine(message);
                    await _suikaDbService.RemovePlayerFromActiveTournament(dbPlayer.PlayerId, dbTournament.TournamentSessionId);
                    Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                }
            }
            if (matchingTournament?.Players?.Count >= request?.TournamentType?.NumberOfPlayers) // if tournament is full, then close it and send it's data
            {
                if (matchingTournament != null)
                {
                    Trace.WriteLine(JsonSerializer.Serialize<TournamentSession>(matchingTournament, _jsonOptions));
                    //todo send matchingTournament data

                    OngoingTournaments.Remove(matchingTournament);
                    //// WaitingRequests?.Remove(matchedRequest);
                }
            }
        }

        // Deprecate this 👇🏻
        public async Task<int?> GetTournamentTypeByCurrency(int? currencyId)
        {
            var currency = _suikaDbService.LeiaContext.Currencies.Find(currencyId);
            var tournamentTypes = _suikaDbService.LeiaContext.TournamentTypes.ToList();
            switch (currency?.CurrencyName)
            {
                case "Gems":
                    return tournamentTypes.FirstOrDefault(tt => tt.TournamentTypeName == "SCFor2Players")?.TournamentTypeId;

                default:
                    return tournamentTypes.FirstOrDefault(tt => tt.TournamentTypeName == "Paid")?.TournamentTypeId;

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
                    var player = _suikaDbService.LeiaContext.Players.Find(id);
                    if (player != null) dbPlayers.Add(player);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                    throw;
                }
            }


            //var tournamentTypeId = await  GetTournamentTypeByCurrency(currencyId);


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

                }
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
                    foreach (var playerId in playerIds)
                    {
                        if (playerId == null) 
                        {
                            continue; 
                        }
                        await _suikaDbService.Log($"SaveNewTournament: Player {playerId} attempts to create tournament {savedTournament.Entity.TournamentSessionId}");
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
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
                //return null;
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
