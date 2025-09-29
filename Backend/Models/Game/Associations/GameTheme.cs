namespace Backend.Models.Game.Associations
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Backend.Models.Game.ReferenceModels;

    public class GameTheme
    {
        [Required]
        public Guid GameId { get; set; }
        
        [Required]
        public Guid ThemeId { get; set; }

        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;
        
        [ForeignKey("ThemeId")]
        public virtual Theme Theme { get; set; } = null!;
    }

}