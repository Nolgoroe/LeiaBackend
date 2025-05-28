using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class UserMainProgression
    {
        [Key]
        public int UserLevel { get; set; }
        public double SessionsDesired { get; set; }
        public int SessionLength { get; set; }
        public int TimePerGame { get; set; }
        public double XpPerMinute { get; set; }
        public double? GamesPlayrd { get; set; }
        public double? XPRequired { get; set; }
        public double? XPForUnity { get; set; }
    }
}
