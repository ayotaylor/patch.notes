namespace Backend.Models.DTO.Game
{
    public class GameDto
    {
        public Guid Id { get; set; }
        public int? IgdbId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Storyline { get; set; }
        public string? Summary { get; set; }
        public long? FirstReleaseDate { get; set; }
        public int Hypes { get; set; }
        public decimal? IgdbRating { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Related data
        public List<GenreDto> Genres { get; set; } = new();
        public List<AgeRatingDto> AgeRatings { get; set; } = new();
        public List<AltNameDto> AltNames { get; set; } = new();
        public List<GameCoverDto> Covers { get; set; } = new();
        public List<GameScreenshotDto> Screenshots { get; set; } = new();
        public List<FranchiseDto> Franchises { get; set; } = new();
        public List<GameModeDto> GameModes { get; set; } = new();
        public List<GameTypeDto> GameTypes { get; set; } = new();
        public List<CompanyDto> Companies { get; set; } = new();
        public List<PlatformDto> Platforms { get; set; } = new();
        public List<PlayerPerspectiveDto> PlayerPerspectives { get; set; } = new();
        public List<ReleaseDateDto> ReleaseDates { get; set; } = new();

        // Game relationships
        public List<GameDto> Dlcs { get; set; } = new();
        public List<GameDto> Expansions { get; set; } = new();
        public List<GameDto> Ports { get; set; } = new();
        public List<GameDto> Remakes { get; set; } = new();
        public List<GameDto> Remasters { get; set; } = new();
        public List<GameDto> SimilarGames { get; set; } = new();

        // Social data
        public int LikesCount { get; set; }
        public int FavoritesCount { get; set; }
        public bool IsLikedByUser { get; set; }
        public bool IsFavoriteByUser { get; set; }
    }
}
