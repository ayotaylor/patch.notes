using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Game.ReferenceModels
{
    public class RatingOrganization : BaseEntity
    {
        public int? IgdbId { get; set; }
        
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public virtual ICollection<AgeRatingCategory> AgeRatingCategory { get; set; } = new List<AgeRatingCategory>();
    }
}