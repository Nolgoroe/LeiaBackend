using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class GivenPlayerLevelReward
    {
        [Key]
        public int GivenLevelRewardId { get; set; }       
        public int LevelRewardId { get; set; }
        [ForeignKey(nameof(LevelRewardId))]
        public LevelReward LevelReward { get; set; }
        public DateTime GivenDate { get; set; }
        public Guid PlayerId { get; set; }
        [ForeignKey(nameof(PlayerId))]
        public Player Player { get; set; }
    }
}
