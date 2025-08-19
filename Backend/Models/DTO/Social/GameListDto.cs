using System.ComponentModel.DataAnnotations;
using Backend.Models.DTO.Game;

namespace Backend.Models.DTO.Social
{
    public class GameListDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "List name cannot exceed 100 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public bool IsPublic { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string? UserDisplayName { get; set; }
        public int GameCount { get; set; }
        public int CommentsCount { get; set; }
        public int LikesCount { get; set; }

        public UserSummaryDto? User { get; set; }
        public List<GameListItemDto>? Games { get; set; }
        public List<CommentSummaryDto>? Comments { get; set; }
    }
}