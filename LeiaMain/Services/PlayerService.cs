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
using System;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Services
{
    public interface IPlayerService
    {
      
        public Task<PlayerDailyReward> CheckForDailyReward(Guid playerId);
        public Task<bool> CheckForHourlyReward(Guid playerId);
        public Task<Achievement> GetPlayerAchievements(Guid playerId);
       
        public Task<List<EggReward>> CheckPlayerEggReward(Guid playerId);
        public Task<List<Feature>> CheckPlayerFeature(Guid playerId);
        public Task<List<int>> CheckPlayerFTUE(Guid playerId);
        public Task<List<LevelReward>> CheckPlayerLevelRewords(Guid playerId);
        public Task<bool> CheckPlayerLevelByExp(Guid playerId);
        public Task<PlayerProfileData> GetPlayerProfileData(Guid playerId);

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
        //public int consecutiveDays { get; set; }
        public bool isGiveReward { get; set; }

    }

    public record EggRewardDTO
    {

        public string currency { get; set; }
        public int rewardAmount { get; set; }
       

    }

    public record FeatureDTO
    {
        public string FeatureName { get; set; } 
        public int PlayerLevel { get; set; }

    }

    public record LevelRewardDTO
    {

        public string currency { get; set; }
        public int rewardAmount { get; set; }
        public int? FeatureId { get; set; }
        public string FeatureName { get; set; }


    }

    public record ProfileDataDTO
    {
        public string PlayerId { get; set; }
        public int? PlayerPictureId { get; set; }
        public int? WinCounte { get; set; }
        public int? FavoriteGameTypeId { get; set; }
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
                DateTime currDate = DateTime.UtcNow;
                DateTime today = DateTime.UtcNow.Date;
                var dailyReward = _suikaDbService.LeiaContext.PlayerDailyRewards.Where(r => r.PlayerId == playerId && r.IsActive == true).FirstOrDefault();                                          
                PlayerDailyReward reward =  new PlayerDailyReward();
                if (dailyReward != null)
                {
                    DateTime startForDailyReward = (DateTime)dailyReward?.LastClaimDate?.Date.AddDays(1).AddHours(4);
                    DateTime endForDailyReword = startForDailyReward.AddHours(24);

                    if (dailyReward.LastClaimDate?.Date == today)
                        {
                        reward.CurrentRewardDay = 0;
                        }
                    else
                        {
                        if (currDate < startForDailyReward)
                        {
                            reward.CurrentRewardDay = 0;
                        }
                        else if (currDate > startForDailyReward && currDate < endForDailyReword || dailyReward?.LastClaimDate?.TimeOfDay < TimeSpan.FromHours(4))
                        {
                           
                            int lastRewardDay = dailyReward.CurrentRewardDay; ;
                            int currentRewardDay = lastRewardDay != dailyReward.ConsecutiveDays ? lastRewardDay + 1 : 1;
                            var updated = _suikaDbService.UpdatePlayerDailyRewards(dailyReward.PlayerDailyRewardId, currentRewardDay);
                            reward.CurrentRewardDay = currentRewardDay;
                            
                        }

                        else
                        {
                            reward.CurrentRewardDay = 1;
                            var updated = _suikaDbService.UpdatePlayerDailyRewards(dailyReward.PlayerDailyRewardId, 1);
                        }
                    }
                }
                else
                {
                    reward.CurrentRewardDay = 1;
                    reward.ConsecutiveDays = 1;                   
                    var added = _suikaDbService.AddPlayerDailyRewards(playerId);
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

                DateTime currDate = DateTime.UtcNow;
                var hourlyReward = _suikaDbService.LeiaContext.PlayerHourlyRewards.Where(r => r.PlayerId == playerId && r.IsActive == true).FirstOrDefault();
                if (hourlyReward != null)
                {
                    DateTime startHourlyReword = hourlyReward.LastClaimDate.AddHours(24);
                    if (currDate < startHourlyReword)
                    {
                        return false;
                    }
                    else
                    {
                        var updated = _suikaDbService.UpdatePlayerHourlyRewards(hourlyReward.HourlyRewardId);
                        return true;
                    }
                }
                else
                {
                    var added = _suikaDbService.AddPlayerHourlyRewards(playerId);
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
                    var achievementElements = _suikaDbService.LeiaContext.GivenPlayerAchievements.Where(r => r.AchievementId == achievement.AchievementId).ToList();
                    if(achievementElements.Count > 0)
                    {
                        achievement.GivenPlayerAchievements = achievementElements; 
                    }
                    return achievement;
                }
                else
                {
                    return new Achievement();

                }
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
                var playerEggReward = _suikaDbService.LeiaContext.PlayerMonthlyEggs.Where(r => r.PlayerId == playerId && r.IsActive == true).FirstOrDefault();
                if (playerEggReward != null)
                {
                    if (playerEggReward.StartDate.Month == currMonth)
                    {
                        playerEggRewardId = playerEggReward.ActivePlayerEggsId;
                    }
                    else
                    {
                        var startNewMonthly = _suikaDbService.StartNewMonthlyEggCount(playerId, playerEggReward.ActivePlayerEggsId);
                        playerEggRewardId = startNewMonthly.Result.ActivePlayerEggsId;
                    }
                }
                else
                {
                    var startNewMonthly = _suikaDbService.StartNewMonthlyEggCount(playerId, 0);
                    playerEggRewardId = startNewMonthly.Result.ActivePlayerEggsId;

                }
                    var eggCount = _suikaDbService.LeiaContext.PlayerCurrencies.Where(r => r.PlayerId == playerId && r.CurrenciesId == (int)Enums.CurrenciesEnum.Eggs).FirstOrDefault();
                      
                if (eggCount != null && eggCount.CurrencyBalance > 0)
                {
                    var rewardsForCount = GetEggRewardsForTotalEggsCount((int)eggCount.CurrencyBalance);
                    if(rewardsForCount != null && rewardsForCount.Result.Count > 0)
                    {
                        
                            var givenEggRewards = _suikaDbService.LeiaContext.GivenPlayerEggRewards.Where(r => r.ActivePlayerEggsId == playerEggRewardId).ToList().Select(g => g.EggReward).ToList();
                        //var rewards = givenEggRewards.Select(g => g.EggReward).ToList();
                        foreach (var rfc in rewardsForCount.Result) 
                        { 
                            if(!givenEggRewards.Contains(rfc))
                                rewordsNotContained.Add(rfc);
                        }
                        if (rewordsNotContained.Count > 0)
                        {
                            var update = _suikaDbService.UpdateGivenPlayerEggRewards(playerEggReward.ActivePlayerEggsId, rewordsNotContained);
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

        public async Task<List<Feature>> CheckPlayerFeature(Guid playerId)
        {
            try
            {
                int playerLevel = _suikaDbService.LeiaContext.Players.Where(p => p.PlayerId == playerId).Select(p => p.Level).FirstOrDefault();
                var closedFeatures = await _suikaDbService.CheckObjectTimeForOpen(playerId, (int)Enums.CategoriesObjectsEnum.Features);
                var features = _suikaDbService.LeiaContext.Features.Where(f => f.PlayerLevel <= playerLevel && !closedFeatures.Contains(f.FeatureId)).OrderBy(f => f.PlayerLevel).ToList();
                var playerFeatures = _suikaDbService.LeiaContext.PlayerFeatures.Where(p =>  p.PlayerId == playerId).ToList().Select(p => p.Feature);
                
                if (playerFeatures == null) 
                {
                    var added = _suikaDbService.UpdatePlayerFeatures(playerId, features);
                    return features;
                }
                else
                {
                    List<Feature> result = new List<Feature>();
                    foreach (var item in features)
                    {
                        if (!playerFeatures.Contains(item))
                        {
                            result.Add(item);
                        }
                    }

                    if (result.Count > 0)
                    {
                        var added = _suikaDbService.UpdatePlayerFeatures(playerId, result);

                    }

                    return result;
                }
                   
            }
            catch (Exception ex)
            {

                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }
        
        }

        //public async Task<List<int>> CheckObjectTimeForOpen(Guid playerId, int category)
        //{ 
            
        //    List<int> result = new List<int>();
        //    List<int> timeManagerIds = new List<int>(); 
            
        //        var playerTimeManagers = _suikaDbService.LeiaContext.PlayerTimeManager.Where(p => p.PlayerId == playerId && p.CategoryObjectId == (int)Enums.CategoriesObjectsEnum.Features && p.IsActive == true);
        //        foreach (var timeManager in playerTimeManagers)
        //        {
        //            if (timeManager.EndTime >= DateTime.UtcNow)
        //            {
        //                result.Add(timeManager.TimeObjectId);
        //                timeManagerIds.Add(timeManager.TimeManagerId);
        //            }
        //        }

        //    var updated = _suikaDbService.UpdatePlayerTimeObject(timeManagerIds, playerId, result, category);
           
        //   return result;  
        //}
        public async Task<List<int>> CheckPlayerFTUE(Guid playerId)
        {

            try
            {
                List<int> result = new List<int>();
                
                var ftues = _suikaDbService.LeiaContext.FTUEs.OrderBy(f => f.SerialNumber).ToList();
                var playerFtues = _suikaDbService.LeiaContext.PlayerFtues.OrderByDescending(p => p.FTUEs.SerialNumber).Where(p => p.PlayerId == playerId).ToList();
                if(playerFtues != null)
                {
                    var notCompleteFtues = playerFtues.Where(f => f.IsComplete == false).ToList();
                    if (notCompleteFtues == null)
                    {
                        
                        int lastNumber = playerFtues.First().FTUEs.SerialNumber;
                        var nextFtue = ftues.Where(f => f.SerialNumber == lastNumber + 1).FirstOrDefault();
                        if(nextFtue != null)
                        {
                            var added = _suikaDbService.UpdatePlayerFTUEs(playerId, nextFtue.FtueId);
                            result.Add(nextFtue.FtueId);
                        }

                    }
                    else
                    {
                        foreach (var ftue in notCompleteFtues)
                        {
                            result.Add(ftue.FtueId);
                        }

                    }
                }

                else
                {
                   int firstFtueId = ftues.First().FtueId;
                    var added = _suikaDbService.UpdatePlayerFTUEs(playerId, firstFtueId);
                    result.Add(firstFtueId);
                }


                return result;
            }
            catch (Exception ex)
            {

                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }

        }

        public async Task<List<LevelReward>> CheckPlayerLevelRewords(Guid playerId)
        {
            try
            {
                //var check = await CheckPlayerLevelByExp(playerId);
                int playerLevel = _suikaDbService.LeiaContext.Players.Where(p => p.PlayerId == playerId).Select(p => p.Level).FirstOrDefault();
                var levelRewards = _suikaDbService.LeiaContext.LevelRewards.Where(l => l.Level <= playerLevel).OrderBy(l => l.Level).ToList();
                var givenLevelRewards = _suikaDbService.LeiaContext.GivenPlayerLevelRewards.Where(g => g.PlayerId == playerId).ToList().Select(g => g.LevelReward).ToList();
                List<LevelReward> result = new List<LevelReward>();
                if (levelRewards.Count > 0)
                {
                    if(givenLevelRewards != null)
                    {
                        
                        if (givenLevelRewards.Count > 0)
                        {
                            
                            foreach (var item in levelRewards)
                            {
                                if (!givenLevelRewards.Contains(item))
                                {
                                    result.Add(item);
                                }
                            }

                        }
                        if (result.Count > 0)
                        {
                            var added = _suikaDbService.UpdatePlayerLevelRewards(playerId, result);
                        }
                        
                    }
                    else
                    {
                        var added = _suikaDbService.UpdatePlayerLevelRewards(playerId, levelRewards);
                        result = levelRewards;
                    }
                  
                }
                return result;
                
            }
            catch (Exception ex)
            {

                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }

        }

        public async Task<bool> CheckPlayerLevelByExp(Guid playerId)
        {
            try
            {
                int playerLevel = _suikaDbService.LeiaContext.Players.Where(p => p.PlayerId == playerId).Select(p => p.Level).FirstOrDefault();               
                var totalExp = _suikaDbService.LeiaContext.Players.Where(p => p.PlayerId == playerId).Select(p => p.TotalExp).FirstOrDefault();
                int levelByExp = _suikaDbService.LeiaContext.UserMainProgression.Where(u => u.XPForUnity == totalExp).Select(u => u.UserLevel).FirstOrDefault();

                if(playerLevel != levelByExp)
                {
                    var updated = _suikaDbService.UpdatePlayerLevel(playerId, levelByExp);
                }

                return true;
            }
            catch (Exception ex)
            {

                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }

        }

        public async Task<PlayerProfileData> GetPlayerProfileData(Guid playerId)
        {
            try
            {
               var userProfile = _suikaDbService.LeiaContext.PlayerProfileData.Where(p => p.PlayerId == playerId).FirstOrDefault();
                return userProfile;             
            }
            catch (Exception ex)
            {

                Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                throw;
            }

        }
    }
}
