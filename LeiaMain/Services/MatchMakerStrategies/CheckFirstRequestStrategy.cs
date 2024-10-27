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
                 _tournamentService.ProcessFirstRequest(_tournamentService.MatchesQueue[0]);
                return null;
            }
            // MULTIPLE REQUESTS PART
            else if (_tournamentService.MatchesQueue.Count > 0)
            {
                return new MultipleRequestsStrategy(_suikaDbService, _tournamentService);
            }
            return null;

        }

    }
}
