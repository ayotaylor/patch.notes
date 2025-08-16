using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Backend.Models.DTO.Game;

namespace Backend.Models.DTO.Social
{
    public class ReviewDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid GameId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Review text cannot exceed 1000 characters")]
        public string? ReviewText { get; set; }

        public DateTime ReviewDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string? UserDisplayName { get; set; }
        public string? GameTitle { get; set; }

        public UserSummaryDto? User { get; set; }
        public GameSummaryDto? Game { get; set; }
    }
}