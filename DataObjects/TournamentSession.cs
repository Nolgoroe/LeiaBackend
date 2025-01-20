using System;
using System.ComponentModel.DataAnnotations;

namespace DataObjects
{
    public class TournamentSession
    {
        public TimeSpan GetTournamentLifeTime() => DateTime.UtcNow - StartTime;

        public TournamentSession()
        {
            Players = new List<Player>();
        }

        [Key]
        public int TournamentSessionId { get; set; }          
        public List<Player> Players { get; } = [];
        //! THIS IS IMPORTANT!! THE MTM WITH PAYLOAD LINKING TABLES PROPERTIES, MUST ONLY BE { get; } AND INITIALIZED WITH = []; OR ELSE THE MTM CONNECTION DOESN'T WORK RIGHT
        public List<PlayerTournamentSession> PlayerTournamentSessions { get;} =  [];// a link to the MtM table 
        public DateTime StartTime { get; set; }
        public DateTime Endtime {  get; set; }
        public int TournamentSeed { get; set; }
        public bool IsOpen { get; set; }
        public int Rating { get; set; }
    }
}
