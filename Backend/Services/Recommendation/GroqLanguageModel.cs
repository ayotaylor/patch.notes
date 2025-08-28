using Backend.Models.DTO.Recommendation;
using Backend.Services.Recommendation.Interfaces;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

                _logger.LogDebug("Groq API response: {Content}", responseContent);
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
                _logger.LogDebug("Follow-up questions response: {Response}", response);

                // Extract JSON from markdown code block if present
                var jsonContent = ExtractJsonFromResponse(response);

                var questions = JsonSerializer.Deserialize<string[]>(jsonContent);
                return questions?.ToList() ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize follow-up questions response: {Response}", response);
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

This structured information should include information that is assessed from the query.
This assessment should be based on the  
and should be categorized by the fields described in the JSON object described below:
Return ONLY a JSON object (no additional text, explanations, or markdown formatting) with:
- genres: array of detected game genres
- platforms: array of detected platforms
- gameModes: array of detected game modes
- moods: array of detected moods/feelings
- releaseDateRange: object with 'from' and 'to' dates if mentioned
- processedQuery: cleaned version of the query
- isAmbiguous: boolean if query needs clarification
- confidenceScore: float 0-1 for analysis confidence

Include alternate names or pseunodyms for any of the values of either of the array above.
Except processedQuery and confidenceScore

Example: {{""genres"":[""RPG""], ""platforms"":[""Playstation 5, PS5, Xbox Series X, XSX""],
            ""gameModes"":[""Single player, Multiplayer""], ""releaseDateRange"":[""happy""],
            ""processedQuery"":[""Here is a processed query""], ""isAmbiguous"":false,
            ""confidenceScore"":0.8}}";

            var response = await GenerateResponseAsync(prompt);

            try
            {
                _logger.LogDebug("QueryAnalysis response: {Response}", response);

                // Extract JSON from markdown code block if present
                var jsonContent = ExtractJsonFromResponse(response);

                var analysis = JsonSerializer.Deserialize<QueryAnalysis>(jsonContent);
                return analysis ?? new QueryAnalysis { ProcessedQuery = query, ConfidenceScore = 0.5f };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize QueryAnalysis response: {Response}", response);
                return new QueryAnalysis
                {
                    ProcessedQuery = query,
                    ConfidenceScore = 0.5f,
                    IsAmbiguous = true
                };
            }
        }

        private string ExtractJsonFromResponse(string response)
        {
            // Try to extract JSON from markdown code block
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(response, @"```json\s*([\s\S]*?)\s*```");
            if (jsonMatch.Success)
            {
                return jsonMatch.Groups[1].Value.Trim();
            }

            // Try to extract JSON from generic code block
            var codeMatch = System.Text.RegularExpressions.Regex.Match(response, @"```\s*([\s\S]*?)\s*```");
            if (codeMatch.Success)
            {
                var content = codeMatch.Groups[1].Value.Trim();
                // Check if it looks like JSON (starts with { or [)
                if (content.StartsWith("{") || content.StartsWith("["))
                {
                    return content;
                }
            }

            // Try to find JSON object directly in the response
            var directJsonMatch = System.Text.RegularExpressions.Regex.Match(response, @"(\{[\s\S]*\})");
            if (directJsonMatch.Success)
            {
                return directJsonMatch.Groups[1].Value.Trim();
            }

            // If no JSON found, return original response
            return response;
        }

        private class GroqResponse
        {
            [JsonPropertyName("choices")]
            public GroqChoice[]? Choices { get; set; }
        }

        private class GroqChoice
        {
            [JsonPropertyName("message")]
            public GroqMessage? Message { get; set; }
        }

        private class GroqMessage
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }
    }
}