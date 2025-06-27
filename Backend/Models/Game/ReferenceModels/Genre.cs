using System.ComponentModel.DataAnnotations;
using Backend.Models.Game.Associations;

namespace Backend.Models.Game.ReferenceModels
{
    public class Genre : BaseEntity, IHasIgdbId
{
    public int IgdbId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Slug { get; set; } = string.Empty;

    public virtual ICollection<GameGenre> GameGenres { get; set; } = new List<GameGenre>();
}
}