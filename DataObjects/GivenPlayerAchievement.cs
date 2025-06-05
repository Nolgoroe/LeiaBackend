using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class GivenPlayerAchievement
    {
        [Key]
        public int GivenAchievementId { get; set; }
        public int AchievementId { get; set; }
        [ForeignKey(nameof(AchievementId))]
        public Achievement Achievement { get; set; }
        public int AchievementsElementId { get; set; }
        [ForeignKey(nameof(AchievementsElementId))]
        public AchievementElement AchievementElement { get; set; }
        public DateTime? GivenDate { get; set; }
    }
}
