using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class PlayerTimeManager
    {
        [Key]
        public int TimeManagerId { get; set; }        
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsActive { get; set; }
        public Guid PlayerId { get; set; }
        [ForeignKey(nameof(PlayerId))]
        public Player Player { get; set; }
        public int CategoryObjectId { get; set; }
        [ForeignKey(nameof(CategoryObjectId))]
        public CategoriesObject CategoriesObject { get; set; }
        public int TimeObjectId { get; set; }
    }
}
