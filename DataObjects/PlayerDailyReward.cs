using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace DataObjects
{
    public class PlayerDailyReward
    {
        [Key]
        public int PlayerDailyRewardId { get; set; }
        public DateTime? LastClaimDate { get; set; }
        public int CurrentRewardDay { get; set; }
        public int ConsecutiveDays { get; set; }
        public int DailyRewardsId { get; set; }
        [ForeignKey(nameof(DailyRewardsId))]
        public DailyReward DailyReward { get; set; }
        public bool IsActive { get; set; }
        public Guid PlayerId { get; set; }
        [ForeignKey(nameof(PlayerId))]
        public Player Player { get; set; }

        [JsonIgnore]
        public bool IsGiveReword { get; set; }
    }
}
