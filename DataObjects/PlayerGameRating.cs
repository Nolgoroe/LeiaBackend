using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class PlayerGameRating
    {
        [Key]
        public int Id { get; set; }

        public Guid PlayerId { get; set; }
        public int GameId { get; set; } // unique ID for the game

        public double Rating { get; set; } = 1500;
        //public double RatingDeviation { get; set; } = 350;
        //public double Volatility { get; set; } = 0.06;

        [ForeignKey(nameof(PlayerId))]
        public Player Player { get; set; }
    }
}
