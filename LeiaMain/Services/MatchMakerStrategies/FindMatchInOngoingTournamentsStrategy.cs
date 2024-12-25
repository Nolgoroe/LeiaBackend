using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Azure.Core;

using DataObjects;

namespace Services.MatchMakerStrategies
{
    public class FindMatchInOngoingTournamentsStrategy : IMatchingStrategy
    {
        private readonly ISuikaDbService _suikaDbService;
        private readonly ITournamentService _tournamentService;
        private readonly MatchRequest[]? _requests;

        public FindMatchInOngoingTournamentsStrategy(ISuikaDbService suikaDbService, ITournamentService tournamentService, params MatchRequest[]? requests)
        {
            _suikaDbService = suikaDbService;
            _tournamentService = tournamentService;
            _requests = requests;
        }
        public async Task<IMatchingStrategy?> RunStrategy()
        {
            if (_tournamentService.OngoingTournaments.Count > 0) // check if the current request can fit into one of the opened tournaments  
            {
                // check if the request.Player't score fits the first player in the ongoing tournament, because the first player in the tournament determines the score that other players should match in that tournament
                var matchingTournament = _tournamentService.OngoingTournaments
                    .FirstOrDefault(t => _tournamentService.FindMatchingTournament(t, _requests).Result);

                //FOUND A MATCH IN THE OngoingTournaments LIST
                if (matchingTournament != null)
                {
                    await _tournamentService.AddToExistingTournament(_requests?[0], null, matchingTournament); // if we find a matching tournament, add the request to it
                }
                // IF NO MATCH IN OngoingTournaments
                else //if not add the request to the WaitingRequests list
                {
                    _tournamentService.WaitingRequests.Add(_requests[0]);
                    _tournamentService.MatchesQueue.Remove(_requests[0]);
                }

            }
            else // if there are no ongoing tournaments, add the request to the WaitingRequests list
            {
                Trace.WriteLine("=====> Inside no ongoing tournaments, add the request to the WaitingRequests list");
                _tournamentService.WaitingRequests.Add(_requests[0]);
                _tournamentService.MatchesQueue.Remove(_requests[0]);
            }
            return null;
        }
    }
}
