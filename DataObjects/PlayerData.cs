using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
 
namespace DataObjects
{
    public class PlayerData
    {
        [Key]  
        public Guid PlayerId { get; set; }
        public string? UserType { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? WalletAddress { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public int? Age { get; set; }
        // public DateOnly Birthday { get; set; } // check if needed
        public int Level { get; set; }
        public string? DeviceType { get; set; }
        public string? OperatingSystemVersion { get; set; }
        public string? AppVersion { get; set; }
        public string? InstallSource { get; set; }
        public DateTime? InstallDate { get; set; }
        public string? AttributionData { get; set; }
      //  [JsonIgnore]
        public List<SessionData> Sessions { get; set; }
        [JsonIgnore]
        public List<TournamentSession> TournamentSessions { get; } = [];
        [JsonIgnore]
        public List<PlayerTournamentSession> PlayerTournamentSessions { get; } = []; // a link to the MtM table
        //! THIS IS IMPORTANT!! THE MTM WITH PAYLOAD LINKING TABLES PROPERTIES, MUST ONLY BE { get; } AND INITIALIZED WITH = []; OR ELSE THE MTM CONNECTION DOESN'T WORK RIGHT
        [JsonIgnore]
        public List<Currencies> Currencies { get; } = [];
        [JsonIgnore]
        public List<PlayerCurrencies> PlayerCurrencies { get; } = [];

        #region Old properties
        /*public int Gems { get; set; } // soft currency
        public int? Leias { get; set; }
        public int? USDT { get; set; }
        public int? USDC { get; set; }
        public int? USD { get; set; }
        public double? TotalMoneyDeposited { get; set; }
        public double? TotalMoneyCashedOut { get; set; }
        public double? TotalTournamentsFeesPaid { get; set; }
        public double? TotalTournamentsEarnings { get; set; }
        public int NumCashTournamentsPlayed { get; set; }
        public List<DateTime> LoginDays { get; set; }*/
        #endregion


    }
}
