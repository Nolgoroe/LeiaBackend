using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class TournamentData
    {
        [Key]
        public int TournamentDataId { get; set; }

        public int TournamentTypeId { get; set; }
        public TournamentType TournamentType { get; set; }

        //public int SessionId { get; set; }
        //public SessionData Session { get; set; }

        public double EntryFee { get; set; }  
        public int EntryFeeCurrencyId { get; set; }
        public Currencies EntryFeeCurrency { get; set; }

        public double Earning { get; set; }
        public int EarningCurrencyId { get; set; }
        public Currencies EarningCurrency { get; set; }

        public DateTime TournamentStart { get; set; }
        public DateTime TournamentEnd { get; set; }

        private TimeSpan _tournamentLifeTime;

        public /*string*/ TimeSpan TournamentLifeTime
        {
            get
            {
                _tournamentLifeTime = DateTime.Now - TournamentStart;
                return _tournamentLifeTime/*.ToString("c")*/;
            }
            //set { _tournamentLifeTime = value; }
        }

        public int? NumBoosterClicked { get; set; }
        public int? NumPowerUps { get; set; }

    }


    

}
