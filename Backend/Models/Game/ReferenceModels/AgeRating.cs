namespace Backend.Models.Game.ReferenceModels
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Backend.Models.Game.Associations;

    public class AgeRating : BaseEntity, IHasIgdbId
    {
        public int IgdbId { get; set; }
        public Guid AgeRatingCategoryId { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [Required, MaxLength(150)]
        public string Slug { get; set; } = string.Empty;
        [ForeignKey("AgeRatingCategoryId")]
        public virtual AgeRatingCategory? AgeRatingCategory { get; set; }
        public virtual ICollection<GameAgeRating> GameAgeRatings { get; set; } = new List<GameAgeRating>();

    }
}