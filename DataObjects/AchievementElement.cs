using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class AchievementElement
    {
        [Key]
        public int AchievementsElementId { get; set; }
        public int ElementNameId { get; set; }
        public int? AmountNeeded{ get; set; }
        public int? CurrentAmount { get; set; }
        public bool IsCompleted { get; set; }
        public int AchievementId { get; set; }
        [ForeignKey(nameof(AchievementId))]
        public Achievement Achievement { get; set; }
    }
}
