using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;


namespace DataObjects
{
    /// <summary>
    ///MtM table with payload(=extra data about each link. In this case, what score each player had in the tournament)
    /// </summary>
    // the name of the  class should be a combination of the names of both the classes that are being combined. Where the first name should be name of the class who's Id prop appears first. e.g in this combination, the Player is first in the props and thus first in  the name 
    public class PlayerTournamentSession : IComparable<PlayerTournamentSession>
    {
        public Guid PlayerId { get; set; }
        [JsonIgnore]
        public Player Player { get; set; } 
        public int TournamentSessionId { get; set; }
        //[JsonIgnore]
        [ForeignKey(nameof(TournamentSessionId))]
        public TournamentSession TournamentSession { get; set; }
        public int? PlayerScore { get; set; }  
        public bool? DidClaim { get; set; }
        public DateTime JoinTime { get; set; }
        public DateTime SubmitScoreTime { get; set; }
        public int Position { get; set; }
        public int TournamentTypeId { get; set; }
        [ForeignKey(nameof(TournamentTypeId))]
        public TournamentType TournamentType { get; set; }

        /// <summary>
        /// Compare by player score
        /// </summary>
        public int CompareTo(PlayerTournamentSession? other)
        {
            if (other == null) return 1;
            if (other.PlayerScore == null) return -1;
            if (PlayerScore == null) return -1;
            return PlayerScore.Value - other.PlayerScore.Value;
        }
    }
}
