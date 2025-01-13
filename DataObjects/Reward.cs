using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;


namespace DataObjects
{
    public class Reward
    {
        [Key]
        public int RewardId { get; set; }
        public string RewardName { get; set; }
        public int CurrenciesId { get; set; }
        public Currencies? Currencies { get; set; }
        public double? RewardAmount { get; set; }
        public int ForPosition { get; set; }
        [JsonIgnore]
        public List<TournamentType> TournamentType { get; } = [];

    }
}
