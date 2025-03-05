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

        /// <summary>
        /// This is generated on every login, and is needed to access the player API
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// This needs to be saved on the client side in order to log in
        /// </summary>
        public string Secret { get; set; }
    }
}
