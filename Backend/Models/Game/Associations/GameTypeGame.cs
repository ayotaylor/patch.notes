namespace Backend.Models.Game
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Backend.Models.Game.ReferenceModels;

    public class GameTypeGame
    {
        [Required]
        public Guid GameId { get; set; }

        [Required]
        public Guid GameTypeId { get; set; }

        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;

        [ForeignKey("GameTypeId")]
        public virtual GameType GameType { get; set; } = null!;
    }
}