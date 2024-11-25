using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DAL;

using DataObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Services
{
    public interface IPostTournamentService
    {
        public Task CloseTournament(TournamentSession tournament);
        //public ISuikaDbService SuikaDbService { get; set; }

    }


    public class PostTournamentService : IPostTournamentService
    {
        private readonly ISuikaDbService _suikaDbService;
        //public ISuikaDbService SuikaDbService { get; set; }

        public PostTournamentService(ISuikaDbService suikaDbService)
        {
            //SuikaDbService = suikaDbService;
            _suikaDbService = new SuikaDbService(new LeiaContext()); ;
        }


        public async Task CloseTournament(TournamentSession? tournament)
        {
            ArgumentNullException.ThrowIfNull(tournament);
            if (tournament is null) Trace.WriteLine($"In CloseTournament, tournament: {tournament?.TournamentSessionId}, was null");

            tournament.IsOpen = false;
            try
            {
                _suikaDbService.LeiaContext.Entry(tournament).State = EntityState.Modified;
                var updatedPlayerTournament = _suikaDbService.LeiaContext.Tournaments.Update(tournament);

                var saved = await _suikaDbService.LeiaContext.SaveChangesAsync();
                if (saved > 0)
                {
                    GrantRewards(updatedPlayerTournament.Entity);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }
        }

        private void GrantRewards(TournamentSession? tournament)
        {
            if (tournament == null)
            {
                Trace.WriteLine($"In PostTournamentService.GrantRewards, tournament: {tournament?.TournamentSessionId}, was null");
                return;
            }
            var dbTournamrent = _suikaDbService.LeiaContext.Tournaments.Where(t => t.TournamentSessionId == tournament.TournamentSessionId)
                 .Include(t => t.Players)
                 .Include(t => t.PlayerTournamentSessions)
                 .FirstOrDefault();

            if (dbTournamrent == null)
            {
                Trace.WriteLine($"In PostTournamentService.GrantRewards, dbTournamrent: {dbTournamrent?.TournamentSessionId}, was null");
                return;
            }

            var playersByScore = dbTournamrent?.PlayerTournamentSessions.OrderByDescending(pt => pt.PlayerScore).Select(pt =>
            {
                return dbTournamrent?.Players.FirstOrDefault(p => p.PlayerId == pt.PlayerId);
            })/*.ToList()*/;

            var rewards = _suikaDbService.LeiaContext.TournamentTypes.Where(tt => tt.TournamentTypeId == dbTournamrent.TournamentData.TournamentTypeId)
                .Include(tt => tt.Reward)
               .FirstOrDefault()?.Reward;
            //todo grant each player his reward according to his position in the playersByScore list and the reward in 

        }
    }
}
