using Backend.Models.DTO.Recommendation;
using Backend.Services.Recommendation.Interfaces;
using System.Text;
using System.Text.Json;

namespace Backend.Services.Recommendation
{
    public class GroqLanguageModel : ILanguageModel
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GroqLanguageModel> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://api.groq.com/openai/v1";

        public GroqLanguageModel(HttpClient httpClient, IConfiguration configuration, ILogger<GroqLanguageModel> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["Groq:ApiKey"] ?? throw new ArgumentException("Groq API key not configured");

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
        }

        public async Task<string> GenerateResponseAsync(string prompt, string? conversationContext = null)
        {
            try
            {
                var fullPrompt = conversationContext != null
                    ? $"Previous context: {conversationContext}\n\nUser query: {prompt}"
                    : prompt;

                var request = new
                {
                    model = "llama-3.1-8b-instant",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a game recommendation assistant. Provide helpful, concise responses about games." },
                        new { role = "user", content = fullPrompt }
                    },
                    max_tokens = 500,
                    temperature = 0.7
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Groq API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return "I'm having trouble processing your request right now. Please try again.";
                }

                var result = JsonSerializer.Deserialize<GroqResponse>(responseContent);
                return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response generated";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Groq API");
                return "I'm experiencing technical difficulties. Please try again later.";
            }
        }

        public async Task<List<string>> GenerateFollowUpQuestionsAsync(string query, List<GameRecommendation> recommendations)
        {
            var prompt = $@"Based on the user query: '{query}' and the recommended games: {string.Join(", ", recommendations.Take(3).Select(g => g.Name))},
generate 2-3 follow-up questions to better understand the user's preferences.
Format as a JSON array of strings. Keep questions concise and relevant.
Examples: [""Do you prefer single-player or multiplayer games?"", ""Are you interested in newer releases or classic games?""]";

            var response = await GenerateResponseAsync(prompt);

            try
            {
                var questions = JsonSerializer.Deserialize<string[]>(response);
                return questions?.ToList() ?? new List<string>();
            }
            catch
            {
                return new List<string> { "Would you like games similar to any specific genre?", "Do you prefer newer or older games?" };
            }
        }

        public async Task<string> ExplainRecommendationsAsync(List<GameRecommendation> recommendations, string originalQuery)
        {
            var gamesInfo = string.Join("\n", recommendations.Take(5).Select(g =>
                $"- {g.Name}: {g.Summary.Substring(0, Math.Min(100, g.Summary.Length))}... (Confidence: {g.ConfidenceScore:F2})"));

            var prompt = $@"Explain why these games were recommended for the query: '{originalQuery}'.
Games recommended:
{gamesInfo}

Provide a brief, friendly explanation focusing on how they match the user's request.";

            return await GenerateResponseAsync(prompt);
        }

        public async Task<QueryAnalysis> AnalyzeQueryAsync(string query)
        {
            var prompt = $@"Analyze this game recommendation query and extract structured information: '{query}'
Return a JSON object with:
- genres: array of detected game genres
- platforms: array of detected platforms
- gameModes: array of detected game modes
- moods: array of detected moods/feelings
- releaseDateRange: object with 'from' and 'to' dates if mentioned
- processedQuery: cleaned version of the query
- isAmbiguous: boolean if query needs clarification
- confidenceScore: float 0-1 for analysis confidence

Example: {{""genres"":[""RPG""], ""moods"":[""happy""], ""isAmbiguous"":false, ""confidenceScore"":0.8}}";

            var response = await GenerateResponseAsync(prompt);

            try
            {
                var analysis = JsonSerializer.Deserialize<QueryAnalysis>(response);
                return analysis ?? new QueryAnalysis { ProcessedQuery = query, ConfidenceScore = 0.5f };
            }
            catch
            {
                return new QueryAnalysis
                {
                    ProcessedQuery = query,
                    ConfidenceScore = 0.5f,
                    IsAmbiguous = true
                };
            }
        }

        private class GroqResponse
        {
            public GroqChoice[]? Choices { get; set; }
        }

        private class GroqChoice
        {
            public GroqMessage? Message { get; set; }
        }

        private class GroqMessage
        {
            public string? Content { get; set; }
        }
    }
}