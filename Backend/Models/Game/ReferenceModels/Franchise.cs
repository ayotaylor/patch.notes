namespace Backend.Models.Game.ReferenceModels
{
    using System.ComponentModel.DataAnnotations;

    public class Franchise : BaseEntity
    {
        public int? IgdbId { get; set; }
        
        [Required, MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [Required, MaxLength(255)]
        public string Slug { get; set; } = string.Empty;

        public virtual ICollection<GameFranchise> GameFranchises { get; set; } = new List<GameFranchise>();
    }
}