using System.ComponentModel.DataAnnotations;
using Backend.Models.Game.Associations;

namespace Backend.Models.Game.ReferenceModels
{
    public class PlayerPerspective : BaseEntity, IHasIgdbId
    {
        public int IgdbId { get; set; }
        
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required, MaxLength(150)]
        public string Slug { get; set; } = string.Empty;

        public virtual ICollection<GamePlayerPerspective> GamePlayerPerspectives { get; set; } = new List<GamePlayerPerspective>();
    }
}