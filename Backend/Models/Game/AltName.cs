using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Game
{
    public class AltName : BaseEntity, IHasIgdbId
    {
        public int IgdbId { get; set; }
        [Required]
        public Guid GameId { get; set; }
        
        [Required, MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Comment { get; set; }

        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;
    }
}