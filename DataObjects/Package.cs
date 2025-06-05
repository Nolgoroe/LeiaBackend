using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class Package
    {
        [Key]
        public int PackageId { get; set; }
        public string Name { get; set; }
        public decimal AmountUSD { get; set; }
        public decimal BonusAmountUSD { get; set; }
        public decimal Gems { get; set; }
        public int? OpenForTime { get; set; }
       
        
    }
}
