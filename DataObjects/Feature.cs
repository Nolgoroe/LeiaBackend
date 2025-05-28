using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class Feature
    {
        [Key]
        public int FeatureId { get; set; }
        public string Name { get; set; }
        public int PlayerLevel { get; set; }
    }
}
