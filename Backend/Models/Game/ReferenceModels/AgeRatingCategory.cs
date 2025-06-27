namespace Backend.Models.Game.ReferenceModels
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Backend.Models.Game.Associations;

    public class AgeRatingCategory : BaseEntity, IHasIgdbId
    {
        public int IgdbId { get; set; }
        public Guid RatingOrganizationId { get; set; }
        [Required, MaxLength(100)]
        public string Rating { get; set; } = string.Empty;
        public virtual RatingOrganization? RatingOrganization { get; set; }
        public virtual ICollection<AgeRating> AgeRatings { get; set; } = new List<AgeRating>();
    }
}