using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class PlayerExpByGameType
    {
        [Key]
        public int GameTypeToExpId { get; set; }
        public int GameTypeId { get; set; }
        [ForeignKey(nameof(GameTypeId))]
        public GameType GameType { get; set; }
        public double Exp { get; set; }
        public int? CurrencyId { get; set; }
        [ForeignKey(nameof(CurrencyId))]
        public Currencies Currencies { get; set; }
        public Guid PlayerId { get; set; }
        [ForeignKey(nameof(PlayerId))]
        public Player Player { get; set; }

    }
}
