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
            // 👇🏻 this is to make sure that we won't get an out of range exception
            var request1 = _requests?.ElementAtOrDefault(0);
            var request2 = _requests?.ElementAtOrDefault(1);
            //FOUND A MATCH IN THE WaitingRequests LIST
            // if the players match, add them to a tournament

            if (request1?.Player?.PlayerId == request2?.Player?.PlayerId) return null;// make sure that both request are not from the same player
            if (_tournamentService.OngoingTournaments.Count <= 0) // if there is no current tournament, create a new one
            {
                await _tournamentService.CreateNewTournament(request1, request2);

            }
            else if (_tournamentService.OngoingTournaments.Count > 0) // if there is a current tournament, see if the matchedRequest and the request could fit any other open tournaments
            {
                var matchingTournament = _tournamentService.OngoingTournaments.FirstOrDefault(t => _tournamentService.FindMatchingTournament(t, _requests).Result);

                if (matchingTournament == null) // if it doesn't match other tournaments, create a new one
                {
                    await _tournamentService.CreateNewTournament(request1, request2);
                }
                else // meaning there is a fitting tournament for the current request and the found matchedRequest
                {
                    await _tournamentService.AddToExistingTournament(request1, request2, matchingTournament);
                }
            }
            return null;
        }

    }
}
