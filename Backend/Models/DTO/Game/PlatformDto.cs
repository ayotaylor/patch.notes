namespace Backend.Models.DTO.Game
{
    public class PlatformDto
    {
        public int? IgdbId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; } = string.Empty;
        public string? Abbreviation { get; set; } = string.Empty;
        public string? AlternativeName { get; set; } = string.Empty;
    }
}