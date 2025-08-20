using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Social
{
    public class Follow : BaseEntity
    {
        [Required]
        public Guid FollowerId { get; set; }
        
        [Required]
        public Guid FollowingId { get; set; }
        
        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("FollowerId")]
        public virtual UserProfile Follower { get; set; } = null!;
        
        [ForeignKey("FollowingId")]
        public virtual UserProfile Following { get; set; } = null!;
    }
}