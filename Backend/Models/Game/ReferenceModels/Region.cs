using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Game.ReferenceModels
{
        public class Region : BaseEntity
    {
        public int? IgdbId { get; set; }
        
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public virtual ICollection<ReleaseDate> ReleaseDates { get; set; } = new List<ReleaseDate>();
    }
}