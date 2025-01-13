using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;


namespace DataObjects
{
    public class SessionData
    {
        [Key]
        public int SessionId { get; set; }
        [JsonIgnore]
        public List<TournamentSession> Tournaments { get; set; }  
        public Guid PlayerId { get; set; }
        [ForeignKey(nameof(PlayerId))]
        public Player Player { get; set; }
        public DateTime  SessionStart { get; set; } //  when was the app launched
        public DateTime SessionEnd { get; set; } //  when was the app closed
        public TimeSpan GameplaySessionTime { get; set; }
        public int SessionChurnRate { get; set; }
    }
}
