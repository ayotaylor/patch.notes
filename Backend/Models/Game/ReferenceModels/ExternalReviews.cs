namespace Backend.Models.Game.ReferenceModels
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Backend.Models.Game.Associations;

    public class ExternalReviews : BaseEntity
    {
        public Guid ExternalReviewerId { get; set; }
        public Guid GameId { get; set; }
        [Required, MaxLength(500)]
        public string Review { get; set; } = string.Empty;
        [ForeignKey("ExternalReviewerId")]
        public virtual ExternalReviewer? ExternalReviewer { get; set; }
        [ForeignKey("GameId")]
        public virtual Game? Game { get; set; }
    }
}