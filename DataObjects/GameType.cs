
using System.ComponentModel.DataAnnotations;

namespace DataObjects
{
    public class GameType
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
