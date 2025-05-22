using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class PlayerProfileData
    {
        [Key]
        public int PlayerLevelId { get; set; }
        public int? PlayerPictureId { get; set; }
        public int? WinCounte { get; set; }
        public int? FavoriteGameTypeId { get; set; }
        public Guid PlayerId { get; set; }
     
        [ForeignKey(nameof(PlayerId))]
        public Player Player { get; set; }

    }
}
