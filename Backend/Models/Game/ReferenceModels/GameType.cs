using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Game.ReferenceModels
{
    public class GameType : BaseEntity, IHasIgdbId
    {
        public int IgdbId { get; set; }
        
        [Required, MaxLength(100)]
        public string Type { get; set; } = string.Empty; // dlc, bundle, episode, etc.

        public virtual ICollection<Game> Games { get; set; } = [];
    }
}