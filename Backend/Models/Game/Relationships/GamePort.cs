namespace Backend.Models.Game.Relationships
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class GamePort
    {
        [Required]
        public Guid OriginalGameId { get; set; }

        [Required]
        public Guid PortGameId { get; set; }

        [ForeignKey("OriginalGameId")]
        public virtual Game OriginalGame { get; set; } = null!;

        [ForeignKey("PortGameId")]
        public virtual Game PortGame { get; set; } = null!;
    }
}