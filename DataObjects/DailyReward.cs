using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class DailyReward
    {
        [Key]
        public int DailyRewardsId { get; set; }
        public string Name { get; set; }
        public int? Type { get; set; }
        public double? Amount { get; set; }
        public int? CurrencyId { get; set; }
        [ForeignKey(nameof(CurrencyId))]
        public Currencies Currencies { get; set; }
        public int? SerialNumber { get; set; }
    }
}
