using System;
using System.ComponentModel.DataAnnotations;

namespace DataObjects
{
    public class TournamentSession
    {

        public TournamentSession()
        {
            Players = new List<Player>();
            // continue here, see how to create a countdown for the session lifetime

        }

        [Key]
        public int TournamentSessionId { get; set; }

        public List<Player> Players { get; } = [];
        public List<PlayerTournamentSession> PlayerTournamentSessions { get; } = [];// a link to the MtM table 
        //! THIS IS IMPORTANT!! THE MTM WITH PAYLOAD LINKING TABLES PROPERTIES, MUST ONLY BE { get; } AND INITIALIZED WITH = []; OR ELSE THE MTM CONNECTION DOESN'T WORK RIGHT

        public List<SessionData> Sessions { get; set; }

        public int TournamentDataId { get; set; }
        public TournamentData TournamentData { get; set; }

        #region old properties
        /*
                public List<PlayerTournamentSession> PlayerTournamentSession { get; set; } // a link to the MtM table

                public int TournamentTypeId { get; set; }
                public TournamentType TournamentType { get; set; }

                public int SessionId { get; set; }
                public SessionData Session { get; set; }

                public double EntryFee { get; set; }
                public int EntryFeeCurrencyId { get; set; }
                public Currencies EntryFeeCurrency { get; set; }

                public double Earning { get; set; }
                public int EarningCurrencyId { get; set; }
                public Currencies EarningCurrency { get; set; }

                public DateTime TournamentStart { get; set; }
                public DateTime TournamentEnd { get; set; }

                private TimeSpan _tournamentLifeTime;

                public *//*string*//* TimeSpan TournamentLifeTime
                {
                    get
                    {
                        _tournamentLifeTime = DateTime.Now - TournamentStart;
                        return _tournamentLifeTime*//*.ToString("c")*//*;
                    }
                    //set { _tournamentLifeTime = value; }
                }

                public int? NumBoosterClicked { get; set; }
                public int? NumPowerUps { get; set; }
        */

        #endregion
        
    }
}
