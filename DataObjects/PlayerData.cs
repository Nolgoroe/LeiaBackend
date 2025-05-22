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
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        [Phone]
        public string? PhoneNumber { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public string? WalletAddress { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public bool IsRegistered { get => RegistrationDate is not null && PhoneNumber is not null; }
        public DateOnly? Birthday { get; set; }
        public int Level { get; set; }
        public string? DeviceType { get; set; }
        public string? DeviceId { get; set; }
        public string? OperatingSystemVersion { get; set; }
        public string? AppVersion { get; set; }
        public string? InstallSource { get; set; }
        public DateTime? InstallDate { get; set; }
        public string? AttributionData { get; set; }
        public int AvatarId  { get; set; }
        public string? UserCode { get; set; }
        public int? TotalExp { get; set; }
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
    }
}
