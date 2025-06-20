namespace Backend.Models.Game.ReferenceModels
{
    using System.ComponentModel.DataAnnotations;
    using Backend.Models.Game.Associations;

    public class Company : BaseEntity
    {
        public int? IgdbId { get; set; }

        [Required, MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Country { get; set; }

        public string? Description { get; set; }

        [MaxLength(255)]
        public string? Slug { get; set; }

        [MaxLength(500)]
        public string? Url { get; set; }

        public virtual ICollection<GameCompany> GameCompanies { get; set; } = new List<GameCompany>();
    }
}
