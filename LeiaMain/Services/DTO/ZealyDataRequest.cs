using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DTO
{
    public class ZealyDataRequest
    {
        [Required]
        public Guid PlayerId { get; set; }

        [Required, MaxLength(200)]
        public string PlayerName { get; set; }

        [Required, MaxLength(200)]
        public string TaskName { get; set; }

        [MaxLength(1000)]
        public string TaskDescription { get; set; } = "";

        [Required]
        public int XpAmount { get; set; }
    }
}
