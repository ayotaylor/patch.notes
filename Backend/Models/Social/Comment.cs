using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Social
{
    public class Comment : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
        public string Content { get; set; } = string.Empty;
        
        public Guid? ReviewId { get; set; }
        
        public Guid? GameListId { get; set; }
        
        public Guid? ParentCommentId { get; set; }

        [ForeignKey("UserId")]
        public virtual UserProfile User { get; set; } = null!;
        
        [ForeignKey("ReviewId")]
        public virtual Review? Review { get; set; }
        
        [ForeignKey("GameListId")]
        public virtual GameList? GameList { get; set; }
        
        [ForeignKey("ParentCommentId")]
        public virtual Comment? ParentComment { get; set; }
        
        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
        public virtual ICollection<CommentLike> Likes { get; set; } = new List<CommentLike>();
    }
}