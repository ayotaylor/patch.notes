using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Game.Models.ReferenceModels;

namespace Backend.Models.Game.Associations
{
    public class GameModeGame
    {
        [Required]
        public Guid GameId { get; set; }
        
        [Required]
        public Guid GameModeId { get; set; }

        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;
        
        [ForeignKey("GameModeId")]
        public virtual GameMode GameMode { get; set; } = null!;
    }
}