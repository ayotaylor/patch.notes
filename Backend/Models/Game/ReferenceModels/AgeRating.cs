namespace Backend.Models.Game.ReferenceModels
{
    using System.ComponentModel.DataAnnotations;
    using Backend.Models.Game.Associations;

    public class AgeRating : BaseEntity
    {
        public int? IgdbId { get; set; }
        public Guid RatingOrganizationId { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [Required, MaxLength(150)]
        public string Slug { get; set; } = string.Empty;
        public virtual RatingOrganization? RatingOrganization { get; set; }
        public virtual ICollection<GameAgeRating> GameAgeRatings { get; set; } = new List<GameAgeRating>();

    }
}