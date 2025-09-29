using System.ComponentModel.DataAnnotations;
using Backend.Models.Game.Associations;

namespace Backend.Models.Game.ReferenceModels
{
    public class Theme : BaseEntity, IHasIgdbId
{
    public int IgdbId { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    public virtual ICollection<GameTheme> GameThemes { get; set; } = [];
}
}