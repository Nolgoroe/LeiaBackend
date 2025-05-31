using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class ZealyData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }                // Auto‐incrementing integer PK

        [Required]
        public Guid PlayerId { get; set; }         // Zealy/Unity player GUID

        [Required, MaxLength(200)]
        public string PlayerName { get; set; }     // e.g. “avishy”

        [Required, MaxLength(200)]
        public string TaskName { get; set; }       // e.g. “TEST Quest”

        [MaxLength(1000)]
        public string TaskDescription { get; set; }// e.g. “Smoke‐test completion”

        [Required]
        public int XpAmount { get; set; }          // e.g. 100

        [Required]
        public DateTime CreatedAtUtc { get; set; } // Timestamp of insertion
    }
}
