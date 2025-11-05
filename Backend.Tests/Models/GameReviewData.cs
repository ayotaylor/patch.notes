using System.Text.Json.Serialization;

namespace Backend.Tests.Models;

/// <summary>
/// Model for deserializing game review data from game-reviews.json
/// </summary>
public class GameReviewData
{
    [JsonPropertyName("IgdbId")]
    public int IgdbId { get; set; }

    [JsonPropertyName("gameName")]
    public string GameName { get; set; } = string.Empty;

    [JsonPropertyName("review")]
    public string Review { get; set; } = string.Empty;

    [JsonPropertyName("reviewComments")]
    public List<string> ReviewComments { get; set; } = new();

    [JsonPropertyName("reviewScore")]
    public double ReviewScore { get; set; }
}
