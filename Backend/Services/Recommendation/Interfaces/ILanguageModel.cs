using Backend.Models.DTO.Recommendation;
using System.Text.Json.Serialization;

namespace Backend.Services.Recommendation.Interfaces
{
    public interface ILanguageModel
    {
        Task<string> GenerateResponseAsync(string prompt, string? conversationContext = null);
        Task<List<string>> GenerateFollowUpQuestionsAsync(string query, List<GameRecommendation> recommendations);
        Task<string> ExplainRecommendationsAsync(List<GameRecommendation> recommendations, string originalQuery);
        Task<QueryAnalysis> AnalyzeQueryAsync(string query);
        Task<string> ExplainGameRecommendationAsync(GameRecommendation game, string originalQuery);
    }

    public class QueryAnalysis
    {
        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = new();
        
        [JsonPropertyName("platforms")]
        public List<string> Platforms { get; set; } = new();
        
        [JsonPropertyName("gameModes")]
        public List<string> GameModes { get; set; } = new();
        
        [JsonPropertyName("moods")]
        public List<string> Moods { get; set; } = new();
        
        [JsonPropertyName("releaseDateRange")]
        public DateRange? ReleaseDateRange { get; set; }
        
        [JsonPropertyName("processedQuery")]
        public string ProcessedQuery { get; set; } = string.Empty;
        
        [JsonPropertyName("isAmbiguous")]
        public bool IsAmbiguous { get; set; }
        
        [JsonPropertyName("confidenceScore")]
        public float ConfidenceScore { get; set; }
    }

    public class DateRange
    {
        [JsonPropertyName("from")]
        public DateTime? From { get; set; }
        
        [JsonPropertyName("to")]
        public DateTime? To { get; set; }
    }
}