using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Azure.Core;

using DataObjects;

namespace Services.MatchMakerStrategies
{
    public class FindMatchInWaitingRequestsStrategy : IMatchingStrategy
    {
        private readonly ISuikaDbService _suikaDbService;
        private readonly ITournamentService _tournamentService;
        private readonly MatchRequest[]? _requests;

        public FindMatchInWaitingRequestsStrategy(ISuikaDbService suikaDbService, ITournamentService tournamentService, params MatchRequest[]? requests)
        {
            _suikaDbService = suikaDbService;
            _tournamentService = tournamentService;
            _requests = requests;
        }
        public async Task<IMatchingStrategy?> RunStrategy()
        {
            //FOUND A MATCH IN THE WaitingRequests LIST
            // if the players match, add them to a tournament

            if (_tournamentService.OngoingTournaments.Count <= 0) // if there is no current tournament, create a new one
            {
                await _tournamentService.CreateNewTournament(_requests?[0], _requests?[1]);

            }
            else if (_tournamentService.OngoingTournaments.Count > 0) // if there is a current tournament, see if the matchedRequest and the request could fit any other open tournaments
            {
                var matchingTournament = _tournamentService.OngoingTournaments.FirstOrDefault(t =>  _tournamentService.FindMatchingTournament(t, _requests).Result);

                if (matchingTournament == null) // if it doesn't match other tournaments, create a new one
                {
                   await  _tournamentService.CreateNewTournament(_requests?[0], _requests?[1]);
                }
                else // meaning there is a fitting tournament for the current request and the found matchedRequest
                {
                    await _tournamentService.AddToExistingTournament(_requests?[0], _requests?[1], matchingTournament);
                }
            }
            return null;
        }

    }
}
