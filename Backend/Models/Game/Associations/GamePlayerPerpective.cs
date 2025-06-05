using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Models.Game.ReferenceModels;

namespace Backend.Models.Game.Associations
{
    public class GamePlayerPerspective
    {
        [Required]
        public Guid GameId { get; set; }
        
        [Required]
        public Guid PlayerPerspectiveId { get; set; }

        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;
        
        [ForeignKey("PlayerPerspectiveId")]
        public virtual PlayerPerspective PlayerPerspective { get; set; } = null!;
    }
}