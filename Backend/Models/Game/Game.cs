using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Models.Game.Associations;
using Backend.Models.Game.ReferenceModels;
using Backend.Models.Game.Relationships;
using Backend.Models.Game.Social;

namespace Backend.Models.Game
{
    public class Game : BaseEntity
    {
        public int? IgdbId { get; set; }

        [Required, MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string Slug { get; set; } = string.Empty;

        public string? Storyline { get; set; }
        public string? Summary { get; set; }

        public long? FirstReleaseDate { get; set; } // Unix timestamp
        public int Hypes { get; set; } = 0;

        [Column(TypeName = "decimal(4,1)")]
        public decimal? IgdbRating { get; set; }

        // Navigation Properties
        public virtual ICollection<GameGenre> GameGenres { get; set; } = new List<GameGenre>();
        public virtual ICollection<GameAgeRating> AgeRatings { get; set; } = new List<GameAgeRating>();
        public virtual ICollection<AltName> AltNames { get; set; } = new List<AltName>();
        public virtual ICollection<Cover> Covers { get; set; } = new List<Cover>();
        public virtual ICollection<Screenshot> Screenshots { get; set; } = new List<Screenshot>();
        public virtual ICollection<ReleaseDate> ReleaseDates { get; set; } = new List<ReleaseDate>();
        public virtual ICollection<GameFranchise> GameFranchises { get; set; } = new List<GameFranchise>();
        public virtual ICollection<GameModeGame> GameModes { get; set; } = new List<GameModeGame>();
        public virtual ICollection<GameTypeGame> GameTypes { get; set; } = new List<GameTypeGame>();
        public virtual ICollection<GameCompany> GameCompanies { get; set; } = new List<GameCompany>();
        public virtual ICollection<GamePlatform> GamePlatforms { get; set; } = new List<GamePlatform>();
        public virtual ICollection<GamePlayerPerspective> GamePlayerPerspectives { get; set; } = new List<GamePlayerPerspective>();

        // Game Relationships
        public virtual ICollection<GameDlc> ParentGameDlcs { get; set; } = new List<GameDlc>();
        public virtual ICollection<GameDlc> DlcGames { get; set; } = new List<GameDlc>();
        public virtual ICollection<GameExpansion> ParentGameExpansions { get; set; } = new List<GameExpansion>();
        public virtual ICollection<GameExpansion> ExpansionGames { get; set; } = new List<GameExpansion>();
        public virtual ICollection<GamePort> OriginalGamePorts { get; set; } = new List<GamePort>();
        public virtual ICollection<GamePort> PortGames { get; set; } = new List<GamePort>();
        public virtual ICollection<GameRemake> OriginalGameRemakes { get; set; } = new List<GameRemake>();
        public virtual ICollection<GameRemake> RemakeGames { get; set; } = new List<GameRemake>();
        public virtual ICollection<GameRemaster> OriginalGameRemasters { get; set; } = new List<GameRemaster>();
        public virtual ICollection<GameRemaster> RemasterGames { get; set; } = new List<GameRemaster>();
        public virtual ICollection<SimilarGame> SimilarGames { get; set; } = new List<SimilarGame>();
        public virtual ICollection<SimilarGame> SimilarToGames { get; set; } = new List<SimilarGame>();

        // Social Features
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
    }

}