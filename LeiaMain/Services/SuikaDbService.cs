using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using DAL;

using DataObjects;

using Microsoft.EntityFrameworkCore;
using Services.Shared;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;


namespace Services
{
    /// <summary>
    /// Formally 'MatchRequest'
    /// A virtual object (not in db) that unifies several records related to a current match-make request of a
    /// specific player
    /// </summary>
    public class MatchQueueEntry
    {
        public Player Player;
        public PlayerActiveTournament QueueEntry;
        public int GameTypeId;

        public Currencies LoadCurrency(LeiaContext leiaContext) => leiaContext.Currencies.First(c => c.CurrencyId == QueueEntry.CurrencyId);
        public TournamentType LoadTournamentType(LeiaContext leiaContext) => leiaContext.TournamentTypes.First(t => t.TournamentTypeId == QueueEntry.TournamentTypeId);
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

        public int GameTypeId { get; set; }

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
        public Task<PlayerGameRating?> GetPlayerGameRating(Guid playerId, int gameId);

        public Task<Transactions> AddTransactionRecordAsync(Guid playerId, int currenciesId, decimal? currencyAmount, string transactionTypeName);
        public Task<(Player, PlayerAuthToken)> CreateNewPlayer(Player player);
        public Task<Player>? UpdatePlayer(Player player);
        public Task<Player?> GetPlayerById(Guid playerId);
        public Task<Player?> LoadPlayerByAuthToken(string token);
        public Task<List<HistoryDTO>> GetPlayerTournaments(LeiaContext context, Guid playerId);
        public Task<Player?> GetPlayerByPhoneNumber(string phoneNumber);
        public Task<double?> GetPlayerBalance(Guid? playerId, int? currencyId);
        public Task<List<PlayerCurrencies?>?> GetAllPlayerBalances(Guid playerId);
        public Task<PlayerCurrencies?> UpdatePlayerBalance(Guid? playerId, int? currencyId, double? amount);
        public Task<League?> GetLeagueById(int leagueId);
        public Task<bool> RecordActiveDayAsync(Guid playerId);
        public Task<int> GetActiveDaysCountAsync(Guid playerId);
        public Task Log(string message);

        public Task Log(string message, Guid playerId);

        public Task Log(Exception ex);
        public Task Log(Exception ex, Guid playerId);

        public Task<(List<PlayerTournamentSession>, int)> LoadPlayerTournamentLeaderboard(LeiaContext context, Guid playerId, int tournamentId);


        /// <summary>
        /// Adds a player to PlayerActiveTournaments
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="entryFee">Entry fee of the tournament type selected</param>
        /// <param name="currencyId">Currency Id of the tournament type selected</param>
        /// <param name="tournamentTypeId">Tournament type selected</param>
        /// <returns>True if player is added, returns false and exception if not</returns>
        public Task<bool> MarkPlayerAsMatchMaking(Guid playerId, double entryFee, int currencyId, int tournamentTypeId, int gameTypeId);

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
        public Task<IEnumerable<TournamentSession>> FindSuitableTournamentForRating(Guid playerId, int gameTypeId, int playerRating, int maxRatingDrift, int tournamentTypeId, int currencyId/*, double? playerBalance*/, int maxResults);
        public Task<List<EggReward>> UpdateGivenPlayerEggRewards(int PlayerEggRewardId, List<EggReward> rewards);
        public Task<PlayerMonthlyEgg> StartNewMonthlyEggCount(Guid playerId, int playerEggRewardId);
        public Task<bool> UpdatePlayerFeatures(Guid playerId, List<Feature> features);
        public Task<bool> UpdatePlayerFTUEs(Guid playerId, int ftueId);
        public Task<bool> UpdatePlayerLevelRewards(Guid playerId, List<LevelReward> levelRewards);
        public Task<bool> UpdatePlayerDailyRewards(int playerDailyRewardId, int currentDay);
        public Task<bool> AddPlayerDailyRewards(Guid playerId);
        public Task<bool> UpdatePlayerHourlyRewards(int hourlyRewardId);
        public Task<bool> AddPlayerHourlyRewards(Guid playerId);
        public Task<bool> UpdatePlayerLevel(Guid playerId, int level);
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
                        CurrencyBalance = 0
                    },
                    new ()
                    {
                        PlayerId = player.PlayerId,
                        CurrenciesId = (int)currencies?.Find(c => c.CurrencyName == "USD")?.CurrencyId,
                        CurrencyBalance = 0
                    },
                    new ()
                    {
                        PlayerId = player.PlayerId,
                        CurrenciesId = (int)currencies?.Find(c => c.CurrencyName == "BonusCash")?.CurrencyId,
                        CurrencyBalance = 0
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

                player.RegistrationDate = DateTime.UtcNow;
                //player.UserCode = GenerateUserCode();

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

        public async Task<IEnumerable<TournamentSession>> FindSuitableTournamentForRating(Guid playerId, int gameTypeId, int playerRating, int maxRatingDrift, int tournamentTypeId, int currencyId, int maxResults)
        {
            var tournamentType = await _leiaContext.TournamentTypes.FindAsync(tournamentTypeId);
            if (tournamentType == null)
            {
                throw new Exception($"Tournament type does not exist {tournamentTypeId}");
            }

            //if (tournamentType.EntryFee > playerBalance)
            //{
            //    throw new Exception($"Player has not enough balance for tournament type {tournamentTypeId}, need {tournamentType.EntryFee}, has {playerBalance}");
            //}

            //if your currency ID == 13 (real money) then change drift to look more upwards and return the touranment with the highest glicko rating from the possible touranments
            
            var winRate = await ComputeWinRate(playerId);

            Expression<Func<TournamentSession, bool>> ratingPred;
            if (winRate >= 0.5)
            {
                // you're winning at least half your matches
                // so look for tournaments *harder* than you:
                ratingPred = t => t.Rating > playerRating;
            }
            else if (winRate <= 0.35)
            {
                // you're winning 35% or less 
                // so look for tournaments *easier* than you:
                ratingPred = t => t.Rating < playerRating;
            }
            else
            {
                // if you're in the middle (35%–50%), 
                // stick within your comfort zone:
                ratingPred = t => Math.Abs(t.Rating - playerRating) < maxRatingDrift;
            }

            // Ensure you load related data


            var baseQuery = _leiaContext.Tournaments
                .Include(t => t.Players)
                .Include(t => t.PlayerTournamentSessions)
                .Where(t => t.GameTypeId == gameTypeId)
                // filter out tournaments where the player already has a session
                .Where(t => !t.PlayerTournamentSessions.Any(ps => ps.PlayerId == playerId))
                // apply your dynamic rating predicate
                .Where(ratingPred);

            // DEBUG CHECK: See all tournaments and their players
            //List<TournamentSession> test1 = _leiaContext.Tournaments
            //    .Include(t => t.Players)
            //    .Include(t => t.PlayerTournamentSessions)
            //    .ToList();

            //List<TournamentSession> test2 = baseQuery.ToList(); // Should now contain populated navigation properties

            // STEP 1: Prefer tournaments with <10 players, sorted by oldest StartTime
            var prioritized = await baseQuery
                    .Where(t => t.Players.Count < 10)
                    .OrderBy(t => t.StartTime)
                    .Take(maxResults)
                    .ToListAsync();

            if (prioritized.Any())
                return prioritized;

            // STEP 2: Fallback by currency logic
            if (currencyId != 10)
            {
                // real‐money tournaments → highest‐rated first
                return await baseQuery
                    .OrderByDescending(t => t.Rating)
                    .Take(maxResults)
                    .ToListAsync();
            }
            else
            {
                // other currencies → closest‐rated first
                return await baseQuery
                    .OrderBy(t => Math.Abs(t.Rating - playerRating))
                    .Take(maxResults)
                    .ToListAsync();
            }
        }

        private async Task<double> ComputeWinRate(Guid playerId)
        {
            // pull the whole history in one go
            var history = await GetPlayerTournaments(_leiaContext, playerId);

            if (history.Count <= 5)
                return 0.425;

            int wins = history.Count(dto =>
            {
                if (dto.players.Length < dto.tournamentTypeMaxPlayers)
                    return false;

                var sorted = dto.players
                    .OrderByDescending(p => p.score)
                    .ToArray();

                int rank = Array.FindIndex(sorted, p => p.id == playerId.ToString()) + 1;
                if (rank <= 0) return false;

                int cutoff = dto.rewards
                    .Select(r => r.ForPosition)
                    .DefaultIfEmpty(0)
                    .Max();

                return rank <= cutoff;
            });

            return (double)wins / history.Count;
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

        public async Task<bool> MarkPlayerAsMatchMaking(Guid playerId, double entryFee, int currencyId, int tournamentTypeId, int gameTypeId)
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
                    EntryFee = entryFee,
                    GameTypeId = gameTypeId,
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

        public async Task<Player?> GetPlayerByPhoneNumber(string phoneNumber)
        {
            var player = await _leiaContext.Players.FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber);
            return player;
        }

        /// Helper function of `GetPlayerTournaments`
        private HistoryDTO GetPlayerTournamentsCalcLeaderboard(Guid playerId, Dictionary<Guid, Player> allPlayersById, IEnumerable<PlayerTournamentSession> allPlayerSessions, TournamentType tournamentType, int tournamentSessionId, int gameTypeId)
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

            int tournamentTypeMaxPlayers = tournamentType.NumberOfPlayers.Value;
            return new HistoryDTO
            {
                players = playerEntries,
                tournamentID = tournamentSessionId,
                tournamentTypeID = tournamentType.TournamentTypeId,
                tournamentTypeMaxPlayers = tournamentTypeMaxPlayers,
                currencyID = tournamentType.CurrenciesId,
                entryFee = (float)tournamentType.EntryFee.Value,
                isOpen = leaderBoard.Any(s => s.PlayerScore == null) || tournamentTypeMaxPlayers > leaderBoard.Count,
                rewards = tournamentType.Reward,
                GameTypeId = gameTypeId
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
            // 1) Load tournament-type metadata (with rewards)
            var allTournamentTypesById = await context.TournamentTypes
                .AsNoTracking()
                .Include(t => t.Reward)
                .ToDictionaryAsync(t => t.TournamentTypeId);

            // 2) Load the last 100 sessions *of this player*
            var allSessionsOfCurrentPlayer = await context.PlayerTournamentSession
                .AsNoTracking()
                .Where(s => s.PlayerId == playerId)
                .OrderByDescending(s => s.SubmitScoreTime)
                .Take(100)
                .ToListAsync();

            var playerTournamentIds = allSessionsOfCurrentPlayer
                .Select(s => s.TournamentSessionId)
                .ToList();

            // 3) Load *all* sessions in those tournaments (other players too)
            var allOtherSessions = await context.PlayerTournamentSession
                .AsNoTracking()
                .Where(s => playerTournamentIds.Contains(s.TournamentSessionId))
                .ToListAsync();

            // group them by tournament ID
            var allOtherSessionsByTournamentId = allOtherSessions
                .GroupBy(s => s.TournamentSessionId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 4) Load the Player entities for everyone involved
            var allPlayerIds = allOtherSessions
                .Select(s => s.PlayerId)
                .Distinct()
                .ToList();

            var allPlayersById = (await context.Players
                .AsNoTracking()
                .Where(p => allPlayerIds.Contains(p.PlayerId))
                .ToListAsync())
                .ToDictionary(p => p.PlayerId);

            // 5) Load the TournamentSession metadata for those IDs
            var allTournamentsById = (await context.Tournaments
                .AsNoTracking()
                .Where(t => playerTournamentIds.Contains(t.TournamentSessionId))
                .ToListAsync())
                .ToDictionary(t => t.TournamentSessionId);

            // 6) Now that everything’s in memory, build your HistoryDTO list
            var history = allSessionsOfCurrentPlayer
                .Select(s =>
                    GetPlayerTournamentsCalcLeaderboard(
                        s.PlayerId,
                        allPlayersById,
                        allOtherSessionsByTournamentId[s.TournamentSessionId],
                        allTournamentTypesById[s.TournamentTypeId],
                        s.TournamentSessionId,
                        allTournamentsById[s.TournamentSessionId].GameTypeId
                    )
                )
                .ToList();

            return history;
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


        public async Task<Transactions> AddTransactionRecordAsync(Guid playerId, int currenciesId, decimal? currencyAmount, string transactionTypeName)
        {
            var transactionType = await _leiaContext.TransactionTypes.FirstOrDefaultAsync(t => t.TransactionTypeName == transactionTypeName);

            if (transactionType == null)
            {
                transactionType = new TransactionType
                {
                    TransactionTypeName = transactionTypeName,
                };
                _leiaContext.TransactionTypes.Add(transactionType);
                await _leiaContext.SaveChangesAsync();
            }

            // Create a new transaction record
            var newTransaction = new Transactions
            {
                PlayerId = playerId,
                TransactionDate = DateTime.UtcNow,
                CurrenciesId = currenciesId,
                CurrencyAmount = currencyAmount,
                TransactionTypeId = transactionType.TransactionTypeId,
                TransactionTypeName = transactionTypeName
            };

            // Add the transaction to the database context
            _leiaContext.Transactions.Add(newTransaction);

            // Save the changes to commit the transaction record
            await _leiaContext.SaveChangesAsync();

            return newTransaction;
        }


        public async Task<PlayerGameRating?> GetPlayerGameRating(Guid playerId, int gameId)
        {
            return await _leiaContext.PlayerGameRatings
                .FirstOrDefaultAsync(r => r.PlayerId == playerId && r.GameId == gameId);
        }

       public async Task<List<EggReward>> UpdateGivenPlayerEggRewards(int activePlayerEggsId, List<EggReward> rewards)
        {
            try
            {
                foreach (var reward in rewards)
                {
                    GivenPlayerEggReward givenReward = new GivenPlayerEggReward();
                    givenReward.ActivePlayerEggsId = activePlayerEggsId;
                    givenReward.EggRewardId = reward.EggRewardId;
                    var isAdded = _leiaContext.GivenPlayerEggRewards.Add(givenReward);
                }
                
               
              var saved = await _leiaContext.SaveChangesAsync();

              return _leiaContext.GivenPlayerEggRewards.Where(r => r.ActivePlayerEggsId == activePlayerEggsId).ToList().Select(g => g.EggReward).ToList();
               

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }

        }
        public async Task<PlayerMonthlyEgg> StartNewMonthlyEggCount(Guid playerId, int playerEggRewardId)
        {
        
            try
            {
                var balance = _leiaContext.PlayerCurrencies.Where(r => r.PlayerId == playerId && r.CurrenciesId == (int)Enums.CurrenciesEnum.Eggs).FirstOrDefault();
                balance.CurrencyBalance = 0;
                _leiaContext.Entry(balance).State = EntityState.Modified;
                var updateBalance = _leiaContext.PlayerCurrencies.Update(balance);

                var playerEggRewardToUpdate = _leiaContext.PlayerMonthlyEggs.Where(p => p.ActivePlayerEggsId == playerEggRewardId).FirstOrDefault();
                playerEggRewardToUpdate.IsActive = false;
                _leiaContext.Entry(playerEggRewardToUpdate).State = EntityState.Modified;
                var updatePlayerEggReward = _leiaContext.PlayerMonthlyEggs.Update(playerEggRewardToUpdate);

                PlayerMonthlyEgg playerEggReward = new PlayerMonthlyEgg();
                playerEggReward.StartDate = DateTime.UtcNow;
                playerEggReward.PlayerId = playerId;
                playerEggReward.IsActive = true;
               var isAdded = _leiaContext.PlayerMonthlyEggs.Add(playerEggReward);
              
               var saved = await _leiaContext.SaveChangesAsync();

                return _leiaContext.PlayerMonthlyEggs.Where(r => r.PlayerId == playerId && r.StartDate.Month == DateTime.UtcNow.Month).FirstOrDefault();


            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }
        }

        public async Task<bool> UpdatePlayerFeatures(Guid playerId, List<Feature> features)
        {
            try
            {
                foreach (var feature in features)
                {
                    PlayerFeature playerFeature = new PlayerFeature();
                    playerFeature.PlayerId = playerId;
                    playerFeature.Feature = feature;    
                    var isAdded = _leiaContext.PlayerFeatures.Add(playerFeature);
                }

                var saved = await _leiaContext.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }

        }

        public async Task<bool> UpdatePlayerFTUEs(Guid playerId, int ftueId)
        {
            try
            {
               
                    PlayerFtue playerFtue = new PlayerFtue();
                    playerFtue.PlayerId = playerId;
                    playerFtue.FtueId = ftueId;
                    playerFtue.IsComplete = false;
                    var isAdded = _leiaContext.PlayerFtues.Add(playerFtue);
                
                var saved = await _leiaContext.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }

        }

        public async Task<bool> UpdatePlayerLevelRewards(Guid playerId, List<LevelReward> levelRewards)
        {
            try
            {
                foreach (var reward in levelRewards)
                {
                    GivenPlayerLevelReward levelReward = new GivenPlayerLevelReward();
                    levelReward.PlayerId = playerId;
                    levelReward.LevelRewardId = reward.LevelRewardId;   
                    var isAdded = _leiaContext.GivenPlayerLevelRewards.Add(levelReward);
                }

                var saved = await _leiaContext.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }
        }

        public async Task<bool> AddPlayerDailyRewards(Guid playerId)
        {
            try
            {

                PlayerDailyReward dailyReward = new PlayerDailyReward();
                var reward = _leiaContext.DailyRewards.Where(r => r.SerialNumber == 1).FirstOrDefault();
                dailyReward.DailyRewardsId = reward.DailyRewardsId;
                dailyReward.PlayerId = playerId;
                dailyReward.LastClaimDate = DateTime.UtcNow;
                dailyReward.ConsecutiveDays = 1;
                dailyReward.IsActive = true;
                var isAdded = _leiaContext.PlayerDailyRewards.Add(dailyReward);

                var saved = await _leiaContext.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }
        }
        public async Task<bool> UpdatePlayerDailyRewards(int playerDailyRewardId, int currentRewardDay)
        {
            try
            {
                var toUpdate = _leiaContext.PlayerDailyRewards.Where(p => p.PlayerDailyRewardId == playerDailyRewardId).FirstOrDefault();
                var reward = _leiaContext.DailyRewards.Where(r => r.SerialNumber == currentRewardDay).FirstOrDefault();
                toUpdate.CurrentRewardDay = currentRewardDay;
                toUpdate.DailyRewardsId = reward.DailyRewardsId;
                toUpdate.LastClaimDate = DateTime.UtcNow;
                toUpdate.ConsecutiveDays++;
                var updated = _leiaContext.PlayerDailyRewards.Update(toUpdate);
                var saved = await _leiaContext.SaveChangesAsync();
                return true;

            }

            catch (Exception ex) 
            {
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }
        }

        public async Task<bool> UpdatePlayerHourlyRewards(int hourlyRewardId)
        {
            try
            {
                var toUpdate = _leiaContext.PlayerHourlyRewards.Where(p => p.HourlyRewardId == hourlyRewardId).FirstOrDefault();                           
                toUpdate.LastClaimDate = DateTime.UtcNow;
                var updated = _leiaContext.PlayerHourlyRewards.Update(toUpdate);
                var saved = await _leiaContext.SaveChangesAsync();
                return true;

            }
            
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }
        }

        public async Task<bool> AddPlayerHourlyRewards(Guid playerId)
        {
            try
            {

                PlayerHourlyReward hourlyReward = new PlayerHourlyReward();
                hourlyReward.PlayerId = playerId;
                hourlyReward.LastClaimDate = DateTime.UtcNow;
                hourlyReward.IsActive = true;
                var isAdded = _leiaContext.PlayerHourlyRewards.Add(hourlyReward);
                var saved = await _leiaContext.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }
        }

        public async Task<bool> UpdatePlayerLevel(Guid playerId, int level)
        {
            try
            {
                var toUpdate = _leiaContext.Players.Where(p => p.PlayerId == playerId).FirstOrDefault();
                toUpdate.Level = level;
                var updated = _leiaContext.Players.Update(toUpdate);
                var saved = await _leiaContext.SaveChangesAsync();
                return true;

            }

            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }
        }



        public string GenerateUserCode()
        {
            StringBuilder result = new StringBuilder();
            Random _randomNum = new Random();
            Random _randomStr = new Random();
            string randomNum = _randomNum.Next(0, 9999).ToString("D4");
            result.Append(randomNum.ToString());
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string randomStr = new string(Enumerable.Repeat(chars, 4)
                .Select(s => s[_randomStr.Next(s.Length)]).ToArray());
            result.Append(randomStr);

            //return randomStr + randomNum;
            return result.ToString();

        }





        public async Task<bool> RecordActiveDayAsync(Guid playerId)
        {
            var today = DateTime.UtcNow.Date;
            bool exists = await LeiaContext.PlayerActiveDays
                .AnyAsync(a => a.PlayerId == playerId && a.Date == today);

            if (!exists)
            {
                LeiaContext.PlayerActiveDays.Add(new PlayerActiveDay
                {
                    PlayerId = playerId,
                    Date = today
                });
                await LeiaContext.SaveChangesAsync();
                return true;
            }

            return false;
        }
        public Task<int> GetActiveDaysCountAsync(Guid playerId)
        {
            return LeiaContext.PlayerActiveDays
                .CountAsync(a => a.PlayerId == playerId);
        }
    }

}
