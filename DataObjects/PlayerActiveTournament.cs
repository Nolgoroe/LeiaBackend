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
        /// <summary>
        /// This is the value of tournamentId inside PlayerActiveTournament incase they are still matchmaking
        /// </summary>
        public const int MATCH_MAKING_TOURNAMENT_ID = -1;

        [Key]
        public Guid PlayerId { get; set; }
        /// <summary>
        /// Should be -1 if a player is in queue
        /// Should be the tournament id 
        /// </summary>
        public int TournamentId { get; set; }

        /// <summary>
        ///  ???
        /// </summary>
        public double EntryFee { get; set; }

        public int CurrencyId { get; set; }

        public int TournamentTypeId { get; set; }

        public DateTime MatchmakeStartTime { get; set; }
        public DateTime JoinTournamentTime { get; set; }

        public bool DidCharge { get; set; }

        /// <summary>
        /// If the player is in the table but doesn't have a tournament yet, the `TournamentId` should be -1.
        /// Returns true if the player is in the queue.
        /// </summary>
        public bool IsStillMatchmaking() => TournamentId == MATCH_MAKING_TOURNAMENT_ID;
    }
}
