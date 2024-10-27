
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

namespace Services
{
    public interface ITournamentService
    {
        public List<MatchRequest> MatchesQueue { get; set; }
        public List<MatchRequest> WaitingRequests { get; }
        public List<TournamentSession> OngoingTournaments { get; }
        public System.Timers.Timer MatchTimer { get; set; }
        public event EventHandler PlayerAddedToTournament;
        public Dictionary<Guid?, int?[]> PlayersSeeds { get; set; }

        public Task<bool> FindMatchingTournament(TournamentSession? tournament, params MatchRequest?[] requests);
        public Task CreateNewTournament(MatchRequest? request, MatchRequest? matchedRequest);
        public Task<bool> CheckRequestsMatch(MatchRequest r, MatchRequest request);
        public Task ProcessFirstRequest(MatchRequest firstRequest);
        public Task AddToExistingTournament(MatchRequest? request, MatchRequest? matchedRequest, TournamentSession? matchingTournament);
        public Task<int?> GetTournamentTypeByCurrency(int? currencyId);




    }

    public class TournamentService : ITournamentService
    {
        private int _timerCycles = 0;

        private int _numMilliseconds = 12000; // get these numbers from a DB or config file
        private int _maxNumPlayers = 5; // get these numbers from a DB or config file
        private int _scoreVariance = 200; // get these numbers from a DB or config file 
        private IMatchingStrategy? _currentMatchingStrategy;
        private readonly ISuikaDbService _suikaDbService;
        //private readonly IConfiguration _configuration;
        private JsonSerializerOptions _jsonOptions;
        public event EventHandler PlayerAddedToTournament;

        public Dictionary<Guid?, int?[]> PlayersSeeds { get; set; } = [];
        public System.Timers.Timer MatchTimer { get; set; }
        public List<MatchRequest> MatchesQueue { get; set; }
        public List<MatchRequest> WaitingRequests { get; private set; }
        public List<TournamentSession> OngoingTournaments { get; private set; }

        public TournamentService(/*IConfiguration configuration */)
        {
            MatchesQueue = new List<MatchRequest>();
            OngoingTournaments = new List<TournamentSession>();
            WaitingRequests = new List<MatchRequest>();
            //_configuration = configuration;
            _suikaDbService = new SuikaDbService(new LeiaContext());
            //_suikaDbService = new SuikaDbService(new LeiaContext(configuration.GetConnectionString("SuikaDb"))) /*suikaDbService*/;
            _jsonOptions = new()
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                WriteIndented = true,
            };
            InitTimer();

            #region How to hash strings
            //  var byteKey = Encoding.UTF8.GetBytes("some-secret-string");
            // var hashedKey = SHA3_512.HashData(byteKey);
            #endregion

        }

        private void InitTimer()
        {
            MatchTimer = new System.Timers.Timer
            {
                Interval = _numMilliseconds
            };
            MatchTimer.Elapsed += MatchTimer_Elapsed;
            MatchTimer.AutoReset = true;
        }

        private async void MatchTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Debug.WriteLine("One match cycle ticked");

            //MatchTimer.Interval = _numMilliseconds * 10;
            _timerCycles++;

            ///<summary>
            /// THIS IS IMPORTANT!!!!
            /// the Dispatcher's lambda  SHOULD NOT be asynchronous !!!
            /// if it is, the calls to the database (through the context) happen in parallel. This causes an error because only one context instance can be alive at any given times.
            /// when two parallel calls to the database happen simultaneously with the same context, there are data conflicts.
            /// when we pass a regular function to the InvokeAsync method (not an async lambda) the calls happen sequentially and not simultaneously 
            ///</summary>
            await Dispatcher.CreateDefault().InvokeAsync(/*async () =>*/
            //{
                 GetMatch
                //await GetMatch();
            //}
            );

            //if (_timerCycles > 4) MatchTimer.Stop();
        }


        // Matching Strategies driver code
        public async Task<IEnumerable<TournamentSession>> GetMatch(/*Guid PlayerId*/)
        {

            Debug.WriteLine(JsonSerializer.Serialize<List<MatchRequest>>(MatchesQueue, _jsonOptions));

            if (MatchesQueue.Any())
            {
                //_strategiesHandler.InitiateStrategies();
                _currentMatchingStrategy = new CheckFirstRequestStrategy(_suikaDbService, this);
                while (_currentMatchingStrategy != null)
                {
                    _currentMatchingStrategy = await _currentMatchingStrategy.RunStrategy();
                }
                #region Full Matching logic 
                /* // FIRST REQUEST PART
                 if (WaitingRequests.Count <= 0)// check if there are any waiting requests. if not, add one the to list ( = the first request)
                 {
                     ProcessFirstRequest(MatchesQueue[0]);
                 }
                 // MULTIPLE REQUESTS PART
                 else if (MatchesQueue.Count > 0)
                 {

                     var ordered = MatchesQueue.OrderByDescending(m => m.Player?.Score).ToList();

                     foreach (var request in ordered)
                     {
                         if (WaitingRequests.Any()) // make sure the list is not empty before iterating on it
                         {
                             // FIND MATCH IN WaitingRequests PART
                             var matchedRequest = WaitingRequests?.FirstOrDefault(r => CheckRequestsMatch(r, request).Result); // check if the current request in queue, matches any of the previous requests the in the WaitingRequests list, by Score and if the players have enough money

                             //FOUND A MATCH IN THE WaitingRequests LIST
                             if (matchedRequest != null) // if the players match, add them to a tournament
                             {
                                 if (OngoingTournaments.Count <= 0) // if there is no current tournament, create a new one
                                 {
                                    await CreateNewTournament(request, matchedRequest);
                                 }
                                 else if (OngoingTournaments.Count > 0) // if there is a current tournament, see if the matchedRequest and the request could fit any other open tournaments
                                 {
                                     var matchingTournament = OngoingTournaments.FirstOrDefault(t => FindMatchingTournament(t, request, matchedRequest).Result);

                                     if (matchingTournament == null) // if it doesn't match other tournaments, create a new one
                                     {
                                         await CreateNewTournament(request, matchedRequest);
                                     }
                                     else // meaning there is a fitting tournament for the current request and the found matchedRequest
                                     {
                                         await AddToExistingTournament(request, matchedRequest, matchingTournament);
                                     }
                                 }
                             }

                             // IF NO MATCH IN THE WaitingRequests LIST, FIND MATCH IN OngoingTournaments PART
                             else // if the players don't match, check for a matching ongoing tournament, and if there isn't one, add the requesting player to WaitingRequests
                             {
                                 if (OngoingTournaments.Count > 0) // check if the current request can fit into one of the opened tournaments  
                                 {
                                     var matchingTournament = OngoingTournaments.FirstOrDefault(t => FindMatchingTournament(t, request).Result
                                      // check if the request.Player't score fits the first player in the ongoing tournament, because the first player in the tournament determines the score that other players should match in that tournament
                                      );

                                     //FOUND A MATCH IN THE OngoingTournaments LIST
                                     if (matchingTournament != null) await AddToExistingTournament(request, null, matchingTournament); // if we find a matching tournament, add the request to it

                                     // IF NO MATCH IN OngoingTournaments
                                     else //if not add the request to the WaitingRequests list
                                     {
                                         WaitingRequests?.Add(request);
                                         MatchesQueue.Remove(request);
                                     }

                                 }
                                 else // if there are no ongoing tournaments, add the request to the WaitingRequests list
                                 {
                                     WaitingRequests?.Add(request);
                                     MatchesQueue.Remove(request);
                                 }
                             }
                         }
                     }
                 }*/
                #endregion
            }

            return OngoingTournaments;
        }

        public async Task<bool> FindMatchingTournament(TournamentSession? tournament, params MatchRequest?[] requests)
        {
            var results = requests.Select(async request =>
            {
                if (request != null)
                {
                    var requestBalance = await _suikaDbService.GetPlayerBalance(request?.Player?.PlayerId, request?.MatchFeeCurrency?.CurrencyId);

                    // we check t.Players[0] because the first player in the tournament determines the score that other players should match in that tournament

                    var doesThePlayerMatch = Enumerable.Range(tournament.Players[0].Score - _scoreVariance, tournament.Players[0].Score + _scoreVariance).ToList().Contains(request.Player.Score)
                        && request?.MatchFeeCurrency?.CurrencyId == tournament.TournamentData.EntryFeeCurrencyId
                        && requestBalance >= tournament.TournamentData.EntryFee;  // check if the matchedRequest.Player't score fits the first player in the ongoing tournament
                    return doesThePlayerMatch;
                }
                else return false;
            });

            return results.All(r => r.Result == true);
        }

        public async Task CreateNewTournament(MatchRequest? request, MatchRequest? matchedRequest)
        {
            /*var currenyType = await _suikaDbService.LeiaContext.Currencies.FindAsync(matchedRequest?.MatchFeeCurrency?.CurrencyId);
            var tournament1 = new TournamentSession
            {
                TournamentData = new TournamentData
                {
                    EntryFee = matchedRequest.MatchFee, // we create a tournament according to the matched request, because the WaitingRequests take precedence over the new ones  
                    EntryFeeCurrency = currenyType
                    //todo 👉🏻👉🏻👉🏻 add a time frame (5 min) to a tournament, and then close it 
                }
            };
            tournament1.Players?.AddRange(new List<Player> { matchedRequest!.Player, request!.Player });
*/

            var tournament = await SaveNewTournament(matchedRequest.MatchFee, matchedRequest?.MatchFeeCurrency?.CurrencyId, matchedRequest?.Player?.PlayerId, request?.Player?.PlayerId);

            Debug.WriteLine($"Player: {matchedRequest!.Player?.PlayerId}, score: {matchedRequest!.Player?.Score},and second player: {request!.Player?.PlayerId}, score: {request!.Player?.Score}, \n were added to tournament: {tournament?.TournamentDataId}.");

            MatchesQueue.Remove(request);
            WaitingRequests?.Remove(matchedRequest);
            OngoingTournaments.Add(tournament);

        }

        public async Task<bool> CheckRequestsMatch(MatchRequest r, MatchRequest request)
        {
            var rBalance = await _suikaDbService.GetPlayerBalance(r?.Player?.PlayerId, r?.MatchFeeCurrency?.CurrencyId);

            var requestBalance = await _suikaDbService.GetPlayerBalance(request?.Player?.PlayerId, request?.MatchFeeCurrency?.CurrencyId);

            return Enumerable.Range(r.Player!.Score - _scoreVariance, r.Player!.Score + _scoreVariance).ToList().Contains(request!.Player!.Score)

                                        //  && request?.MatchFee == r.MatchFee // check that both players entered a match on the same amount (e.g. both entered a match on 3$)

                                        && rBalance >= r?.MatchFee //! make sure the player has enough money to enter the match. even though a player should not be able to select a match type on the client that he doest have enough money for

                                         && requestBalance >= r.MatchFee // check if the player of the current request has enough money to join the match
                                          && r?.MatchFeeCurrency?.CurrencyId == request?.MatchFeeCurrency?.CurrencyId; // check that both players entered with same type of currency


        }

        public async Task ProcessFirstRequest(MatchRequest firstRequest)
        {
            var playerBalance = await _suikaDbService.GetPlayerBalance(firstRequest.Player.PlayerId, firstRequest.MatchFeeCurrency.CurrencyId);
            if (playerBalance >= firstRequest?.MatchFee)//! make sure the player has enough money to create the request. even though a player should not be able to select a match type in the client that he doest have enough money for
            {

                WaitingRequests?.Add(firstRequest);
                MatchesQueue.Remove(firstRequest);

                if (OngoingTournaments.Count <= 0) // if there are no open sessions, create a new one
                {
                    var tournament = await SaveNewTournament(firstRequest.MatchFee, firstRequest.MatchFeeCurrency.CurrencyId, firstRequest.Player.PlayerId);

                    Debug.WriteLine($"Player: {firstRequest?.Player?.PlayerId}, score: {firstRequest?.Player?.Score}, was added to tournament: {tournament?.TournamentSessionId}.");

                    //_session.Players?.Add(request!.Player);
                    OngoingTournaments.Add(tournament);
                    WaitingRequests.RemoveAt(0); // remove request from the waiting list after moving it into a tournament

                }

            }
        }

        public async Task AddToExistingTournament(MatchRequest? request, MatchRequest? matchedRequest, TournamentSession? matchingTournament)
        {
            if (matchingTournament?.Players?.Count < _maxNumPlayers) // if tournament has room in it, add the current request player
            {
                var dbTournament = _suikaDbService.LeiaContext.Tournaments.Find(matchingTournament?.TournamentSessionId);
                if (matchedRequest != null)
                {
                    var dbPlayer = _suikaDbService.LeiaContext.Players.Find(matchedRequest?.Player?.PlayerId);

                    if (dbPlayer != null) dbTournament?.Players?.Add(dbPlayer);
                    WaitingRequests?.Remove(matchedRequest);
                }

                if (request != null)
                {
                    var dbPlayer = _suikaDbService.LeiaContext.Players.Find(request?.Player?.PlayerId);

                    if (dbPlayer != null) dbTournament?.Players?.Add(dbPlayer);
                    MatchesQueue.Remove(request);
                }

                try
                {
                    _suikaDbService.LeiaContext.Entry(dbTournament).State = EntityState.Modified;

                    var savedTournament = _suikaDbService?.LeiaContext?.Tournaments?.Update(dbTournament);
                    var saved = await _suikaDbService?.LeiaContext?.SaveChangesAsync();

                    Debug.WriteLine($"Player: {matchedRequest?.Player?.PlayerId}, score: {matchedRequest?.Player?.Score},and second player: {request?.Player?.PlayerId}, score: {request?.Player?.Score}, \n were added to tournament: {savedTournament?.Entity?.TournamentSessionId}.");

                    if (saved > 0) SendPlayerAndSeed(savedTournament?.Entity?.TournamentSeed, savedTournament?.Entity?.TournamentSessionId, matchedRequest.Player.PlayerId, request.Player.PlayerId);

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                }

            }
            if (matchingTournament?.Players?.Count >= _maxNumPlayers) // if tournament is full, then close it and send it's data
            {
                if (matchingTournament != null)
                {
                    Debug.WriteLine(JsonSerializer.Serialize<TournamentSession>(matchingTournament, _jsonOptions));
                    //todo send matchingTournament data

                    OngoingTournaments.Remove(matchingTournament);
                    //// WaitingRequests?.Remove(matchedRequest);
                }
            }
        }

        public async Task<int?> GetTournamentTypeByCurrency(int? currencyId)
        {
            var currency = await _suikaDbService.LeiaContext.Currencies.FindAsync(currencyId);
            var tournamentTypes = _suikaDbService.LeiaContext.TournamentTypes.ToList();
            switch (currency?.CurrencyName)
            {
                case "Gems":
                    return tournamentTypes.FirstOrDefault(tt => tt.TournamentTypeName == "Free")?.TournamentTypeId;

                default:
                    return tournamentTypes.FirstOrDefault(tt => tt.TournamentTypeName == "Paid")?.TournamentTypeId;

            }

        }

        public async Task<TournamentSession?> SaveNewTournament(double matchFee, int? currencyId, params Guid?[]? playerIds)
        {


            var currency = await _suikaDbService.LeiaContext.Currencies.FindAsync(currencyId);

            var dbPlayers = playerIds?.Select(id =>
            {
                return _suikaDbService.LeiaContext.Players.Find(id);
            }).ToList();

            var tournamentTypeId = await GetTournamentTypeByCurrency(currencyId);


            var tournament = new TournamentSession
            {
                TournamentSeed = new Random().Next(),
                TournamentData = new TournamentData
                {
                    EntryFee = matchFee,
                    EntryFeeCurrencyId = currency.CurrencyId,
                    EarningCurrencyId = currency.CurrencyId,
                    TournamentTypeId = (int)tournamentTypeId,
                    TournamentStart = DateTime.Now,
                    TournamentEnd = DateTime.Now.AddDays(1),

                }
            };
            tournament.Players?.AddRange(dbPlayers);
            _suikaDbService.LeiaContext.Entry(currency).State = EntityState.Detached;

            try
            {

                var savedTournament =  _suikaDbService?.LeiaContext?.Tournaments?.Add(tournament);
                var saved =  await _suikaDbService?.LeiaContext?.SaveChangesAsync();

                var idsArray = dbPlayers?.Select(p => p.PlayerId).ToArray();
                if (saved > 0) SendPlayerAndSeed(savedTournament?.Entity?.TournamentSeed, savedTournament?.Entity?.TournamentSessionId, idsArray); 
                return savedTournament?.Entity;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
                //return null;
            }

        }

        private void SendPlayerAndSeed(int? seed, int? tournamentId, params Guid[] playerIds)
        {
            var data = (Seed: seed, TournamentId: tournamentId, Ids: playerIds);
            PlayerAddedToTournament?.Invoke(data, new EventArgs());
        }

        public async Task CloseTimedOutMatches()
        {
            //todo check for sessions who are older than 12 hours and close them
            //todo send event telling the tournament was closed 
            throw new NotImplementedException();
        }

    }
}
