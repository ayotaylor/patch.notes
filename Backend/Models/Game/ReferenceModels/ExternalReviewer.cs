namespace Backend.Models.Game.ReferenceModels
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Backend.Models.Game.Associations;

    public class ExternalReviewer : BaseEntity, IHasIgdbId
    {
        public int IgdbId { get; set; }
        [Required, MaxLength(150)]
        public string Source { get; set; } = string.Empty;
        public virtual ICollection<ExternalReviews> ExternalReviews { get; set; } = [];

    }
}