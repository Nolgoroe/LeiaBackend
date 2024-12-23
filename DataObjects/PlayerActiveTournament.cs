using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    /// <summary>
    /// Indicates a player's current tournament assignment
    /// 
    /// A player can only have one record of this, unlike `PlayerTournamnetSessions` which keeps the record forever and has multiple records per-player
    /// </summary>
    public class PlayerActiveTournament
    {
        [Key]
        public Guid PlayerId { get; set; }
        /// <summary>
        /// Should be -1 if a player is in queue
        /// Should be the tournament id 
        /// </summary>
        public int TournamentId { get; set; }

        public DateTime MatchmakeStartTime { get; set; }
        public DateTime JoinTournamentTime { get; set; }
    }
}
