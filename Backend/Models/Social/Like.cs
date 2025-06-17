using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Game.Social
{
    public class Like : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        public Guid GameId { get; set; }

        [ForeignKey("UserId")]
        public virtual UserProfile User { get; set; } = null!;
        
        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;
    }
}