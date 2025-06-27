using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Game
{
    public class Cover : BaseEntity, IHasIgdbId
    {
        public int IgdbId { get; set; }
        [Required]
        public Guid GameId { get; set; }

        [Required, MaxLength(500)]
        public string Url { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ImageId { get; set; }

        public int Height { get; set; }
        public int Width { get; set; }

        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;
    }

}