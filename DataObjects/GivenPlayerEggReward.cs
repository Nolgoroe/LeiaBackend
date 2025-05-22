using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class GivenPlayerEggReward
    {
        [Key]
        public int GivenEggRewordId { get; set; }
        public int PlayerEggRewardId { get; set; }
        [ForeignKey(nameof(PlayerEggRewardId))]
        public PlayerEggReward PlayerEggReward { get; set; }
        public int EggRewardId { get; set; }
        [ForeignKey(nameof(EggRewardId))]
        public EggReward EggReward { get; set; }
        public DateTime GivenDate { get; set; }

    }
}
