using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DataObjects
{
    /// <summary>
    /// This object contains auth tokens.
    /// Players MUST use auth tokens to communicate with the backend
    /// Auth tokens are one-time tokens created on a successful login
    /// </summary>
    public class PlayerAuthToken
    {
        [Key]
        [ForeignKey(nameof(Player))]
        public Guid PlayerId { get; set; }

        [JsonIgnore]
        public Player Player { get; set; }

        public string Token { get; set; }
    }
}
