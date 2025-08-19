using System.ComponentModel.DataAnnotations;

namespace Backend.Models.DTO.Social
{
    public class CommentDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
        public string Content { get; set; } = string.Empty;

        public Guid? ReviewId { get; set; }
        public Guid? GameListId { get; set; }
        public Guid? ParentCommentId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string? UserDisplayName { get; set; }
        public int LikeCount { get; set; }
        public int ReplyCount { get; set; }

        public UserSummaryDto? User { get; set; }
        public List<CommentSummaryDto>? Replies { get; set; }
    }
}