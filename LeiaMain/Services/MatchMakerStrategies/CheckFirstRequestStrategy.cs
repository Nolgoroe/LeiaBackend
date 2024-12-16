using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DataObjects;

namespace Services.MatchMakerStrategies
{
    public class CheckFirstRequestStrategy : IMatchingStrategy
    {
        private readonly ISuikaDbService _suikaDbService;
        private readonly ITournamentService _tournamentService;

        public CheckFirstRequestStrategy(ISuikaDbService suikaDbService, ITournamentService tournamentService)
        {
            _suikaDbService = suikaDbService;
            _tournamentService = tournamentService;
        }

        public async Task<IMatchingStrategy?> RunStrategy()
        {
            // FIRST REQUEST PART
            if (_tournamentService.WaitingRequests.Count <= 0)// check if there are any waiting requests. if not, add one the to list ( = the first request)
            {
                Trace.WriteLine("=====> Inside FirstRequestStrategy ➡️ WaitingRequests.Count <= 0");
                
                var isFirstTournament = await _tournamentService.ProcessFirstRequest(_tournamentService.MatchesQueue[0]);
                if (isFirstTournament) return null;
                else return new FindMatchInOngoingTournamentsStrategy(_suikaDbService, _tournamentService, _tournamentService.MatchesQueue[0]);

            }
            // MULTIPLE REQUESTS PART
            else if (_tournamentService.MatchesQueue.Count > 0)
            {
                Trace.WriteLine("=====> Inside FirstRequestStrategy ➡️ MatchesQueue.Count > 0");
                return new MultipleRequestsStrategy(_suikaDbService, _tournamentService);
            }
            Trace.WriteLine("=====> Inside FirstRequestStrategy ➡️  return null");
            return null;

        }

    }
}
