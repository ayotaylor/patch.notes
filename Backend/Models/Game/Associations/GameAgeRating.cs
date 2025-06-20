using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Models.Game.ReferenceModels;

namespace Backend.Models.Game.Associations
{
    public class GameAgeRating
    {
        [Required]
        public Guid GameId { get; set; }
        
        [Required]
        public Guid AgeRatingId { get; set; }

        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;
        
        [ForeignKey("AgeRatingId")]
        public virtual AgeRating AgeRating { get; set; } = null!;
    }
}