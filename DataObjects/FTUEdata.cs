using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Collections.Generic;
using System.Numerics;

namespace DataObjects
{
    public class FTUEdata
    {
        /// <summary>
        /// Needs further characterization.
        /// Do not implement in DB yet
        /// </summary>
        
        public Guid PlayerId { get; set; }  
        public Player Player { get; set; }
        public DateTime TutStart { get; set; } //When did player start the tutorials 
        public DateTime TutEnd { get; set; } // When did player finish the tutorial 
        public int TutChurnRate { get; set; } // did player passed 1/2/3 clicks in the tutorial
       
        public bool BoosterClicks { get; set; } //   has clicked booster X
        public bool DidSubmitScore { get; set; } //  has the player passed the stage of submitting score

        //? public bool DidClickTournament { get; set; } //  has the player passed the stage of clicking on tournament
        public DateTime FirstTournamentStart { get; set; } //  When did the player start first tournament 

        public bool DidPlayerTakePrize { get; set; } //  has the clicked take prize button
        public bool DidPlayerClickPopup { get; set; } //  has the clicked popup
       
        /* // Game Analytics sending example
                public BLA[] blaArray = new BLA[6];

                public class BLA
                {
                    public string A = null; // the path Achivement:Kill:dothis
                    public string B = null; // the value - 120
                }


                private void bla2(BLA[] inBla)
                {
                    foreach (var bla in inBla)
                    {
                        //send to GA the bla.A and bla.b
                        GameAnalytics.NewDesignEvent(bla.A, bla.B);
                    }
                }
        */


    }
}
