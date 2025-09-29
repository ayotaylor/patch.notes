namespace Backend.Models.DTO.Game
{
    public class GameRelationshipDto
    {
        public int IgdbId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? CoverUrl { get; set; }
        public decimal? IgdbRating { get; set; }
        public decimal? IgdbTotalRating { get; set; }
        public int? IgdbTotalRatingCount { get; set; }
        public long? FirstReleaseDate { get; set; }
    }
}