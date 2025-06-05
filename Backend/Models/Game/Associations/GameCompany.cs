using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Models.Game.ReferenceModels;

namespace Backend.Models.Game.Associations
{
    public class GameCompany
    {
        [Required]
        public Guid GameId { get; set; }
        
        [Required]
        public Guid CompanyId { get; set; }
        
        [Required]
        public CompanyRole Role { get; set; }

        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;
        
        [ForeignKey("CompanyId")]
        public virtual Company Company { get; set; } = null!;
    }
}