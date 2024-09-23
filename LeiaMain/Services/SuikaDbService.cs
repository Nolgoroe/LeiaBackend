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

        public async Task<double?> GetPlayerBalance(Guid? playerId, int? currencyId )
        {
            var balance = await _leiaContext.PlayerCurrencies.FirstOrDefaultAsync (p => p.PlayerId == playerId && p.CurrencyId == currencyId);

            return balance?.CurrencyBalance;
        }
        public async Task<Player?> GetPlayerById(Guid playerId)
        {
            var player = await _leiaContext.Players.FindAsync(playerId);

            return player;
        }
    }

}
