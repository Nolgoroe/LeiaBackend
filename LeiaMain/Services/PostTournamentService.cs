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
        public Task<(double?, bool?)> GrantTournamentPrizes(TournamentSession? tournament, Player? player);

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


        private GlickoPlayer ConvertPlayerToGlicko(Player player)
        {
            return new GlickoPlayer(player.Rating, 100);
        }

        private Dictionary<Guid, int> CalculatePlayersRatingFromTournament(TournamentSession tournament)
        {
            var result = new Dictionary<Guid, int>();
            // convert all players to glickoplayers
            var glickoOpponents = new List<GlickoOpponent>(tournament.Players.Count);
            for (var i = 0; i < tournament.Players.Count; i++)
            {
                var corePlayer = tournament.Players[i];
                var glickoPlayer = ConvertPlayerToGlicko(corePlayer);
                var nullableScore = tournament.PlayerTournamentSessions[i].PlayerScore;
                var score = nullableScore.HasValue ? nullableScore.Value : 0;
                glickoOpponents[i] = new GlickoOpponent(glickoPlayer, score);
            }
            for (var i = 0; i < tournament.Players.Count; i++)
            {
                var corePlayer = tournament.Players[i];
                var currentGlickoPlayer = glickoOpponents[i];
                var glicko = GlickoCalculator.CalculateRanking(currentGlickoPlayer, glickoOpponents);
                result.Add(corePlayer.PlayerId, (int)Math.Round(glicko.Rating));
            }
            return result;
        }

        public async Task CloseTournament(TournamentSession? tournament)
        {
           
            ArgumentNullException.ThrowIfNull(tournament);
            if (tournament is null) Trace.WriteLine($"In PostTournamentService.CloseTournament, tournament: {tournament?.TournamentSessionId}, was null");

            tournament.IsOpen = false;
            var dbTournamentData = await _suikaDbService.LeiaContext.TournamentsData.FindAsync(tournament.TournamentDataId);
            if (dbTournamentData == null)
            {
                Trace.WriteLine($"CloseTournament: Error got `null` tournament data from {tournament.TournamentSessionId}");
                return;
            }
            dbTournamentData.TournamentEnd = DateTime.Now;
            tournament.TournamentData = dbTournamentData;
            var newPlayerRatings = CalculatePlayersRatingFromTournament(tournament);
            try
            {
                _suikaDbService.LeiaContext.Entry(dbTournamentData).State = EntityState.Modified;
                var updatedPlayerTournamentData = _suikaDbService.LeiaContext.TournamentsData.Update(dbTournamentData);

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
        public async Task<(double?,bool?)> GrantTournamentPrizes(TournamentSession? tournament, Player? player)
        {
            if (tournament == null || player == null)
            {
                Trace.WriteLine($"In PostTournamentService.GrantTournamentPrizes, tournament: {tournament?.TournamentSessionId}, or Player {player?.PlayerId} were null");
                return (-1,false);
            }

            var playersByScore = tournament?.PlayerTournamentSessions.OrderByDescending(pt => pt.PlayerScore).Select(pt =>
            {
                return tournament?.Players.FirstOrDefault(p => p.PlayerId == pt.PlayerId);
            }).ToList();

            var rewards = _suikaDbService.LeiaContext.TournamentTypes.Where(tt => tt.TournamentTypeId == tournament.TournamentData.TournamentTypeId)
                .Include(tt => tt.Reward)
               .FirstOrDefault()?.Reward;

            var playerPosition = playersByScore?.IndexOf(player) + 1; // +1 because the index is 0 based and positions are 1 based
            if (playerPosition != -1) //if player position was found
            { //update with the corresponding reward
                var reward = rewards?.FirstOrDefault(r => r.ForPosition == playerPosition);
                if (reward != null) // if reward was found
                {
                    var updated = await UpdateBalanceWithReward(reward, player);
                    var wasClaimed = await MarkTournamentAsClaimed(tournament, player); 
                    return (updated, wasClaimed);
                }
            }
            // return -1 if updating failed
            return (-1,false);
        }

        private async Task<bool?> MarkTournamentAsClaimed(TournamentSession? tournament, Player player)
        {
            if (tournament == null || player == null) // make sure the parameters are not empty
            {
                Trace.WriteLine($"In PostTournamentService.MarkTournamentAsClaimed, tournament: {tournament?.TournamentSessionId}, or player: {player?.PlayerId}, were null");
                return false;
            }

            // get the PlayerTournamentSession for the player and tournament form the DB
            var dbPlayerTournament = await _suikaDbService.LeiaContext.PlayerTournamentSession.FirstOrDefaultAsync(pt => pt.TournamentSessionId == tournament.TournamentSessionId && pt.PlayerId == player.PlayerId);

            if (dbPlayerTournament != null) // if we find it, update that the player has claimed the prizes
            {
                dbPlayerTournament.DidClaim = true;

                try
                {
                    _suikaDbService.LeiaContext.Entry(dbPlayerTournament).State = EntityState.Modified;

                    var savedPlayerTournament = _suikaDbService.LeiaContext.PlayerTournamentSession.Update(dbPlayerTournament);

                    var saved = await _suikaDbService?.LeiaContext?.SaveChangesAsync(); 

                    if (saved > 0) Trace.WriteLine($"In PostTournamentService.MarkTournamentAsClaimed, updated PlayerTournamentSession: Player - {savedPlayerTournament?.Entity?.PlayerId}, Tournament - {savedPlayerTournament?.Entity?.TournamentSessionId}, DidClaim - {savedPlayerTournament?.Entity?.DidClaim}");

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
                Trace.WriteLine($"In PostTournamentService.UpdateBalanceWithReward, reward: {reward?.RewardName}, or player: {player?.PlayerId}, were null");
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
                Trace.WriteLine($"In PostTournamentService.UpdateBalanceWithReward, the player {player?.PlayerId} has no balances");
                return -1;
            }
        }
    }
}
