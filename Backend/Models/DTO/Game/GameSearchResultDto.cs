namespace Backend.Models.DTO.Game
{
    public class GameSearchResultDto
    {
        public int IgdbId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? Summary { get; set; }
        public long? FirstReleaseDate { get; set; }
        public decimal? IgdbRating { get; set; }
        public decimal? IgdbTotalRating { get; set; }
        public int? IgdbTotalRatingCount { get; set; }
        public int Hypes { get; set; }

        // Essential related data for search results
        public List<GenreDto> Genres { get; set; } = new();
        public List<ThemeDto> Themes { get; set; } = new();
        public List<GameCoverDto> Covers { get; set; } = new();
        public List<PlatformDto> Platforms { get; set; } = new();
        public List<CompanyDto> Companies { get; set; } = new();

        // Social data
        public int LikesCount { get; set; }
        public int FavoritesCount { get; set; }
        public bool IsLikedByUser { get; set; }
        public bool IsFavoriteByUser { get; set; }
    }
}