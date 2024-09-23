using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace DataObjects
{
    public class LobbyData
    {
        /// <summary>
        /// Needs further characterization.
        /// Do not implement in DB yet
        /// </summary>
        /// 
        public Guid PlayerId { get; set; }  
        public Player Player { get; set; }
        public DateTime LobbyStart { get; set; } //  when was the app launched
        public DateTime LobbyEnd { get; set; }
        public TimeSpan LobbyTime { get; set; }
        public string? SessionEndReason { get; set; }
        public int? LobbyClicksAmount  { get; set; }
        public int? SumOfPurchases   { get; set; }
        public int? AmountOfPurchases   { get; set; }
        public bool DidDailyCollect    { get; set; }
        public bool DidOpenSettings   { get; set; }
        public double? MoneyDeposited   { get; set; }
        public double? MoneyWithdrawn   { get; set; }
       
    }
}
