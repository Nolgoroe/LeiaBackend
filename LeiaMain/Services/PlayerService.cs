using System.Diagnostics;
using System.Text.Json;
using System.Timers;
using DataObjects;
using Microsoft.EntityFrameworkCore;
using DAL;
using Microsoft.Extensions.DependencyInjection;
using Azure.Core;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Services.Shared;

namespace Services
{
    public interface IPlayerService
    {
      
        public Task<PlayerDailyReward> CheckForDailyReward(Guid playerId);
        public Task<bool> CheckForHourlyReward(Guid playerId);
        public Task<Achievement> GetPlayerAchievements(Guid playerId);
       
        public Task<List<EggReward>> CheckPlayerEggReward(Guid playerId);

    }
    public record AchievementDTO 
    {
        public string AchievementName { get; set; }
        public List<AchievementElementDTO> AchievementElements { get; set; } = [];

    }

    public record AchievementElementDTO
    {
        
        public int ElementNameId { get; set; }
        public int? AmountNeeded { get; set; }
        public int? CurrentAmount { get; set; }
        public bool IsCompleted { get; set; }
       
    }

    public record DailyRewardDTO 
    {

        public int currentRewardDay { get; set; }
        public int consecutiveDays { get; set; }
        public bool isGiveReward { get; set; }

    }

    public record EggRewardDTO
    {

        public string currency { get; set; }
        public int rewardAmount { get; set; }
       

    }

    public class PlayerService : IPlayerService
    {

        private readonly ISuikaDbService _suikaDbService;
       

        public PlayerService()
        {           
            _suikaDbService = new SuikaDbService(new LeiaContext()); ;
        }

        public async Task<PlayerDailyReward> CheckForDailyReward(Guid playerId)
        {
           
            try
            {

                //DateTime  today = DateTime.Today.AddHours(4.0);
                DateTime currDate = DateTime.UtcNow;
                DateTime today = DateTime.UtcNow.Date;
                DateTime startForDailyReward = today.AddHours(4);
                DateTime endForDailyReword = startForDailyReward.AddHours(24);
                var dailyReward = _suikaDbService.LeiaContext.PlayerDailyRewards.Where(r => r.PlayerId == playerId && r.IsActive == true).FirstOrDefault();
                PlayerDailyReward reward =  new PlayerDailyReward();
                if (dailyReward != null)
                {
                    if(dailyReward.LastClaimDate != null)
                    {
                        if (dailyReward.LastClaimDate?.Date == today)
                        {
                            reward.IsGiveReword = false;
                            
                        }
                        else
                        {
                            if (currDate < startForDailyReward)
                            {
                                reward.IsGiveReword = false;
                               
                            }
                            else if (currDate > startForDailyReward && currDate < endForDailyReword)
                                reward.IsGiveReword = true;
                            else
                            {

                                reward.IsGiveReword = false;  

                            }
                        }
                        
                    }
                    else
                    {
                       
                        reward.CurrentRewardDay = 1;
                        reward.ConsecutiveDays = 7;
                        reward.IsGiveReword = true; 
                          
                    }
                   
                }
                else
                {
                    reward.CurrentRewardDay = 1;
                    reward.ConsecutiveDays = 7;
                    reward.IsGiveReword = true;
                }
                return reward;
            }
            catch (Exception ex)
            {              
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }
            
        }

        public async Task<bool> CheckForHourlyReward(Guid playerId)
        {
            try
            {

                //DateTime  today = DateTime.Today.AddHours(4.0);
                DateTime currDate = DateTime.UtcNow;
                DateTime today = DateTime.UtcNow.Date;               
                DateTime endForHourlyReword = today.AddHours(24);
                var hourlyReward = _suikaDbService.LeiaContext.PlayerHourlyRewards.Where(r => r.PlayerId == playerId && r.IsActive == true).FirstOrDefault();
                if (hourlyReward != null)
                {
                    if (hourlyReward.StartDate > today && hourlyReward.StartDate < endForHourlyReword)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
               
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }
        }

        public async Task<Achievement> GetPlayerAchievements(Guid playerId)
        {

            try
            {              
                var achievement = _suikaDbService.LeiaContext.Achievements.Where(r => r.PlayerId == playerId).FirstOrDefault();
                if (achievement != null)
                {
                    var achievementaElements = _suikaDbService.LeiaContext.AchievementElements.Where(r => r.AchievementId == achievement.AchievementId).ToList();
                    if(achievementaElements.Count > 0)
                    {
                        achievement.AchievementElements = achievementaElements; 
                    }
                }
                return achievement;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }

        }

        
        public async Task<List<EggReward>> CheckPlayerEggReward(Guid playerId)
        {
            try
            {
                List<EggReward> rewordsNotContained = new List<EggReward>();
                int currMonth = DateTime.UtcNow.Month;
                int playerEggRewardId = 0;
                var playerEggReward = _suikaDbService.LeiaContext.PlayerEggRewards.Where(r => r.PlayerId == playerId && r.IsActive == true).FirstOrDefault();
                if (playerEggReward.StartDate.Month == currMonth)
                {
                    playerEggRewardId = playerEggReward.PlayerEggRewardId;
                }
                else
                {
                    var startNewMonthly = _suikaDbService.StartNewMonthlyEggCount(playerId, playerEggReward.PlayerEggRewardId);
                    playerEggRewardId = startNewMonthly.Result.PlayerEggRewardId;
                }
                var eggCount = _suikaDbService.LeiaContext.PlayerCurrencies.Where(r => r.PlayerId == playerId && r.CurrenciesId == (int)Enums.CurrenciesEnum.Eggs).FirstOrDefault();
                      
                if (eggCount != null && eggCount.CurrencyBalance > 0)
                {
                    var rewardsForCount = GetEggRewardsForTotalEggsCount((int)eggCount.CurrencyBalance);
                    if(rewardsForCount != null && rewardsForCount.Result.Count > 0)
                    {
                        
                            var givenEggRewards = _suikaDbService.LeiaContext.GivenPlayerEggRewards.Where(r => r.PlayerEggRewardId == playerEggReward.PlayerEggRewardId).ToList().Select(g => g.EggReward).ToList();
                        //var rewards = givenEggRewards.Select(g => g.EggReward).ToList();
                        foreach (var rfc in rewardsForCount.Result) 
                        { 
                            if(!givenEggRewards.Contains(rfc))
                                rewordsNotContained.Add(rfc);
                        }
                        if (rewordsNotContained.Count > 0)
                        {
                            var update = _suikaDbService.UpdateGivenPlayerEggRewards(playerEggReward.PlayerEggRewardId, rewordsNotContained);
                            return update.Result;
                        }
                        else 
                        {
                            return givenEggRewards; 
                        }

                    }
                    else
                    {
                        return new List<EggReward>();
                    }
                }
                else
                {
                    return new List<EggReward>();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }
        }

        public async Task<List<EggReward>> GetEggRewardsForTotalEggsCount(int eggCount)
        {
            try
            { 
            var eggRewards = _suikaDbService.LeiaContext.EggRewards.OrderBy(er => er.Count).ToList();
            int sum = 0;
            List<EggReward> result = new List<EggReward>(); 
            foreach (var reward in eggRewards) 
            {
                sum += reward.Count;
                if(sum > eggCount) break;
                result.Add(reward);

            }
            return result;
            }
            catch (Exception ex)
            {

                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }
        }
        
    }
}
