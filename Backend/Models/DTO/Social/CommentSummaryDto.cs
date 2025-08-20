namespace Backend.Models.DTO.Social
{
    public class CommentSummaryDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? UserDisplayName { get; set; }
        public int LikeCount { get; set; }
        public int ReplyCount { get; set; }
    }
}