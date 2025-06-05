namespace Backend.Models.Game.ReferenceModels
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class GameFranchise
    {
        [Required]
        public Guid GameId { get; set; }
        
        [Required]
        public Guid FranchiseId { get; set; }

        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;
        
        [ForeignKey("FranchiseId")]
        public virtual Franchise Franchise { get; set; } = null!;
    }
}