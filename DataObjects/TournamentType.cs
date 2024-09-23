using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class TournamentType
    {
        [Key]
        public int TournamentTypeId { get; set; }
        public string TournamentTypeName { get; set; }
    }
}
