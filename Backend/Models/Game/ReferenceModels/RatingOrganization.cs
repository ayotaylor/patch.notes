using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Game.ReferenceModels
{
    public class RatingOrganization : BaseEntity
    {
        public int? IgdbId { get; set; }
        
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public virtual ICollection<AgeRating> AgeRatings { get; set; } = new List<AgeRating>();
    }
}