using Backend.Models.DTO.Recommendation;

namespace Backend.Services.Recommendation.Interfaces
{
    public interface ILanguageModel
    {
        Task<string> GenerateResponseAsync(string prompt, string? conversationContext = null);
        Task<List<string>> GenerateFollowUpQuestionsAsync(string query, List<GameRecommendation> recommendations);
        Task<string> ExplainRecommendationsAsync(List<GameRecommendation> recommendations, string originalQuery);
        Task<QueryAnalysis> AnalyzeQueryAsync(string query);
    }

    public class QueryAnalysis
    {
        public List<string> Genres { get; set; } = new();
        public List<string> Platforms { get; set; } = new();
        public List<string> GameModes { get; set; } = new();
        public List<string> Moods { get; set; } = new();
        public DateRange? ReleaseDateRange { get; set; }
        public string ProcessedQuery { get; set; } = string.Empty;
        public bool IsAmbiguous { get; set; }
        public float ConfidenceScore { get; set; }
    }

    public class DateRange
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}