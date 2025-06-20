using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Models.Game.ReferenceModels;

namespace Backend.Models.Game
{
    public class ReleaseDate : BaseEntity
    {
        public int? IgdbId { get; set; }

        [Required]
        public Guid GameId { get; set; }

        [Required]
        public Guid PlatformId { get; set; }

        [Required]
        public Guid RegionId { get; set; }

        public long Date { get; set; } // Unix timestamp

        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;

        [ForeignKey("PlatformId")]
        public virtual Platform Platform { get; set; } = null!;

        [ForeignKey("RegionId")]
        public virtual Region Region { get; set; } = null!;
    }
}