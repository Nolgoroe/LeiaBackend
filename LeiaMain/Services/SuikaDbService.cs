using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Numerics;

using DAL;

using DataObjects;

using Microsoft.EntityFrameworkCore;

namespace Services
{
    /// <summary>
    /// Formally MatchRequest
    /// A virtual object (not in db) that unifies several records related to a current match-make request of a
    /// specific player
    /// </summary>
    public class MatchQueueEntry
    {
        public Player Player;
        public PlayerActiveTournament QueueEntry;

        /// <summary>
        /// A temporary method that is meant to reduce the amount of refactoring done in a single iteration
        /// Eventually the class MatchRequest is to be deprecated
        /// </summary>
        public MatchRequest ConvertToLegacyMatchRequest(LeiaContext leiaContext)
        {
            var currency = leiaContext.Currencies.First(c => c.CurrencyId == QueueEntry.CurrencyId);
            var tournamentType = leiaContext.TournamentTypes.First(t => t.TournamentTypeId == QueueEntry.TournamentTypeId);
            return new MatchRequest()
            {
                Player = Player,
                MatchFee = QueueEntry.EntryFee,
                MatchFeeCurrency = currency,
                RequestTime = QueueEntry.MatchmakeStartTime,
                TournamentType = tournamentType,
            };
        }
    }

    public record HistoryDTO //DTO = data transfer object from server
    {
        public LeaderboardPlayerData[] players { get; set; }
        public int tournamentID { get; set; }
        public int tournamentTypeID { get; set; }
        public int tournamentTypeMaxPlayers { get; set; }
        public int currencyID { get; set; }
        public float entryFee { get; set; }
        public bool isOpen { get; set; }
        public List<Reward> rewards { get; set; }

    }


    public record LeaderboardPlayerData
    {
        public string name { get; set; }
        public string id { get; set; }
        public int? score { get; set; }
        public bool? didClaim { get; set; }
        public DateTime joinTime { get; set; }
    }

    public interface ISuikaDbService
    {
        public Task<(Player, PlayerAuthToken)> CreateNewPlayer(Player player);
        public Task<Player>? UpdatePlayer(Player player);
        public Task<Player?> GetPlayerById(Guid playerId);
        public Task<Player?> GetPlayerByName(string playerName);
        public Task<Player?> LoadPlayerByAuthToken(string token);
        public Task<List<HistoryDTO>> GetPlayerTournaments(LeiaContext context, Guid playerId);
        public Task<double?> GetPlayerBalance(Guid? playerId, int? currencyId);
        public Task<List<PlayerCurrencies?>?> GetAllPlayerBalances(Guid playerId);
        public Task<PlayerCurrencies?> UpdatePlayerBalance(Guid? playerId, int? currencyId, double? amount);
        public Task<League?> GetLeagueById(int leagueId);
        public Task Log(string message);

        public Task Log(string message, Guid playerId);

        public Task Log(Exception ex);
        public Task Log(Exception ex, Guid playerId);

        public async Task<(List<PlayerTournamentSession>, int)> LoadPlayerTournamentLeaderboard(LeiaContext context, Guid playerId, int tournamentId);


        /// <summary>
        /// Adds a player to PlayerActiveTournaments
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="entryFee">Entry fee of the tournament type selected</param>
        /// <param name="currencyId">Currency Id of the tournament type selected</param>
        /// <param name="tournamentTypeId">Tournament type selected</param>
        /// <returns>True if player is added, returns false and exception if not</returns>
        public Task<bool> MarkPlayerAsMatchMaking(Guid playerId, double entryFee, int currencyId, int tournamentTypeId);

        /// <summary>
        /// Gets the PlayerActiveTournament for a player
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns>The PlayerActiveTournament of the player</returns>
        public Task<PlayerActiveTournament> GetPlayerActiveMatchMakeRecord(Guid playerId);

        public Task<bool> MarkMatchMakeEntryAsCharged(Guid playerId, int tournamentId);
        public Task<bool> RemovePlayerFromActiveMatchMaking(Guid playerId);
        /// <summary>
        /// Checks if this should use PlayerActiveTournaments.Remove instead of ExecuteDeleteAsync
        /// </summary>
        public Task<bool> RemovePlayerFromActiveTournament(Guid playerId, int tournamentId);
        public Task<bool> RemovePlayerFromAnyActiveTournament(Guid playerId);
        public Task<bool> SetPlayerActiveTournament(Guid playerId, int tournamentId);
        public Task<bool> IsPlayerMatchMaking(Guid playId);
        /// <summary>
        /// Joins Players and PlayerActiveTournaments tables on playerId, and gets the ActiveTournaments with an Id of -1
        /// Returns, up to `maxPlayers`, `MatchQueueEntry`s that are pending to join a match (in queue)
        /// The results are sorted by waiting time, from longest to shortest
        /// </summary>
        /// <param name="maxPlayers">Maximum amount of players to return</param>
        /// <returns>Queue entries sorted by waiting time</returns>
        public Task<IEnumerable<MatchQueueEntry>> GetPlayersWaitingForMatch(int maxPlayers);

        /// <summary>
        /// Returns up to `maxResults` tournaments that comply with ALL the following rules:
        /// 1. Tournament is open
        /// 2. Suitable for `playerRating`
        /// 3. Fit the given `tournamentTypeId`
        /// 4. Fit the given `currencyId`
        /// 5. Player has sufficient balance to join
        /// 6. Tournament is not full
        /// 
        /// The results are sorted by how suitable the tournament rating is to the given player rating
        /// </summary>
        /// <param name="playerRating">Player rating</param>
        /// <param name="maxRatingDrift">Maximum amount of rating drift</param>
        /// <param name="tournamentTypeId"></param>
        /// <param name="currencyId"></param>
        /// <param name="playerBalance"></param>
        /// <param name="maxResults"></param>
        /// <returns></returns>
        public Task<IEnumerable<TournamentSession>> FindSuitableTournamentForRating(Guid playerId, int playerRating, int maxRatingDrift, int tournamentTypeId, int currencyId, double playerBalance, int maxResults);
        public LeiaContext LeiaContext { get; set; }
    }

    public class SuikaDbService : ISuikaDbService
    {
        private readonly LeiaContext _leiaContext;

        public SuikaDbService(LeiaContext leiaContext)
        {
            _leiaContext = leiaContext;
            LeiaContext = leiaContext;
        }

        public LeiaContext LeiaContext { get; set; }

        public async Task<(Player, PlayerAuthToken)> CreateNewPlayer(Player player)
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

                var league = await _leiaContext.League.FindAsync(player?.LeagueId);
                if (league != null) player.League = league;

                try
                {
                    var newPlayer = _leiaContext.Players.Add(player);
                    var newPlayerSecrest = _leiaContext.PlayerAuthToken.Add(new PlayerAuthToken
                    {
                        Player = newPlayer.Entity,
                        Secret = Guid.NewGuid().ToString(),
                        Token = Guid.NewGuid().ToString()
                    });
                    await _leiaContext.SaveChangesAsync();

                    return (newPlayer.Entity, newPlayerSecrest.Entity);

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


        public async Task<IEnumerable<MatchQueueEntry>> GetPlayersWaitingForMatch(int maxPlayers)
        {
            return await (from player in _leiaContext.Players
                          join activeTournament in _leiaContext.PlayerActiveTournaments
                          on player.PlayerId equals activeTournament.PlayerId
                          where activeTournament.TournamentId == PlayerActiveTournament.MATCH_MAKING_TOURNAMENT_ID
                          orderby activeTournament.JoinTournamentTime
                          select new MatchQueueEntry
                          {
                              Player = player,
                              QueueEntry = activeTournament,
                          })
                   .Take(maxPlayers)
                   .ToListAsync();
        }

        public async Task<IEnumerable<TournamentSession>> FindSuitableTournamentForRating(Guid playerId, int playerRating, int maxRatingDrift, int tournamentTypeId, int currencyId, double playerBalance, int maxResults)
        {
            var tournamentType = await _leiaContext.TournamentTypes.FindAsync(tournamentTypeId);
            if (tournamentType == null)
            {
                throw new Exception($"Tournament type does not exist {tournamentTypeId}");
            }
            if (tournamentType.EntryFee > playerBalance)
            {
                throw new Exception($"Player has not enough balance for tournament type {tournamentTypeId}, need {tournamentType.EntryFee}, has {playerBalance}");
            }
            return await _leiaContext.Tournaments
                .Where(
                    t => Math.Abs(t.Rating - playerRating) < maxRatingDrift &&         // Rating is in range
                                                                                       // t.TournamentData.TournamentTypeId == tournamentTypeId &&
                    t.IsOpen &&                                                        // Tournament is open
                                                                                       // t.TournamentData.EntryFeeCurrencyId == currencyId &&               // The currency Id is matching
                    t.Players.Count < tournamentType.NumberOfPlayers &&
                    !t.Players.Select(p => p.PlayerId).Contains(playerId)
                    ) // Tournament is not full
                .OrderBy(t => Math.Abs(t.Rating - playerRating))
                .Take(maxResults)
                .ToListAsync();
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
                await Log("In SuikaDbService.UpdatePlayerBalance: Player balances not found", playerId.Value);
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
                        if (saved > 0) await Log($"In SuikaDbService.UpdatePlayerBalance, updated PlayerCurrencies: Player - {savedBalance?.Entity?.PlayerId}, Currency - {savedBalance?.Entity?.CurrenciesId}, Amount - {savedBalance?.Entity?.CurrencyBalance}", savedBalance.Entity.PlayerId);

                        return savedBalance?.Entity;
                    }
                    catch (Exception ex)
                    {
                        await Log(ex, playerId.Value);
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
                        if (saved > 0) await Log($"In UpdateBalanceWithReward, saved new PlayerCurrencies: Player - {savedBalance?.Entity?.PlayerId}, Currency - {savedBalance?.Entity?.CurrenciesId}, Amount - {savedBalance?.Entity?.CurrencyBalance}", playerId.Value);

                        return savedBalance?.Entity;
                    }
                    catch (Exception ex)
                    {
                        await Log(ex, playerId.Value);
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

        public async Task Log(Exception ex)
        {
            await Log(ex, Guid.Empty);
        }

        public async Task Log(Exception ex, Guid playerId)
        {
            var message = $"Exception: {ex.ToString()}:\n{ex.InnerException?.ToString()}";
            await Log(message, playerId);
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
            Trace.WriteLine(message);
        }

        public async Task<bool> MarkPlayerAsMatchMaking(Guid playerId, double entryFee, int currencyId, int tournamentTypeId)
        {

            try
            {
                _leiaContext.PlayerActiveTournaments.Add(new PlayerActiveTournament()
                {
                    PlayerId = playerId,
                    TournamentId = -1,
                    MatchmakeStartTime = DateTime.Now,
                    CurrencyId = currencyId,
                    TournamentTypeId = tournamentTypeId,
                    EntryFee = entryFee
                });
                await _leiaContext.SaveChangesAsync();
                var message = $"Player {playerId} started matchmaking";
                await Log(message, playerId);
                return true;
            }
            catch (DbUpdateException ex)
            {
                var message = $"Could not mark player as matchmaking, player {playerId} is either already matchmaking or in a tournament";
                await Log(message, playerId);
                Trace.WriteLine(message);
                return false;
            }
            catch (Exception ex)
            {
                var message = $"Unknown error while trying to mark player as 'matchmaking': {ex.Message}";
                await Log(message, playerId);
                Trace.WriteLine(message);
                return false;
            }
        }

        public async Task<bool> MarkMatchMakeEntryAsCharged(Guid playerId, int tournamentId)
        {
            var rowsUpdated = await _leiaContext.PlayerActiveTournaments
                .Where(p => p.PlayerId == playerId && p.TournamentId == tournamentId)
                .ExecuteUpdateAsync(p => p.SetProperty(p => p.DidCharge, true));
            return rowsUpdated > 0;
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
            return await RemovePlayerFromActiveTournament(playerId, PlayerActiveTournament.MATCH_MAKING_TOURNAMENT_ID);
        }


        public async Task<PlayerActiveTournament> GetPlayerActiveMatchMakeRecord(Guid playerId)
        {
            return await _leiaContext.PlayerActiveTournaments.FindAsync(playerId);
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
                .Where(p => p.PlayerId == playerId && p.TournamentId == PlayerActiveTournament.MATCH_MAKING_TOURNAMENT_ID)
                .ExecuteUpdateAsync(p => p.SetProperty(p => p.TournamentId, tournamentId).SetProperty(p => p.JoinTournamentTime, DateTime.UtcNow));
            return rowsUpdated > 0;
        }
        public async Task<bool> IsPlayerMatchMaking(Guid playerId)
        {
            return await _leiaContext.PlayerActiveTournaments
                .Where(p => p.TournamentId == PlayerActiveTournament.MATCH_MAKING_TOURNAMENT_ID).CountAsync(p => p.PlayerId == playerId) > 0;
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



        /// Helper function of `GetPlayerTournaments`
        private HistoryDTO GetPlayerTournamentsCalcLeaderboard(Guid playerId, Dictionary<Guid, Player> allPlayersById, IEnumerable<PlayerTournamentSession> allPlayerSessions, TournamentType tournamentType, int tournamentSessionId)
        {
            var leaderBoard = PostTournamentService.CalculateLeaderboardForPlayer(playerId, allPlayerSessions, tournamentType, tournamentSessionId).ToList();
            var playerEntries = leaderBoard.Select(l => new LeaderboardPlayerData()
            {
                name = allPlayersById[l.PlayerId].Name,
                id = l.PlayerId.ToString(),
                score = l.PlayerScore,
                didClaim = l.DidClaim,
                joinTime = l.JoinTime,
            }).ToArray();

            return new HistoryDTO
            {
                players = playerEntries,
                tournamentID = tournamentSessionId,
                tournamentTypeID = tournamentType.TournamentTypeId,
                tournamentTypeMaxPlayers = tournamentType.NumberOfPlayers.Value,
                currencyID = tournamentType.CurrenciesId,
                entryFee = (float)tournamentType.EntryFee.Value,
                isOpen = leaderBoard.Any(s => s.PlayerScore == null),
                rewards = tournamentType.Reward,
            };
        }

        public async Task<(List<PlayerTournamentSession>, int)> LoadPlayerTournamentLeaderboard(LeiaContext context, Guid playerId, int tournamentId)
        {
            var allPlayerSessions = context.PlayerTournamentSession.Where(s => s.TournamentSessionId == tournamentId).ToList();
            var playerSession = allPlayerSessions.First(s => s.PlayerId == playerId);
            var tournamentType = context.TournamentTypes.First(t => t.TournamentTypeId == playerSession.TournamentTypeId);
            var leaderBoard = PostTournamentService.CalculateLeaderboardForPlayer(playerId, allPlayerSessions, tournamentType, tournamentId).ToList();
            return (leaderBoard, tournamentType.NumberOfPlayers.Value) ;
        }

        public async Task<List<HistoryDTO>> GetPlayerTournaments(LeiaContext context, Guid playerId)
        {
            // We load all the tournament types and 100 of the most recent sessions of the current player
            var allTournamentTypesById = await context.TournamentTypes.Include(t => t.Reward).ToDictionaryAsync(t => t.TournamentTypeId);
            var allSessionsOfCurrentPlayer = context.PlayerTournamentSession
                .Where(s => s.PlayerId == playerId)
                .OrderByDescending(s => s.SubmitScoreTime)
                .Take(100)
                .ToList();
            // We load the rest of the sessions of other players in the 100 last tournaments of the current player
            // We arrange these sessions into groups and save the groups to a dictionary with tournamentId as the key
            var playerTournamentIds = allSessionsOfCurrentPlayer.Select(s => s.TournamentSessionId).ToList();
            var allOtherSessionsByTournamentId = context.PlayerTournamentSession.Where(s => playerTournamentIds.Contains(s.TournamentSessionId))
                .GroupBy(s => s.TournamentSessionId)
                .ToDictionary(group => group.Key, group => group.ToList());
            var allPlayerIds = allOtherSessionsByTournamentId.SelectMany(kv => kv.Value).Select(s => s.PlayerId).Distinct().ToList();
            var allPlayersById = context.Players.Where(p => allPlayerIds.Contains(p.PlayerId)).ToDictionary(p => p.PlayerId);
            // We convert and sort the sessions to leaderboards using the mixed-trounament leaderboard calculator
            return allSessionsOfCurrentPlayer.Select(s =>
            {
                return GetPlayerTournamentsCalcLeaderboard(s.PlayerId, allPlayersById, allOtherSessionsByTournamentId[s.TournamentSessionId], allTournamentTypesById[s.TournamentTypeId], s.TournamentSessionId);
            }).ToList();
        }
     

        public async Task<League?> GetLeagueById(int leagueId)
        {
            var league = await _leiaContext.League.Include(l => l.Players)
                .FirstOrDefaultAsync(l => l.LeagueId == leagueId);
            return league;
        }

        public Task<Player?> LoadPlayerByAuthToken(string authToken)
        {
            return _leiaContext.PlayerAuthToken
                .Where(pat => pat.Token == authToken)
                .Select(pat => pat.Player)
                .FirstOrDefaultAsync();
        }
    }

}
