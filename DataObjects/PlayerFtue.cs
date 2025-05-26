using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class PlayerFtue
    {
        [Key]
        public int Id { get; set; }
        public int FtueId { get; set; }
        [ForeignKey(nameof(FtueId))]
        public FTUE FTUEs { get; set; }
        public Guid PlayerId { get; set; }
        [ForeignKey(nameof(PlayerId))]
        public Player Player { get; set; }
        public bool IsComplete { get; set; }
    }
}
