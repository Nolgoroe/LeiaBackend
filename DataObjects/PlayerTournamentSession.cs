using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    /// <summary>
    ///MtM table with payload(=extra data about each link. In this case, what score each player had in the tournament)
    /// </summary>
    public class PlayerTournamentSession // the name of the  class should be a combination of the names of both the classes that are being combined. Where the first name should be name of the class who's Id prop appears first. e.g in this combination, the Player is first in the props and thus first in  the name 
    {
        //[Key]
        public Guid PlayerId { get; set; } // the name and type of the Id properties should be the same as in the connected classes (e.g. the Id prop from the Player class should be Guid PlayerId - the same as it is called in the Player class )
        public int TournamentSessionId { get; set; }
        public int PlayerScore { get; set; }  
    }
}
