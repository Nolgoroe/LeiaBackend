using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class EggReward
    {
        [Key]
        public int EggRewardId { get; set; }
        public int Count { get; set; }
        public int? CurrencyId { get; set; }
        [ForeignKey(nameof(CurrencyId))]
        public Currencies Currencies { get; set; }
        public int? RewardAmount { get; set; }
        
    }
}
