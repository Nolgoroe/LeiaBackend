using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
namespace DataObjects
{
    public class TournamentType
    {
        [Key]
        public int TournamentTypeId { get; set; }
        public string TournamentTypeName { get; set; }
        public double? Rake { get; set; }
        public int CurrenciesId { get; set; }
        public Currencies? Currencies { get; set; }
        public int? NumberOfPlayers { get; set; }
        public List<Reward> Reward { get; set; }
        public double? EntryFee { get; set; }
        public int? OpenForTime { get; set; }
    }
}
