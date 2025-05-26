using Services;
using Xunit;
using static Services.PlayerService;

namespace TestUserData
{
    public class TestUserData
    {
        [Fact]
        public void TestUserData()
        {
            Guid playerTestId = new Guid("4AB48AAC-FB8C-4343-B148-C8C262EDB3AD");
            var playerService = new PlayerService();
            var dailyReward = playerService.CheckForDailyReward(playerTestId);
            var hourlyReward = playerService.CheckForHourlyReward(playerTestId);
            var achievements = playerService.GetPlayerAchievements(playerTestId);
            var eggRewards = playerService.CheckPlayerEggReward(playerTestId);
            var feature = playerService.CheckPlayerFeature(playerTestId);
            var ftues = playerService.CheckPlayerFTUE(playerTestId);
            var levelReward = playerService.CheckPlayerLevelRewords(playerTestId);


        }
    }
}