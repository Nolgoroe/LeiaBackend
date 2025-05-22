using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class FTUE
    {
        [Key]
        public int FtueId { get; set; }
        public string Name { get; set; }
        public bool IsComplete { get; set; }
        public int? Type { get; set; }
        public int? GameTypeId { get; set; }
        public int SerialNumber { get; set; }

    }
}
