using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DataObjects
{
    public class SessionData
    {
        [Key]
        public int SessionId { get; set; }
        //public List<Player>? Players { get; set; }
        [JsonIgnore]
        public List<TournamentSession> Tournaments { get; set; }  
        public Guid PlayerId { get; set; }
        public Player Player { get; set; }
      

        public DateTime  SessionStart { get; set; } //  when was the app launched
        public DateTime SessionEnd { get; set; } //  when was the app closed
        public TimeSpan GameplaySessionTime { get; set; }
        public int SessionChurnRate { get; set; }
        
        #region Old properties
        /*
        * public bool DidSubmitScore { get; set; }
        public bool BoosterClicks { get; set; }
        //public int Balance { get; set; } // balance of what 
        public int NumberOfGames { get; set; }
        public int NumberOfWins { get; set; }
        public int NumberOfLosses { get; set; }
        public int  GemsSpent { get; set; }
        public int GemsEarned { get; set; }
        public double LeiasSpent { get; set; }
        public double LeiasEarned { get; set; }
        public double USDTspent { get; set; }
        public double USDTearned { get; set; }
        public double USDCspent { get; set; }
        public double USDCearned { get; set; }
        public double USDspent { get; set; }
        public double USDearned { get; set; }
       */
        #endregion
    }
}
