using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    /// <summary>
    ///MtM table with payload(=extra data about each link. In this case, what currency balance each player has)  
    /// </summary>
    public class PlayerCurrencies
    {
        public Guid PlayerId { get; set; }
        public int CurrencyId { get; set; }
        public double CurrencyBalance { get; set; }
    }
}
