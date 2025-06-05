namespace Backend.Models.Game.Associations
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Backend.Models.Game.ReferenceModels;

    public class GameGenre
    {
        [Required]
        public Guid GameId { get; set; }
        
        [Required]
        public Guid GenreId { get; set; }

        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;
        
        [ForeignKey("GenreId")]
        public virtual Genre Genre { get; set; } = null!;
    }

}