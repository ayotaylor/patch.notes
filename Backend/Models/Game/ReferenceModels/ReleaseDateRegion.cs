using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Game.ReferenceModels
{
    public class ReleaseDateRegion : BaseEntity, IHasIgdbId
    {
        public int IgdbId { get; set; }

        public string Region { get; set; } = string.Empty;

        public virtual ICollection<ReleaseDate> ReleaseDates { get; set; } = new List<ReleaseDate>();
    }
}