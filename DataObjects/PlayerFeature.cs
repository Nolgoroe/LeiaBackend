using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class PlayerFeature
    {
        [Key]
        public int PlayerFeatureId { get; set; }
        public int FeatureId { get; set; }
        [ForeignKey(nameof(FeatureId))]
        public Feature Feature { get; set; }       
        public Guid PlayerId { get; set; }
        [ForeignKey(nameof(PlayerId))]
        public Player Player { get; set; }
    }
}
