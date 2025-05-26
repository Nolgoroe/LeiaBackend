using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class LevelReward
    {
        [Key]
        public int LevelRewardId { get; set; }
        public int Level { get; set; }
        public int CurrencyId { get; set; }
        [ForeignKey(nameof(CurrencyId))]
        public Currencies Currencies { get; set; }
        public double RewardAmount { get; set; }
        public int? FeatureId { get; set; }
    }
}
