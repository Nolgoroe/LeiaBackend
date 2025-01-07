using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class League
    {
        [Key]
        public int LeagueId { get; set; }
        public string? Name { get; set; }
        public int? Rank { get; set; }
        public List<Player>? Players { get; set; }
        public List<Reward>? Rewards { get; set; }
    }
}
