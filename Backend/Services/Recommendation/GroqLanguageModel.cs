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
            var gameDetails = recommendations.Take(3).Select(g => 
                $"{g.Name} ({string.Join(", ", g.Genres.Take(2))}) - {g.ConfidenceScore:F2}").ToList();
            
            var prompt = $@"You are generating intelligent follow-up questions for a game recommendation system to refine future searches and improve recommendation accuracy.

CONTEXT:
- Original Query: '{query}'
- Top Recommendations: {string.Join(" | ", gameDetails)}

GENERATE 2-3 STRATEGIC FOLLOW-UP QUESTIONS that will:

🎯 REFINEMENT GOALS:
1. Clarify ambiguous aspects of the original query
2. Gather missing semantic dimensions (genre, platform, mood, game mode preferences)
3. Understand user's depth vs breadth preferences
4. Identify what made the recommendations appealing or not

🧠 QUESTION TYPES TO PRIORITIZE:

SEMANTIC CLARIFICATION:
- If query was genre-vague: Ask about specific genre preferences or mechanics
- If platform-unclear: Ask about preferred gaming platforms or contexts
- If mood-ambiguous: Ask about desired emotional experience or session length

PREFERENCE REFINEMENT:
- Difficulty preferences: challenging vs casual, complex vs accessible
- Social aspects: solo vs multiplayer, competitive vs cooperative
- Narrative importance: story-driven vs gameplay-focused
- Time investment: quick sessions vs long campaigns

CONTEXTUAL UNDERSTANDING:
- Gaming experience level: newcomer vs veteran preferences
- Similar games enjoyed: ""games like X you've loved""
- Deal-breakers: elements to avoid or must-have features

🎨 QUESTION CRAFTING RULES:
- Make questions specific and actionable (avoid ""Do you like X?"" - use ""Would you prefer X or Y?"")
- Reference the recommended games when helpful for context
- Focus on aspects that will significantly impact future recommendations
- Use natural, conversational language
- Avoid overwhelming technical jargon

EXAMPLE QUALITY QUESTIONS:
- ""Would you prefer games like *insert relevant game here* that focus on exploration, or more action-oriented experiences?""
- ""Are you looking for games you can play in short 30-minute sessions, or longer immersive experiences?""
- ""Do you enjoy games with complex storylines and character development, or prefer gameplay-focused experiences?""

RETURN ONLY a JSON array of 2-3 strategic questions:
[""question 1"", ""question 2"", ""question 3""]";

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
            var prompt = $@"You are analyzing game recommendation queries for a semantic vector database search system. The games are indexed with rich semantic metadata including genres, mechanics, themes, moods, platforms, game modes, and player perspectives.

ANALYZE THIS QUERY: '{query}'

Your analysis will drive semantic keyword enhancement that boosts vector search relevance. Focus on extracting ALL possible semantic dimensions that could match game embeddings.

DETECTION GUIDELINES:

🎮 GENRES: Look for explicit/implicit game genres:
- Direct mentions: ""RPG"", ""strategy"", ""platformer"", ""shooter""
- Descriptive clues: ""character progression"" → RPG, ""resource management"" → Strategy, ""jumping"" → Platform
- Mood-based: ""relaxing"" → Puzzle/Simulation, ""intense"" → Action/Shooter

🖥️ PLATFORMS: Detect platforms via exact names, abbreviations, or contextual clues:
- Console generations: ""next-gen"" → PS5/Xbox Series X, ""retro"" → older consoles
- Portability hints: ""on the go"" → Switch/handheld, ""big screen"" → console
- Include ALL aliases: ""PS5, PlayStation 5, Sony PS5""

👥 GAME MODES: Extract multiplayer preferences and social aspects:
- Social indicators: ""with friends"" → Multiplayer, ""solo experience"" → Single-player
- Cooperation clues: ""team up"" → Co-op, ""compete"" → Competitive multiplayer
- Local vs online: ""couch gaming"" → Split-screen, ""online"" → Online multiplayer

😊 MOODS: Identify emotional goals and desired feelings:
- Energy levels: ""relaxing"", ""chill"", ""intense"", ""adrenaline-pumping""  
- Emotional states: ""nostalgic"", ""immersive"", ""challenging"", ""uplifting""
- Time context: ""after work"" → relaxing, ""weekend"" → engaging

📅 RELEASE DATES: Parse temporal references:
- Specific years/ranges: ""2023"", ""last 5 years"", ""recent releases""
- Era descriptions: ""retro"", ""classic"", ""modern"", ""cutting-edge""
- Convert to actual date ranges when possible

🎯 PROCESSED QUERY: Create search-optimized text by:
- Expanding abbreviations: ""RPGs"" → ""role-playing games RPG""
- Adding semantic keywords: ""horror"" → ""horror scary atmospheric dark survival""
- Including synonyms: ""multiplayer"" → ""multiplayer co-op online social""
- Preserving key descriptive phrases

⚠️ AMBIGUITY DETECTION: Mark as ambiguous if:
- Multiple conflicting preferences: ""casual but challenging""
- Vague descriptors without context: ""good games""  
- Missing critical info: genre/platform preferences unclear
- Contradictory requirements: ""single-player multiplayer""

🎯 CONFIDENCE SCORING: Base on clarity and specificity:
- 0.9+: Very specific (""Dark fantasy RPG like Dark Souls for PS5"")
- 0.7-0.8: Clear intent with some details (""Fun multiplayer games for Switch"")
- 0.5-0.6: General but understandable (""Something relaxing"")
- <0.5: Very vague or contradictory

RETURN ONLY JSON (no markdown, explanations, or additional text):
{{
    ""genres"": [""detected genres with alternatives""],
    ""platforms"": [""platform names and all aliases""],
    ""gameModes"": [""game modes and social aspects""],
    ""moods"": [""emotional keywords and descriptors""],
    ""releaseDateRange"": {{""from"": ""YYYY-MM-DD"", ""to"": ""YYYY-MM-DD""}},
    ""processedQuery"": ""semantically enhanced search text with synonyms and keywords"",
    ""isAmbiguous"": false,
    ""confidenceScore"": 0.85
}}";

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

        public async Task<string> ExplainGameRecommendationAsync(GameRecommendation game, string originalQuery)
        {
            var prompt = $@"Why is '{game.Name}' a good match for: '{originalQuery}'?
Game: {game.Summary.Substring(0, Math.Min(150, game.Summary.Length))} | Genres: {string.Join(", ", game.Genres)} | Rating: {game.Rating ?? 0}/100
Keep explanation concise (2-3 sentences) and personal.";

            return await GenerateResponseAsync(prompt);
        }
    }
}