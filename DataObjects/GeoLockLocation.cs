using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class GeoLockLocation
    {
        public int Id { get; set; }

        [Required]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "CountryCode must be exactly 2 letters.")]
        public string CountryCode { get; set; }
    }
}
