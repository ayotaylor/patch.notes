using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Social
{
    public class GameList : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        [StringLength(100, ErrorMessage = "List name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public bool IsPublic { get; set; } = true;

        [ForeignKey("UserId")]
        public virtual UserProfile User { get; set; } = null!;
        
        public virtual ICollection<GameListItem> GameListItems { get; set; } = new List<GameListItem>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<GameListLike> Likes { get; set; } = new List<GameListLike>();
    }
}