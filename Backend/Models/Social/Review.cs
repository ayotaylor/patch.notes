using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Social
{
    public class Review : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        public Guid GameId { get; set; }
        
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }
        
        [StringLength(1000, ErrorMessage = "Review text cannot exceed 1000 characters")]
        public string? ReviewText { get; set; }
        
        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual UserProfile User { get; set; } = null!;
        
        [ForeignKey("GameId")]
        public virtual Backend.Models.Game.Game Game { get; set; } = null!;
        
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<ReviewLike> Likes { get; set; } = new List<ReviewLike>();
    }
}