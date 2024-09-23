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

        public IMatchingStrategy? RunStrategy()
        {
            // FIRST REQUEST PART
            if (_tournamentService.WaitingRequests.Count <= 0)// check if there are any waiting requests. if not, add one the to list ( = the first request)
            {
                ProcessFirstRequest(_tournamentService.WaitingRequests[0]);
                return null;
            }
            // MULTIPLE REQUESTS PART
            else if (_tournamentService.MatchesQueue.Count > 0)
            {
                return new MultipleRequestsStrategy(_suikaDbService, _tournamentService);
            }
            return null;

        }

        private async void ProcessFirstRequest(MatchRequest firstRequest)
        {
            var playerBalance = await _suikaDbService.GetPlayerBalance(firstRequest.Player.PlayerId, firstRequest.MatchFeeCurrency.CurrencyId);
            if (playerBalance >= firstRequest?.MatchFee)//! make sure the player has enough money to create the request. even though a player should not be able to select a match type in the client that he doest have enough money for
            {

                _tournamentService.WaitingRequests?.Add(firstRequest);
                _tournamentService.MatchesQueue.Remove(firstRequest);

                if (_tournamentService.OngoingTournaments.Count <= 0) // if there are no open sessions, create a new one
                {
                    var tournament = new TournamentSession
                    {
                        TournamentData = new TournamentData
                        {
                            EntryFee = firstRequest.MatchFee
                        }
                    };
                    try
                    {
                        tournament.Players?.Add(firstRequest?.Player);

                        var savedTournament = await _suikaDbService.LeiaContext.Tournaments.AddAsync(tournament);
                        var saved = await _suikaDbService.LeiaContext.SaveChangesAsync();

                        Debug.WriteLine($"Player: {firstRequest?.Player?.PlayerId}, score: {firstRequest?.Player?.Score}, was added to tournament: {tournament?.TournamentSessionId}.");

                        //_session.Players?.Add(request!.Player);
                        _tournamentService.OngoingTournaments.Add(tournament);
                        _tournamentService?.WaitingRequests?.RemoveAt(0); // remove request from the waiting list after moving it into a tournament

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }

                }

            }
        }

    }
}
