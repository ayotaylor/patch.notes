using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Game.Relationships
{
    public class SimilarGame
    {
        [Required]
        public Guid GameId { get; set; }
        
        [Required]
        public Guid SimilarGameId { get; set; }

        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;
        
        [ForeignKey("SimilarGameId")]
        public virtual Game SimilarGameRef { get; set; } = null!;
    }
}