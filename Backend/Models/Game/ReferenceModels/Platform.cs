namespace Backend.Models.Game.ReferenceModels
{
    using System.ComponentModel.DataAnnotations;
    using Backend.Models.Game.Associations;

    public class Platform : BaseEntity
    {
        public int? IgdbId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Slug { get; set; } = string.Empty;

        public virtual ICollection<GamePlatform> GamePlatforms { get; set; } = new List<GamePlatform>();
        public virtual ICollection<ReleaseDate> ReleaseDates { get; set; } = new List<ReleaseDate>();
    }
}