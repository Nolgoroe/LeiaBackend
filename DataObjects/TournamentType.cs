using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DataObjects
{
    public class TournamentType
    {

        [Key]
        public int TournamentTypeId { get; set; }
        public string TournamentTypeName { get; set; }

        public double? Rake { get; set; }
        public int CurrenciesId { get; set; }
        public Currencies Currencies { get; set; }
        public int? NumberOfPlayers { get; set; }

        //[JsonIgnore]
        public List<Reward> Reward { get; set; }

        private double? entryFee;
        public double? EntryFee
        {
            get 
            {
                if (Reward != null && Reward.Count > 0)
                {
                    var sum = Reward.Select(r => r.RewardAmount).Sum();
                    var rakeAmount = 1 - (Rake / 100);
                    var entryFee = sum / rakeAmount / NumberOfPlayers;
                    return entryFee;
                }
                return entryFee; 
            }
            set { entryFee = value; }
        }
    }
}
