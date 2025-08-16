using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Social
{
    public class GameListItem : BaseEntity
    {
        [Required]
        public Guid GameListId { get; set; }
        
        [Required]
        public Guid GameId { get; set; }
        
        public int Order { get; set; } = 0;
        
        [StringLength(200, ErrorMessage = "Note cannot exceed 200 characters")]
        public string? Note { get; set; }

        [ForeignKey("GameListId")]
        public virtual GameList GameList { get; set; } = null!;
        
        [ForeignKey("GameId")]
        public virtual Backend.Models.Game.Game Game { get; set; } = null!;
    }
}