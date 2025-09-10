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
        Task<List<string>> ExplainGameRecommendationsBatchAsync(List<GameRecommendation> games, string originalQuery);
    }

    public class QueryAnalysis
    {
        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = new();
        
        [JsonPropertyName("platforms")]
        public List<string> Platforms { get; set; } = new();
        
        [JsonPropertyName("gameModes")]
        public List<string> GameModes { get; set; } = new();
        
        [JsonPropertyName("playerPerspectives")]
        public List<string> PlayerPerspectives { get; set; } = new();
        
        [JsonPropertyName("moods")]
        public List<string> Moods { get; set; } = new();
        
        [JsonPropertyName("releaseDateRange")]
        public DateRange? ReleaseDateRange { get; set; }

        [JsonPropertyName("companies")]
        public List<string>? Companies { get; set; }

        [JsonPropertyName("gameType")]
        public string? GameType { get; set; }
        
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
        public string? FromStr { get; set; }
        
        [JsonPropertyName("to")]
        public string? ToStr { get; set; }
        
        // Helper properties to parse dates safely
        public DateTime? From => ParseDate(FromStr);
        public DateTime? To => ParseDate(ToStr);
        
        private static DateTime? ParseDate(string? dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;
                
            if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, out var result))
            {
                return result;
            }
            
            // Fallback to general parsing
            if (DateTime.TryParse(dateString, out var fallbackResult))
            {
                return fallbackResult;
            }
            
            return null;
        }
    }
}