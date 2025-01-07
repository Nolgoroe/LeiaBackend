using System.Text.Json.Serialization;

using DataObjects;

namespace DataObjects
{
    public class Player :PlayerData
    {
        public int Rating { get; set; }
        public int? LeagueId { get; set; }
        [JsonIgnore]
        public League? League { get; set; }
    }
}
