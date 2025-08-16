using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Social
{
    public class CommentLike : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        public Guid CommentId { get; set; }

        [ForeignKey("UserId")]
        public virtual UserProfile User { get; set; } = null!;
        
        [ForeignKey("CommentId")]
        public virtual Comment Comment { get; set; } = null!;
    }
}