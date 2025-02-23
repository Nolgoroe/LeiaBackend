using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

using DAL;

using DataObjects;

using Glicko2;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Services
{
    public interface IPostTournamentService
    {
        public Task CloseTournament(TournamentSession tournament);
        public Task<(double?, bool?, double?)> GrantTournamentPrizes(TournamentSession? tournament, Player? player);

    }


    public class PostTournamentService : IPostTournamentService
    {
        private struct TournamentGlickoRatingCalculationEntry
        {
            public int score;
            public Player player;
            public GlickoOpponent opponent;
        }


        private readonly ISuikaDbService _suikaDbService;
        //public ISuikaDbService SuikaDbService { get; set; }

        public PostTournamentService()
        {
            //SuikaDbService = suikaDbService;
            _suikaDbService = new SuikaDbService(new LeiaContext()); ;
        }


        private static GlickoPlayer ConvertPlayerToGlicko(Player player)
        {
            return new GlickoPlayer(player.Rating, 100);
        }


        public async Task<List<PlayerTournamentSession>> LoadLeaderboardForPlayerFromDb(LeiaContext context, Guid requestingPlayerGuid, int tournamentId)
        {
            // Load ALL player sessions from that tournament
            var allPlayerTournamentSessions = await context.PlayerTournamentSession.Where(e => e.TournamentSessionId == tournamentId).ToListAsync();
            // Find the tournament type of the requesting player
            var tournamentTypeId = allPlayerTournamentSessions.First(s => s.PlayerId == requestingPlayerGuid).TournamentTypeId;

            if (tournamentTypeId < 0)
            {
                throw new Exception($"Could not find any player session for player {requestingPlayerGuid} in tournament {tournamentId}");
            }
            // Load the tournament type and see the maximum number of leaderboard spots
            var tournamentType = await context.TournamentTypes.FindAsync(tournamentTypeId);
            if (tournamentType == null)
            {
                throw new Exception($"No such tournament type {tournamentTypeId}");
            }
            
            return CalculateLeaderboardForPlayer(requestingPlayerGuid, allPlayerTournamentSessions, tournamentType, tournamentId);
        }

        public static List<PlayerTournamentSession> CalculateLeaderboardForPlayer(Guid requestingPlayerGuid, IEnumerable<PlayerTournamentSession> allPlayerTournamentSessions, TournamentType tournamentType, int tournamentId)
        {
            var playerTournamentSessionsByJoinTime = allPlayerTournamentSessions.OrderBy(s => s.JoinTime).ToList();
            var requestingPlayerSession = playerTournamentSessionsByJoinTime.First(s => s.PlayerId == requestingPlayerGuid);
            var indexOfRequestingPlayer = playerTournamentSessionsByJoinTime.IndexOf(requestingPlayerSession);
            var leaderboardPlayerCount = tournamentType.NumberOfPlayers.Value;

            var playersEarlierThanRequestingPlayer = Math.Min(indexOfRequestingPlayer, leaderboardPlayerCount - 1);
            var playersLaterThanRequestingPlayer = Math.Min(playerTournamentSessionsByJoinTime.Count - indexOfRequestingPlayer - 1, leaderboardPlayerCount - 1 - playersEarlierThanRequestingPlayer);
            var startIndex = indexOfRequestingPlayer - playersEarlierThanRequestingPlayer;
            var endIndex = indexOfRequestingPlayer + playersLaterThanRequestingPlayer;


            var leaderboard = playerTournamentSessionsByJoinTime.Skip(startIndex).Take(leaderboardPlayerCount).ToList();
            leaderboard.Sort();
            leaderboard.Reverse();
            return leaderboard;
        }


        /// <summary>
        /// Calculates the new ranks for players according to their tournament leaderboard
        /// Uses the context to load data from the database
        /// </summary>
        public Dictionary<Guid, int> CalculatePlayersRatingFromTournament(LeiaContext context, TournamentSession tournament)
        {
            var allTournamentTypes = context.TournamentTypes.ToList();
            return CalculatePlayersRatingFromTournament(tournament, allTournamentTypes);
        }

        /// <summary>
        /// Calculates the new ranks for players according to their tournament leaderboard
        /// This function does not load data from the database - so it can be unit-tested
        /// </summary>
        public Dictionary<Guid, int> CalculatePlayersRatingFromTournament(TournamentSession tournament, IEnumerable<TournamentType> allTournamentTypes)
        {
            var result = new Dictionary<Guid, int>();
            var allTournamentTypesById = allTournamentTypes.ToDictionary(tt => tt.TournamentTypeId);
            // Each session of a player has its own leaderboard according to the tournament type.
            // players in the same tournament type share the same leaderboard
            var leaderboardPerTournamentTypeId = new Dictionary<int, List<TournamentGlickoRatingCalculationEntry>>();
            // We will iterate the player sessions from best score to worst
            var allPlayerSessionsSortedByScore = tournament.PlayerTournamentSessions.OrderByDescending(s => s.PlayerScore).ToList();
            // Cache players by GUID for quick access
            var allPlayerGuids = allPlayerSessionsSortedByScore.Select(s => s.PlayerId).ToHashSet();
            var playerByGuid = tournament.Players.Where(p => allPlayerGuids.Contains(p.PlayerId)).ToDictionary(p => p.PlayerId);
            // Cache players as "Glicko players" for quick access
            var glickoPlayersByGuid = playerByGuid.Values.ToDictionary(p => p.PlayerId, p => ConvertPlayerToGlicko(p));

            // Iterate all the sessions in the tournament
            for (var i = 0; i < allPlayerSessionsSortedByScore.Count(); i++)
            {
                var playerSession = allPlayerSessionsSortedByScore[i];
                var score = playerSession.PlayerScore.HasValue ? playerSession.PlayerScore.Value : 0;
                // If the leaderboard for this tournament type is not cached, then cache it!
                if (!leaderboardPerTournamentTypeId.TryGetValue(playerSession.TournamentTypeId, out var playerLeaderboard))
                {
                    playerLeaderboard = CalculateLeaderboardForPlayer(playerSession.PlayerId, allPlayerSessionsSortedByScore, 
                        allTournamentTypesById[playerSession.TournamentTypeId], playerSession.TournamentSessionId).Select(s =>
                        {
                            return new TournamentGlickoRatingCalculationEntry
                            {
                                score = s.PlayerScore.HasValue ? s.PlayerScore.Value : 0,
                                player = playerByGuid[s.PlayerId],
                                opponent = new GlickoOpponent(glickoPlayersByGuid[s.PlayerId], s.PlayerScore.HasValue ? s.PlayerScore.Value : 0)
                            };
                        }).ToList();
                    leaderboardPerTournamentTypeId[playerSession.TournamentTypeId] = playerLeaderboard;
                }
                // Set up the player 'glicko result' which is either 0 or 1, where 0 is worse score than player, 1 is better score 
                var isBetterThanCurrentPlayer = true;
                foreach (var entry in playerLeaderboard)
                {
                    if (entry.player.PlayerId == playerSession.PlayerId)
                    {
                        isBetterThanCurrentPlayer = false;
                    }
                    entry.opponent.Result = isBetterThanCurrentPlayer ? 0 : 1;
                }
                // Take all opponents from the current leaderboard except the current player
                var opponents = playerLeaderboard.Where(e => e.player.PlayerId != playerSession.PlayerId).Select(e => e.opponent).ToList();

                // Take the current player's glicko-player
                var glickoPlayer = glickoPlayersByGuid[playerSession.PlayerId];
                // Calculate current player's new rank according to the suitable leaderboard of their tournament type
                var glicko = GlickoCalculator.CalculateRanking(glickoPlayer, opponents);
                // Add to results
                result.Add(playerSession.PlayerId, (int)Math.Round(glicko.Rating));
            }
            return result;
        }

        public async Task CloseTournament(TournamentSession? tournament)
        {

            ArgumentNullException.ThrowIfNull(tournament);
            if (tournament is null) Trace.WriteLine($"In PostTournamentService.CloseTournament, tournament: {tournament?.TournamentSessionId}, was null");

            tournament.IsOpen = false;
            tournament.Endtime = DateTime.UtcNow;
            
            var newPlayerRatings = CalculatePlayersRatingFromTournament(_suikaDbService.LeiaContext, tournament);
            try
            {
                _suikaDbService.LeiaContext.Entry(tournament).State = EntityState.Modified;
                var updatedPlayerTournament = _suikaDbService.LeiaContext.Tournaments.Update(tournament);

                foreach (var playerRaitingPair in newPlayerRatings)
                {
                    _suikaDbService.LeiaContext.Players.Find(playerRaitingPair.Key).Rating = playerRaitingPair.Value;
                }


                var saved = await _suikaDbService.LeiaContext.SaveChangesAsync();
                if (saved > 0)
                {

                    var granted = await GrantTournamentEggs(updatedPlayerTournament.Entity);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }
        }

        public async Task<List<double?>> GrantTournamentEggs(TournamentSession tournament)
        {
            if (tournament == null) return [];
            var playersByScore = tournament?.PlayerTournamentSessions.OrderByDescending(pt => pt.PlayerScore).Select(pt =>
            {
                return tournament?.Players.FirstOrDefault(p => p.PlayerId == pt.PlayerId);
            }).ToList();

            /*var firstReward = await _suikaDbService.LeiaContext.Rewards.FirstOrDefaultAsync( r => r.RewardName == "EggsFor1Place");
            var secondReward = await _suikaDbService.LeiaContext.Rewards.FirstOrDefaultAsync( r => r.RewardName == "EggsFor2Place");

            var rewards = new List<Reward>() {firstReward,secondReward};*/
            var rewards = _suikaDbService.LeiaContext.Rewards.Where(r => r.RewardName.Contains("Eggs")).OrderBy(r => r.ForPosition).ToList();

            var updated = new List<double?>()
                ;
            for (int i = 0; i < playersByScore?.Count; i++)
            {
                var player = playersByScore[i];
                var reward = rewards[i];
                if (player != null && reward != null)
                {
                    var newBalance = await UpdateBalanceWithReward(reward, player);
                    updated.Add(newBalance);
                }
            }
            return updated;
        }

        //turn this into an endpoint and get player as well
        public async Task<(double?, bool?, double?)> GrantTournamentPrizes(TournamentSession? tournament, Player? player)
        {
            if (tournament == null || player == null)
            {
                await _suikaDbService.Log($"In PostTournamentService.GrantTournamentPrizes, tournament: {tournament?.TournamentSessionId}, or Player {player?.PlayerId} were null");
                return (-1, false, -1);
            }

            
            var playerSession = tournament.PlayerTournamentSessions.FirstOrDefault(p => p.PlayerId == player.PlayerId);
            var tournamentType = playerSession.TournamentType;
            var leaderboard = PostTournamentService.CalculateLeaderboardForPlayer(player.PlayerId, tournament.PlayerTournamentSessions, playerSession.TournamentType, tournament.TournamentSessionId);
            var isTournamentClosed = leaderboard.Count >= tournamentType.NumberOfPlayers && leaderboard.All(s => s.PlayerScore != null);
            if (!isTournamentClosed)
            {
                return (-1, false, -1);
            }
            var playersByScore = leaderboard.Select(s => s.Player).ToList();


            var individualPlayerTournament = _suikaDbService.LeiaContext.PlayerTournamentSession.FirstOrDefault(pt => pt.TournamentSession.TournamentSessionId == tournament.TournamentSessionId && pt.PlayerId == player.PlayerId);
            if (individualPlayerTournament == null)
            {
                await _suikaDbService.Log($"In PostTournamentService.GrantTournamentPrizes, the PlayerTournamentSession for: Player - {player?.PlayerId}, Tournament - {tournament?.TournamentSessionId}, is null", player.PlayerId);
                return (-1, false, -1);
            }

            var rewards = _suikaDbService.LeiaContext.TournamentTypes.Include(tt => tt.Reward).FirstOrDefault(tt => tt.TournamentTypeId == individualPlayerTournament.TournamentTypeId)?.Reward;

            var playerPosition = playersByScore?.IndexOf(player) + 1; // +1 because the index is 0 based and positions are 1 based
            if (playerPosition != -1) //if player position was found
            { //update with the corresponding reward

                // TODO make this compatible with multiple rewards per player position (send list of rewards to UpdateBalanceWithReward) 
                var reward = rewards?.FirstOrDefault(r => r.ForPosition == playerPosition);


                if (reward != null) // if reward was found
                {
                    var updated = await UpdateBalanceWithReward(reward, player);
                    var wasClaimed = await MarkTournamentAsClaimed(tournament, player);
                    double? PTbalance = -1;

                    if (wasClaimed == true)
                    {
                        PTbalance = await GrantPurpleTokenReward(player, playerPosition);
                    }

                    return (updated, wasClaimed, PTbalance);
                }
            }

            return (-1, false, -1);
        }

        private async Task<double?> GrantPurpleTokenReward(Player player, int? playerPosition)
        {
            var rewards = _suikaDbService.LeiaContext.Rewards.Where(r => r.RewardName.Contains("PurpleToken")).OrderBy(r => r.ForPosition).ToList();

            var reward = rewards.FirstOrDefault(r => r.ForPosition == playerPosition);
            if (reward == null) return -1;

            var newBalance = await UpdateBalanceWithReward(reward, player);
            return newBalance;
        }

        private async Task<bool?> MarkTournamentAsClaimed(TournamentSession? tournament, Player player)
        {
            if (tournament == null || player == null) // make sure the parameters are not empty
            {
                Trace.WriteLine($"In PostTournamentService.MarkTournamentAsClaimed, tournament: {tournament?.TournamentSessionId}, or player: {player?.PlayerId}, were null");
                return false;
            }

            // get the PlayerTournamentSession for the player and tournament form the DB
            var dbPlayerTournament = await _suikaDbService.LeiaContext.PlayerTournamentSession.FirstOrDefaultAsync(pt => pt.TournamentSession.TournamentSessionId == tournament.TournamentSessionId && pt.PlayerId == player.PlayerId);

            if (dbPlayerTournament != null) // if we find it, update that the player has claimed the prizes
            {
                dbPlayerTournament.DidClaim = true;

                try
                {
                    _suikaDbService.LeiaContext.Entry(dbPlayerTournament).State = EntityState.Modified;

                    var savedPlayerTournament = _suikaDbService.LeiaContext.PlayerTournamentSession.Update(dbPlayerTournament);

                    var saved = await _suikaDbService?.LeiaContext?.SaveChangesAsync();

                    if (saved > 0) Trace.WriteLine($"In PostTournamentService.MarkTournamentAsClaimed, updated PlayerTournamentSession: Player - {savedPlayerTournament?.Entity?.PlayerId}, Tournament - {savedPlayerTournament?.Entity?.TournamentSession?.TournamentSessionId}, DidClaim - {savedPlayerTournament?.Entity?.DidClaim}");

                    return savedPlayerTournament?.Entity?.DidClaim;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                    throw;
                }
            }
            else // if we don't find it, log an error
            {
                Trace.WriteLine($"In PostTournamentService.MarkTournamentAsClaimed, the PlayerTournamentSession for: Player - {player?.PlayerId}, Tournament - {tournament?.TournamentSessionId}, is null");
                return false;
            }
        }

        private async Task<double?> UpdateBalanceWithReward(Reward? reward, Player? player)
        {
            if (reward == null || player == null)
            {
                await _suikaDbService.Log($"In PostTournamentService.UpdateBalanceWithReward, reward: {reward?.RewardName}, or player: {player?.PlayerId}, were null", player.PlayerId);
                return -1;
            }
            //check if the player even have balances
            /*var balances = _suikaDbService.LeiaContext.PlayerCurrencies.Where(pc => pc.PlayerId == player.PlayerId).ToList();

            if (balances?.Count > 0)
            {
                var balance = balances.FirstOrDefault(pc => pc.CurrenciesId == reward.CurrenciesId);
                if (balance != null) //if he already has a balance  , update it
                {
                    balance.CurrencyBalance += Convert.ToDouble(reward?.RewardAmount);

                    try
                    {
                        _suikaDbService.LeiaContext.Entry(balance).State = EntityState.Modified;
                        var savedBalance = _suikaDbService.LeiaContext.PlayerCurrencies.Update(balance);
                        var saved = await _suikaDbService?.LeiaContext?.SaveChangesAsync();
                        if (saved > 0) Trace.WriteLine($"In PostTournamentService.UpdateBalanceWithReward, updated PlayerCurrencies: Player - {savedBalance?.Entity?.PlayerId}, Currency - {savedBalance?.Entity?.CurrenciesId}, Amount - {savedBalance?.Entity?.CurrencyBalance}");

                        return savedBalance?.Entity?.CurrencyBalance;
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
                        CurrenciesId = reward.CurrenciesId,
                        CurrencyBalance = Convert.ToDouble(reward?.RewardAmount),
                        PlayerId = player.PlayerId
                    };
                    try
                    {
                        var savedBalance = _suikaDbService.LeiaContext.PlayerCurrencies.Add(newBalance);
                        var saved = await _suikaDbService?.LeiaContext?.SaveChangesAsync();
                        if (saved > 0) Trace.WriteLine($"In UpdateBalanceWithReward, saved new PlayerCurrencies: Player - {savedBalance?.Entity?.PlayerId}, Currency - {savedBalance?.Entity?.CurrenciesId}, Amount - {savedBalance?.Entity?.CurrencyBalance}");

                        return savedBalance?.Entity?.CurrencyBalance;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                        throw;
                    }
                }
            }*/

            var updated = await _suikaDbService.UpdatePlayerBalance(player?.PlayerId, reward?.CurrenciesId, reward?.RewardAmount);
            if (updated != null) return updated?.CurrencyBalance;
            else
            {
                await _suikaDbService.Log($"In PostTournamentService.UpdateBalanceWithReward, the player {player?.PlayerId} has no balances", player.PlayerId);
                return -1;
            }
        }
    }
}
