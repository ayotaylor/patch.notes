using Backend.Models.DTO.Game;

namespace Backend.Models.DTO.Social
{
    public class GameSummaryDto
    {
        public int IgdbId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? CoverUrl { get; set; }
    }
}