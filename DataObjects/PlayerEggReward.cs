using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class PlayerEggReward
    {
        [Key]
        public int PlayerEggRewardId { get; set; }
        public DateTime StartDate { get; set; }
        public bool IsActive { get; set; }
        public Guid PlayerId { get; set; }
        [ForeignKey(nameof(PlayerId))]
        public Player Player { get; set; }
    }
}
