using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Game.Relationships
{
    public class GameRemaster
    {
        [Required]
        public Guid OriginalGameId { get; set; }
        
        [Required]
        public Guid RemasterGameId { get; set; }

        [ForeignKey("OriginalGameId")]
        public virtual Game OriginalGame { get; set; } = null!;
        
        [ForeignKey("RemasterGameId")]
        public virtual Game RemasterGame { get; set; } = null!;
    }
}