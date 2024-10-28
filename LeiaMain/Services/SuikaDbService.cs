using System.Diagnostics;

using DAL;

using DataObjects;

using Microsoft.EntityFrameworkCore;

namespace Services
{
    public interface ISuikaDbService
    {
        public Task<Player> AddNewPlayer(Player player);
        public Task<double?> GetPlayerBalance(Guid? playerId, int? currencyId);
        public Task<Player?> GetPlayerById(Guid playerId);
        public Task<Player?> GetPlayerByName(string playerName);
        public Task<List<TournamentSession?>?> GetPlayerTournaments(Guid playerId);
        public LeiaContext LeiaContext { get;  /*set;*/ }
    }

    public class SuikaDbService : ISuikaDbService
    {
        private readonly LeiaContext _leiaContext;

        public SuikaDbService(LeiaContext leiaContext)
        {
            _leiaContext = leiaContext;
            LeiaContext = leiaContext;
        }

        public LeiaContext LeiaContext { get;  /*set;*/ }

        public async Task<Player> AddNewPlayer(Player player)
        {
            var newPlayer = await _leiaContext.Players.AddAsync(player);
            await _leiaContext.SaveChangesAsync();

            return newPlayer.Entity;
        }

        public async Task<double?> GetPlayerBalance(Guid? playerId, int? currencyId)
        {
            try
            {
                var balance = _leiaContext.PlayerCurrencies.FirstOrDefault(p => p.PlayerId == playerId && p.CurrenciesId == currencyId);
                return balance?.CurrencyBalance;

            }
            catch (Exception ex)
            {

                Debug.WriteLine(ex.InnerException?.Message);
            }
            return null;
        }
        public async Task<Player?> GetPlayerById(Guid playerId)
        {
            var player = await _leiaContext.Players.FindAsync(playerId);

            return player;
        }

        public async Task<Player?> GetPlayerByName(string playerName)
        {
            try
            {
                var player = /*await*/ _leiaContext.Players.FirstOrDefault(p => p.Name == playerName);
                return player;

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }
        }

        public async Task<List<TournamentSession?>?> GetPlayerTournaments(Guid playerId)
        {
            var tournaments = _leiaContext.Tournaments.Where(t => t.PlayerTournamentSessions.Any(pt => pt.PlayerId == playerId))
                .Include(t => t.TournamentData)
                    .ThenInclude(td => td.EntryFeeCurrency)
                .Include(t => t.TournamentData)
                    .ThenInclude(td => td.EarningCurrency)
                .Include(t => t.TournamentData)
                    .ThenInclude(td => td.TournamentType)
                .Include(t => t.Players)
                .ToList();
            return tournaments;
        }
    }

}
