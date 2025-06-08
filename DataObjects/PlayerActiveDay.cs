using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DataObjects
{
    public class PlayerActiveDay
    {
        [Key]
        public int Id { get; set; }

        public Guid PlayerId { get; set; }
        [JsonIgnore]
        public Player Player { get; set; }

        // we only care about the date portion
        public DateTime Date { get; set; }
    }
}
