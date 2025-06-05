using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Game
{
    public class AltName : BaseEntity
    {
        [Required]
        public Guid GameId { get; set; }
        
        [Required, MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;
    }
}