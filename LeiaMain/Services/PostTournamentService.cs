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

        public static Dictionary<Guid, int> CalculatePlayersRatingFromTournament(TournamentSession tournament)
        {
            // First we build the context
            var playerEntries = new TournamentGlickoRatingCalculationEntry[tournament.Players.Count];
            var playerScoreByGuid = tournament.PlayerTournamentSessions.ToDictionary(p => p.PlayerId, p => p.PlayerScore);


            for (var i = 0; i < tournament.Players.Count; i++)
            {
                var player = tournament.Players[i];
                var glickoPlayer = ConvertPlayerToGlicko(player);
                var nullableScore = playerScoreByGuid[player.PlayerId];
                var score = nullableScore.HasValue ? nullableScore.Value : 0;

                var playerEntry = new TournamentGlickoRatingCalculationEntry
                {
                    score = score,
                    player = player,
                    opponent = new GlickoOpponent(glickoPlayer, 0)
                };
                playerEntries[i] = playerEntry;
            }
            // Sort the player entries by score from largest to smallest
            Array.Sort(playerEntries,
                delegate (TournamentGlickoRatingCalculationEntry x, TournamentGlickoRatingCalculationEntry y) { return -x.score.CompareTo(y.score); });


            var result = new Dictionary<Guid, int>();
            // For each player, calculate the rating compared to other players
            for (var i = 0; i < playerEntries.Length; i++)
            {
                var currentGlickoPlayer = playerEntries[i].opponent;
                // Set up the player 'glicko result' which is either 0 or 1, where 0 is worse score than player, 1 is better score 
                for (var j = 0; j < playerEntries.Length; j++)
                {
                    playerEntries[j].opponent.Result = j < i ? 0 : 1;
                }
                var playerOpponents = playerEntries.Where((e, idx) => idx != i).Select(e => e.opponent).ToList();
                var glicko = GlickoCalculator.CalculateRanking(currentGlickoPlayer, playerOpponents);
                result.Add(playerEntries[i].player.PlayerId, (int)Math.Round(glicko.Rating));
            }
            return result;
        }

        public async Task CloseTournament(TournamentSession? tournament)
        {

            ArgumentNullException.ThrowIfNull(tournament);
            if (tournament is null) Trace.WriteLine($"In PostTournamentService.CloseTournament, tournament: {tournament?.TournamentSessionId}, was null");

            tournament.IsOpen = false;
            tournament.Endtime = DateTime.UtcNow;
            
            var newPlayerRatings = CalculatePlayersRatingFromTournament(tournament);
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

            var playersByScore = tournament?.PlayerTournamentSessions.OrderByDescending(pt => pt.PlayerScore).Select(pt =>
            {
                return tournament?.Players.FirstOrDefault(p => p.PlayerId == pt.PlayerId);
            }).ToList();

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
