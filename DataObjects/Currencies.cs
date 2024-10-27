using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DataObjects
{
    public class Currencies
    {
        [Key]
        public int CurrencyId{ get; set; }
        public string CurrencyName{ get; set; }

        [JsonIgnore]
        public List<Player> Players { get; } = [];
        [JsonIgnore]
        public List<PlayerCurrencies> PlayerCurrencies { get; } = [];

    }
}
