using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DataObjects
{
    public class Achievement
    {
        [Key]
        public int AchievementId { get; set; }
        public string AchievementName { get; set; }
        public Guid PlayerId { get; set; }
        [ForeignKey(nameof(PlayerId))]
        public Player Player { get; set; }

        [JsonIgnore]
        public List<AchievementElement> AchievementElements { get; set; } = [];
    }
}
