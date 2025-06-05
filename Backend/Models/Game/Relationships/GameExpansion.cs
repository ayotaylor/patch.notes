namespace Backend.Models.Game.Relationships
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class GameExpansion
    {
        [Required]
        public Guid ParentGameId { get; set; }

        [Required]
        public Guid ExpansionGameId { get; set; }

        [ForeignKey("ParentGameId")]
        public virtual Game ParentGame { get; set; } = null!;

        [ForeignKey("ExpansionGameId")]
        public virtual Game ExpansionGame { get; set; } = null!;
    }
}