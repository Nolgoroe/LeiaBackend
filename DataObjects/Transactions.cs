using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DataObjects
{
    public class Transactions
    {
        [Key]
        public int TransactionId { get; set; }
        public Guid PlayerId { get; set; }
        public Player Player { get; set; } 
        public DateTime TransactionDate{ get; set; }
        public int CurrenciesId { get; set; }
        public Currencies Currencies { get; set; }
        public int CurrencyAmount { get; set; }
        public int TransactionTypeId { get; set; }
        public TransactionType TransactionType { get; set; }
          

    }
}
