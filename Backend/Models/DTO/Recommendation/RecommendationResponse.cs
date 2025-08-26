namespace Backend.Models.DTO.Recommendation
{
    public class RecommendationResponse
    {
        public List<GameRecommendation> Games { get; set; } = new();
        public string? ResponseMessage { get; set; }
        public List<string> FollowUpQuestions { get; set; } = new();
        public string? ConversationId { get; set; }
        public bool RequiresFollowUp { get; set; }
    }

    public class GameRecommendation
    {
        public Guid GameId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string? CoverUrl { get; set; }
        public decimal? Rating { get; set; }
        public List<string> Genres { get; set; } = new();
        public List<string> Platforms { get; set; } = new();
        public string Reasoning { get; set; } = string.Empty;
        public float ConfidenceScore { get; set; }
        public UserActivityMatch UserActivityMatch { get; set; } = new();
    }

    public class UserActivityMatch
    {
        public bool IsUserFavorite { get; set; }
        public bool IsUserLiked { get; set; }
        public bool IsFromFollowedUsers { get; set; }
        public List<string> FollowedUsersWhoLiked { get; set; } = new();
        public List<string> SimilarToUserFavorites { get; set; } = new();
        public List<string> SimilarToUserLikedGames { get; set; } = new();
        public List<string> SimilarToUserLikedReviews { get; set; } = new();
        public List<string> SimilarToUserLikedLists { get; set; } = new();
    }
}