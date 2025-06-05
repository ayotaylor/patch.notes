using System.ComponentModel.DataAnnotations;
using Backend.Models;
using Backend.Models.Game.Associations;

namespace Game.Models.ReferenceModels
{
    public class GameMode : BaseEntity
    {
        public int? IgdbId { get; set; }
        
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required, MaxLength(150)]
        public string Slug { get; set; } = string.Empty;

        public virtual ICollection<GameModeGame> GameModeGames { get; set; } = new List<GameModeGame>();
    }
}