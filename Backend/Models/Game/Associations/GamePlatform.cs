using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Models.Game.ReferenceModels;

namespace Backend.Models.Game.Associations
{
    public class GamePlatform
    {
        [Required]
        public Guid GameId { get; set; }
        
        [Required]
        public Guid PlatformId { get; set; }

        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;
        
        [ForeignKey("PlatformId")]
        public virtual Platform Platform { get; set; } = null!;
    }
}