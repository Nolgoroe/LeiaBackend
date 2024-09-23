
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

namespace Services
{
    public interface ITournamentService
    {
        public List<MatchRequest> MatchesQueue { get; set; }
        public List<MatchRequest> WaitingRequests { get; }
        public List<TournamentSession> OngoingTournaments { get; }
        public System.Timers.Timer MatchTimer { get; set; }

        public Task<bool> CheckRequestsMatch(MatchRequest r, MatchRequest request);
        public void AddToExistingTournament(MatchRequest? request, MatchRequest? matchedRequest, TournamentSession? matchingTournament);
        public void CreateNewTournament(MatchRequest? request, MatchRequest? matchedRequest);
        public Task<bool> FindMatchingTournament(TournamentSession? tournament, params MatchRequest?[] requests);




    }

    public class TournamentService : ITournamentService
    {
        private int _numMilliseconds = 6000; // get these numbers from a DB or config file
        private int _maxNumPlayers = 4; // get these numbers from a DB or config file
        private int _scoreVariance = 200; // get these numbers from a DB or config file 
        private IMatchingStrategy? _currentMatchingStrategy;
        private readonly ISuikaDbService _suikaDbService;

        public System.Timers.Timer MatchTimer { get; set; }
        public List<MatchRequest> MatchesQueue { get; set; }
        public List<MatchRequest> WaitingRequests { get; private set; }
        public List<TournamentSession> OngoingTournaments { get; private set; }


        public TournamentService(ISuikaDbService suikaDbService)
        {
            MatchesQueue = new List<MatchRequest>();
            OngoingTournaments = new List<TournamentSession>();
            WaitingRequests = new List<MatchRequest>();
            /*
            playerQueue.Enqueue(new Player { PlayerId = 4, Rating = 22, Score = 1000 });
            playerQueue.Enqueue(new Player { PlayerId = 7, Rating = 32, Score = 500 });
            playerQueue.Enqueue(new Player { PlayerId = 3, Rating = 21, Score = 1100 });
            playerQueue.Enqueue(new Player { PlayerId = 5, Rating = 23, Score = 900 });
            playerQueue.Enqueue(new Player { PlayerId = 9, Rating = 34, Score = 400 });
            playerQueue.Enqueue(new Player { PlayerId = 2, Rating = 12, Score = 1400 });
            playerQueue.Enqueue(new Player { PlayerId = 8, Rating = 33, Score = 400 });
            playerQueue.Enqueue(new Player { PlayerId = 1, Rating = 11, Score = 1500 });
            playerQueue.Enqueue(new Player { PlayerId = 6, Rating = 31, Score = 600 });
            */
            InitTimer();
            _suikaDbService = suikaDbService;

            #region How to hash strings
            //  var byteKey = Encoding.UTF8.GetBytes("some-secret-string");
            // var hashedKey = SHA3_512.HashData(byteKey);
            #endregion

        }

        private void InitTimer()
        {
            MatchTimer = new System.Timers.Timer();
            MatchTimer.Interval = _numMilliseconds;
            MatchTimer.Elapsed += MatchTimer_Elapsed;
            MatchTimer.AutoReset = true;
        }

        private async void MatchTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Debug.WriteLine("One match cycle ticked");


            await Dispatcher.CreateDefault().InvokeAsync(async () =>
            {
                await GetMatch();
            });
        }


        // Matching Strategies driver code
        public async Task<IEnumerable<TournamentSession>> GetMatch(/*Guid PlayerId*/)
        {
            Debug.WriteLine(JsonSerializer.Serialize(MatchesQueue));

            if (MatchesQueue.Any())
            {
                _currentMatchingStrategy = new CheckFirstRequestStrategy(_suikaDbService, this);
                while (_currentMatchingStrategy != null)
                {
                    _currentMatchingStrategy = _currentMatchingStrategy.RunStrategy();
                }
                // FIRST REQUEST PART
                /*  if (WaitingRequests.Count <= 0)// check if there are any waiting requests. if not, add one the to list ( = the first request)
                  {
                       ProcessFirstRequest(WaitingRequests[0]);

                  }*/
                // MULTIPLE REQUESTS PART
                /* else if (MatchesQueue.Count > 0)
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
                                    CreateNewTournament(request, matchedRequest);
                                }
                                else if (OngoingTournaments.Count > 0) // if there is a current tournament, see if the matchedRequest and the request could fit any other open tournaments
                                {
                                    var matchingTournament = OngoingTournaments.FirstOrDefault(t => FindMatchingTournament(t, request, matchedRequest).Result);

                                    if (matchingTournament == null) // if it doesn't match other tournaments, create a new one
                                    {
                                        CreateNewTournament(request, matchedRequest);
                                    }
                                    else // meaning there is a fitting tournament for the current request and the found matchedRequest
                                    {
                                        AddToExistingTournament(request, matchedRequest, matchingTournament);
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
                                    if (matchingTournament != null) AddToExistingTournament(request, null, matchingTournament); // if we find a matching tournament, add the request to it

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

                    var doesThePlayerMatch = Enumerable.Range(tournament.Players[0].Score - _scoreVariance, tournament.Players[0].Score + _scoreVariance).ToList().Contains(request.Player!.Score)
                        && request?.MatchFeeCurrency?.CurrencyId == tournament.TournamentData.EntryFeeCurrency.CurrencyId
                        && requestBalance >= tournament.TournamentData.EntryFee;  // check if the matchedRequest.Player't score fits the first player in the ongoing tournament
                    return doesThePlayerMatch;
                }
                else return false;
            });

            return results.All(r => r.Result == true);
        }

        public async void CreateNewTournament(MatchRequest? request, MatchRequest? matchedRequest)
        {
            var tournament = new TournamentSession
            {
                TournamentData = new TournamentData
                {
                    EntryFee = matchedRequest.MatchFee // we create a tournament according to the matched request, because the WaitingRequests take precedence over the new ones  
                                                       //todo 👉🏻👉🏻👉🏻 add a time frame (5 min) to a tournament, and then close it 
                }
            };
            tournament.Players?.AddRange(new List<Player> { matchedRequest!.Player, request!.Player });

            Debug.WriteLine($"Player: {matchedRequest!.Player?.PlayerId}, score: {matchedRequest!.Player?.Score},and second player: {request!.Player?.PlayerId}, score: {request!.Player?.Score}, \n were added to tournament: {tournament?.TournamentDataId}.");

            MatchesQueue.Remove(request);
            WaitingRequests?.Remove(matchedRequest);
            OngoingTournaments.Add(tournament);

            // save tournament to DB
            try
            {
                var entity = await _suikaDbService.LeiaContext.Tournaments.AddAsync(tournament);
                var saved = await _suikaDbService.LeiaContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }

        public async Task<bool> CheckRequestsMatch(MatchRequest r, MatchRequest request)
        {
            var rBalance = await _suikaDbService.GetPlayerBalance(r?.Player?.PlayerId, r?.MatchFeeCurrency?.CurrencyId);

            var requestBalance = await _suikaDbService.GetPlayerBalance(request?.Player?.PlayerId, request?.MatchFeeCurrency?.CurrencyId);

            return Enumerable.Range(r.Player!.Score - _scoreVariance, r.Player!.Score + _scoreVariance).ToList().Contains(request!.Player!.Score)

                                        //  && request?.MatchFee == r.MatchFee // check that both players entered a match on the same amount (e.g. both entered a match on 3$)

                                        && rBalance >= r?.MatchFee //! make sure the player has enough money to enter the match. even though a player should not be able to select a match type in the client that he doest have enough money for

                                         && requestBalance >= r.MatchFee; // check if the player of the current request has enough money to join the match


        }

        private async void ProcessFirstRequest(MatchRequest firstRequest)
        {
            var playerBalance = await _suikaDbService.GetPlayerBalance(firstRequest.Player.PlayerId, firstRequest.MatchFeeCurrency.CurrencyId);
            if (playerBalance >= firstRequest?.MatchFee)//! make sure the player has enough money to create the request. even though a player should not be able to select a match type in the client that he doest have enough money for
            {

                WaitingRequests?.Add(firstRequest);
                MatchesQueue.Remove(firstRequest);

                if (OngoingTournaments.Count <= 0) // if there are no open sessions, create a new one
                {
                    var tournament = new TournamentSession
                    {
                        TournamentData = new TournamentData
                        {
                            EntryFee = firstRequest.MatchFee
                        }
                    };
                    tournament.Players?.Add(firstRequest?.Player);

                    var savedTournament = await _suikaDbService.LeiaContext.Tournaments.AddAsync(tournament);
                    var saved = await _suikaDbService.LeiaContext.SaveChangesAsync();

                    Debug.WriteLine($"Player: {firstRequest?.Player?.PlayerId}, score: {firstRequest?.Player?.Score}, was added to tournament: {tournament?.TournamentSessionId}.");

                    //_session.Players?.Add(request!.Player);
                    OngoingTournaments.Add(tournament);
                    WaitingRequests.RemoveAt(0); // remove request from the waiting list after moving it into a tournament

                }

            }
        }

        public void AddToExistingTournament(MatchRequest? request, MatchRequest? matchedRequest, TournamentSession? matchingTournament)
        {
            if (matchingTournament?.Players?.Count < _maxNumPlayers) // if tournament has room in it, add the current request player
            {
                if (matchedRequest != null)
                {
                    matchingTournament?.Players?.Add(matchedRequest?.Player);
                    WaitingRequests?.Remove(matchedRequest);
                }

                if (request != null)
                {
                    matchingTournament?.Players?.Add(request?.Player);
                    MatchesQueue.Remove(request);
                }

                // Debug.WriteLine($"Player: {request!.Player?.PlayerId}, score: {request!.Player?.Score}, was added to tournament: {matchingTournament?.SessionId}.");

                Debug.WriteLine($"Player: {matchedRequest?.Player?.PlayerId}, score: {matchedRequest?.Player?.Score},and second player: {request?.Player?.PlayerId}, score: {request?.Player?.Score}, \n were added to tournament: {matchingTournament?.TournamentSessionId}.");


                //todo check why  when the last player is sent he doesn't get added to the tournament, and why when sent again, he gets added twice 
            }
            if (matchingTournament?.Players?.Count >= _maxNumPlayers) // if tournament is full, than close it and send it't data
            {
                if (matchingTournament != null)
                {
                    Debug.WriteLine(JsonSerializer.Serialize(matchingTournament));
                    //todo send matchingTournament data

                    OngoingTournaments.Remove(matchingTournament);
                    //// WaitingRequests?.Remove(matchedRequest);
                }
            }
        }

        public async Task CloseTimedOutMatches()
        {
            //todo check for sessions who are older than 12 hours and close them
            //todo send event telling the tournament was closed 
            throw new NotImplementedException();
        }

    }
}
