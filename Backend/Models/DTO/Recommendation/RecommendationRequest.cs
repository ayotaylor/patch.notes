using System.ComponentModel.DataAnnotations;

namespace Backend.Models.DTO.Recommendation
{
    public class RecommendationRequest
    {
        [Required]
        [StringLength(500, ErrorMessage = "Query cannot exceed 500 characters")]
        public string Query { get; set; } = string.Empty;
        
        public string? ConversationId { get; set; }
        
        public int MaxResults { get; set; } = 10;
        
        public bool IncludeFollowedUsersPreferences { get; set; } = true;
    }
}