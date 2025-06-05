using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Game
{
    public class Screenshot : BaseEntity
    {
        [Required]
        public Guid GameId { get; set; }
        
        [Required, MaxLength(500)]
        public string Url { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? IgdbImageId { get; set; }
        
        public int Height { get; set; }
        public int Width { get; set; }

        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;
    }
}