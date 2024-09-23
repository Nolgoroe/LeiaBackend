using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class Currencies
    {
        [Key]
        public int CurrencyId{ get; set; }
        public string CurrencyName{ get; set; }

        public List<Player> Players { get; } = [];
        public List<PlayerCurrencies> PlayerCurrencies { get; } = [];

    }
}
