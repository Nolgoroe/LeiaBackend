using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DataObjects;

namespace Services.MatchMakerStrategies
{
    public class MultipleRequestsStrategy : IMatchingStrategy
    {
        private readonly ISuikaDbService _suikaDbService;
        private readonly ITournamentService _tournamentService;

        public MultipleRequestsStrategy(ISuikaDbService suikaDbService, ITournamentService tournamentService)
        {
            _suikaDbService = suikaDbService;
            _tournamentService = tournamentService;
        }

        public async Task<IMatchingStrategy?> RunStrategy()
        {
            var ordered = _tournamentService.MatchesQueue.OrderByDescending(m => m.Player?.Rating).ToList();

            foreach (var request in ordered)
            {
                if (_tournamentService.WaitingRequests.Any()) // make sure the list is not empty before iterating on it
                {
                    // FIND MATCH IN WaitingRequests PART
                    var matchedRequest = _tournamentService.WaitingRequests?.FirstOrDefault(r => _tournamentService.CheckRequestsMatch(r, request).Result); // check if the current request in queue, matches any of the previous requests the in the WaitingRequests list, by Score and if the players have enough money

                    //FOUND A MATCH IN THE WaitingRequests LIST
                    if (matchedRequest != null) // if the players match, add them to a tournament
                    {
                        /* if (_tournamentService.OngoingTournaments.Count <= 0) // if there is no current tournament, create a new one
                         {
                             SaveNewTournament(request, matchedRequest);
                         }
                         else if (_tournamentService.OngoingTournamentss.Count > 0) // if there is a current tournament, see if the matchedRequest and the request could fit any other open tournaments
                         {
                             var matchingTournament = _tournamentService.OngoingTournaments.FirstOrDefault(t => FindMatchingTournament(t, request, matchedRequest).Result);

                             if (matchingTournament == null) // if it doesn't match other tournaments, create a new one
                             {
                                 SaveNewTournament(request, matchedRequest);
                             }
                             else // meaning there is a fitting tournament for the current request and the found matchedRequest
                             {
                                 AddToExistingTournament(request, matchedRequest, matchingTournament);
                             }
                         }*/
                        
                        return new FindMatchInWaitingRequestsStrategy(_suikaDbService, _tournamentService, request, matchedRequest);
                    }

                    // IF NO MATCH IN THE WaitingRequests LIST, FIND MATCH IN OngoingTournaments PART 
                    else // if the players don't match, check for a matching ongoing tournament, and if there isn't one, add the requesting player to WaitingRequests
                    {
                        /* if (_tournamentService.OngoingTournaments.Count > 0) // check if the current request can fit into one of the opened tournaments  
                         {
                             var matchingTournament = _tournamentService.OngoingTournaments.FirstOrDefault(t => FindMatchingTournament(t, request).Result
                              // check if the request.Player't score fits the first player in the ongoing tournament, because the first player in the tournament determines the score that other players should match in that tournament
                              );

                             //FOUND A MATCH IN THE OngoingTournaments LIST
                             if (matchingTournament != null) AddToExistingTournament(request, null, matchingTournament); // if we find a matching tournament, add the request to it

                             // IF NO MATCH IN OngoingTournaments
                             else //if not add the request to the WaitingRequests list
                             {
                                 _tournamentService.WaitingRequests?.Add(request);
                                 _tournamentService.MatchesQueue.Remove(request);
                             }

                         }
                         else // if there are no ongoing tournaments, add the request to the WaitingRequests list
                         {
                             _tournamentService.WaitingRequests?.Add(request);
                             _tournamentService.MatchesQueue.Remove(request);
                         }*/
                        return new FindMatchInOngoingTournamentsStrategy(_suikaDbService, _tournamentService,request);
                    }
                }
            }
            return null;
        }
    }
}
