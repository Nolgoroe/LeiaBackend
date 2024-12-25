using System.Diagnostics;
using System.Numerics;

using DAL;

using DataObjects;

using Microsoft.EntityFrameworkCore;

namespace Services
{
    public interface ISuikaDbService
    {
        public Task<Player> AddNewPlayer(Player player);
        public Task<Player>? UpdatePlayer(Player player);
        public Task<Player?> GetPlayerById(Guid playerId);
        public Task<Player?> GetPlayerByName(string playerName);
        public Task<List<TournamentSession?>?> GetPlayerTournaments(Guid playerId);
        public Task<double?> GetPlayerBalance(Guid? playerId, int? currencyId);
        public Task<List<PlayerCurrencies?>?> GetAllPlayerBalances(Guid playerId);
        public Task<PlayerCurrencies?> UpdatePlayerBalance(Guid? playerId, int? currencyId, double? amount);
        public Task Log(string message);

        public Task Log(string message, Guid playerId);

        public Task<bool> MarkPlayerAsMatchMaking(Guid playerId);

        public Task<bool> RemovePlayerFromActiveMatchMaking(Guid playerId);
        public Task<bool> RemovePlayerFromActiveTournament(Guid playerId, int tournamentId);
        public Task<bool> RemovePlayerFromAnyActiveTournament(Guid playerId);
        public Task<bool> SetPlayerActiveTournament(Guid playerId, int tournamentId);
        public Task<bool> IsPlayerMatchMaking(Guid playId);
        public LeiaContext LeiaContext { get; set; }
    }

    public class SuikaDbService : ISuikaDbService
    {
        /// <summary>
        /// This is the value of tournamentId inside PlayerActiveTournament incase they are still matchmaking
        /// </summary>
        private const int PLAYER_ACTIVE_TOURNAMENT_MATCHMAKING_TOURNAMENT_ID = -1;

        private readonly LeiaContext _leiaContext;

        public SuikaDbService(LeiaContext leiaContext)
        {
            _leiaContext = leiaContext;
            LeiaContext = leiaContext;
        }

        public LeiaContext LeiaContext { get; set; }
      
        public async Task<Player> AddNewPlayer(Player player)
        {
            if (player != null)
            {
                var currencies = await _leiaContext.Currencies.ToListAsync();

                player.PlayerCurrencies.AddRange(new List<PlayerCurrencies> {
                    new ()
                    {
                        PlayerId = player.PlayerId,
                        CurrenciesId = (int)currencies?.Find(c => c.CurrencyName == "Gems")?.CurrencyId,
                        CurrencyBalance = 1000
                    },
                    new()
                    {
                         PlayerId = player.PlayerId,
                         CurrenciesId = (int)currencies?.Find(c => c.CurrencyName == "Eggs")?.CurrencyId,
                         CurrencyBalance = 0
                    },
                    new ()
                    {
                        PlayerId = player.PlayerId,
                        CurrenciesId = (int)currencies?.Find(c => c.CurrencyName == "XP")?.CurrencyId,
                        CurrencyBalance = 0
                    },
                });

                try
                {
                    var newPlayer = _leiaContext.Players.Add(player);
                    await _leiaContext.SaveChangesAsync();

                    return newPlayer.Entity;

                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                    throw;
                }
            }
            else throw new ArgumentNullException(nameof(player));
        }


        public async Task<Player>? UpdatePlayer(Player player)
        {
            if (player != null)
            {
                _leiaContext.Entry(player).State = EntityState.Modified;
                try
                {
                    var updatedPlayer = _leiaContext.Players.Update(player);
                    var saved = await _leiaContext.SaveChangesAsync();
                    if (saved > 0)
                    {
                        Trace.WriteLine($"In SuikaDbService.UpdatePlayer, Player: {player?.Name}, was updated successfully");
                        return updatedPlayer.Entity;
                    }
                    else
                    {
                        Trace.WriteLine($"In SuikaDbService.UpdatePlayer, Player: {player?.Name}, was not updated");
                        return null;
                    }

                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                    throw;
                }
            }
            else
            {
                Trace.WriteLine($"In SuikaDbService.UpdatePlayer, Player: {player?.Name}, was not updated");
                return null;
            }
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

                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
            }
            return null;
        }

        public async Task<List<PlayerCurrencies?>?> GetAllPlayerBalances(Guid playerId)
        {
            try
            {
                var balances = _leiaContext.PlayerCurrencies.Where(p => p.PlayerId == playerId)
                    .Include(pc => pc.Currencies)
                    // .Include(pc => pc.Player)
                    .ToList();
                return balances;

            }
            catch (Exception ex)
            {

                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
            }
            return null;
        }

        public async Task<PlayerCurrencies?> UpdatePlayerBalance(Guid? playerId, int? currencyId, double? amount)
        {
            var balances = _leiaContext.PlayerCurrencies.Where(p => p.PlayerId == playerId).ToList();
            if (balances == null)
            {
                Trace.WriteLine("In SuikaDbService.UpdatePlayerBalance: Player balances not found");
                return null;
            }

            if (balances?.Count > 0)
            {
                var balance = balances.FirstOrDefault(pc => pc.CurrenciesId == currencyId);
                if (balance != null) //if he already has a balance  , update it
                {
                    balance.CurrencyBalance += (double)amount /*Convert.ToDouble(amount)*/;

                    try
                    {
                        _leiaContext.Entry(balance).State = EntityState.Modified;
                        var savedBalance = _leiaContext.PlayerCurrencies.Update(balance);
                        var saved = await _leiaContext?.SaveChangesAsync();
                        if (saved > 0) Trace.WriteLine($"In SuikaDbService.UpdatePlayerBalance, updated PlayerCurrencies: Player - {savedBalance?.Entity?.PlayerId}, Currency - {savedBalance?.Entity?.CurrenciesId}, Amount - {savedBalance?.Entity?.CurrencyBalance}");

                        return savedBalance?.Entity;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                        throw;
                    }
                }
                else //if not, create a new balance for him
                {
                    var newBalance = new PlayerCurrencies
                    {
                        CurrenciesId = (int)currencyId /*Convert.ToInt32( currencyId)*/,
                        CurrencyBalance = (double)amount /*Convert.ToDouble(amount)*/,
                        PlayerId = (Guid)playerId
                    };
                    try
                    {
                        var savedBalance = _leiaContext.PlayerCurrencies.Add(newBalance);
                        var saved = await _leiaContext?.SaveChangesAsync();
                        if (saved > 0) Trace.WriteLine($"In UpdateBalanceWithReward, saved new PlayerCurrencies: Player - {savedBalance?.Entity?.PlayerId}, Currency - {savedBalance?.Entity?.CurrenciesId}, Amount - {savedBalance?.Entity?.CurrencyBalance}");

                        return savedBalance?.Entity;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                        throw;
                    }
                }
            }
            return null;
        }


        public async Task Log(string message)
        {
            await Log(message, Guid.Empty);
        }

        public async Task Log(string message, Guid playerId)
        {
            // We do not use SaveChanges here, because that may cause deadlocks
            await _leiaContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO BackendLogs (PlayerId, Timestamp, Log) VALUES ({0}, {1}, {2})",
                playerId,
                DateTime.UtcNow,
                message
            );
        }

        public async Task<bool> MarkPlayerAsMatchMaking(Guid playerId)
        {

            try
            {
                _leiaContext.PlayerActiveTournaments.Add(new PlayerActiveTournament()
                {
                    PlayerId = playerId,
                    TournamentId = -1,
                    MatchmakeStartTime = DateTime.Now,
                });
                await _leiaContext.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex) 
            {
                var message = $"Could not mark player as matchmaking, player {playerId} is either already matchmaking or in a tournament";
                Log(message, playerId);
                Trace.WriteLine(message);
                return false;
            }
            catch (Exception ex)
            {
                var message = $"Unknown error while trying to mark player as 'matchmaking': {ex.Message}";
                Log(message, playerId);
                Trace.WriteLine(message);
                return false;
            }
        }

        public async Task<bool> RemovePlayerFromAnyActiveTournament(Guid playerId)
        {
            var rowsAffected = await _leiaContext.PlayerActiveTournaments
                .Where(p => p.PlayerId == playerId)
                .ExecuteDeleteAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> RemovePlayerFromActiveMatchMaking(Guid playerId)
        {
            return await RemovePlayerFromActiveTournament(playerId, PLAYER_ACTIVE_TOURNAMENT_MATCHMAKING_TOURNAMENT_ID);
        }

        public async Task<bool> RemovePlayerFromActiveTournament(Guid playerId, int tournamentId)
        {
            var rowsAffected = await _leiaContext.PlayerActiveTournaments
                .Where(p => p.PlayerId == playerId && p.TournamentId == tournamentId)
                .ExecuteDeleteAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> SetPlayerActiveTournament(Guid playerId, int tournamentId)
        {
            var rowsUpdated = await _leiaContext.PlayerActiveTournaments
                .Where(p => p.PlayerId == playerId && p.TournamentId == PLAYER_ACTIVE_TOURNAMENT_MATCHMAKING_TOURNAMENT_ID)
                .ExecuteUpdateAsync(p => p.SetProperty(p => p.TournamentId, tournamentId).SetProperty(p => p.JoinTournamentTime, DateTime.UtcNow));
            return rowsUpdated > 0;
        }
        public async Task<bool> IsPlayerMatchMaking(Guid playerId)
        {
            return await _leiaContext.PlayerActiveTournaments
                .Where(p => p.TournamentId == PLAYER_ACTIVE_TOURNAMENT_MATCHMAKING_TOURNAMENT_ID)
                .CountAsync(p => p.PlayerId == playerId) > 0;
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
                        .ThenInclude(tt => tt.Reward)
                .Include(t => t.Players)
                .ToList();
            return tournaments;
        }
    }

}
