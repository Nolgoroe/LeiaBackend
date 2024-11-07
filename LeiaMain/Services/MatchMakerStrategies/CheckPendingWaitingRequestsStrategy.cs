
using Azure.Core;

using DataObjects;

namespace Services.MatchMakerStrategies
{
    public class CheckPendingWaitingRequestsStrategy : IMatchingStrategy
    {
        private readonly ISuikaDbService _suikaDbService;
        private readonly ITournamentService _tournamentService;

        public CheckPendingWaitingRequestsStrategy(ISuikaDbService suikaDbService, ITournamentService tournamentService)
        {
            _suikaDbService = suikaDbService;
            _tournamentService = tournamentService;
        }
        public async Task<IMatchingStrategy?> RunStrategy()
        {
            if (_tournamentService.WaitingRequests.Count > 0) // make sure the list has members in it before iterating on it
            {
                var oldRequests = _tournamentService?.WaitingRequests?.Where(r => r?.RequestTime.AddMilliseconds(_tournamentService.NumMilliseconds*2) <= DateTime.Now).ToList();

                for (int i = 0; i < oldRequests?.Count; i++)
                {
                    var request1 = oldRequests.ElementAtOrDefault(i);
                    var request2 = oldRequests.ElementAtOrDefault(i + 1);
                    var strategy = new FindMatchInWaitingRequestsStrategy(_suikaDbService, _tournamentService, request1, request2);

                    await strategy?.RunStrategy();
                }
            }

            return null;
        }
    }
}