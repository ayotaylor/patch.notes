using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Game.Relationships
{
    public class GameRemake
    {
        [Required]
        public Guid OriginalGameId { get; set; }
        
        [Required]
        public Guid RemakeGameId { get; set; }

        [ForeignKey("OriginalGameId")]
        public virtual Game OriginalGame { get; set; } = null!;
        
        [ForeignKey("RemakeGameId")]
        public virtual Game RemakeGame { get; set; } = null!;
    }
}