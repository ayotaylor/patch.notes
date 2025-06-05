using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Game.Relationships
{
    public class GameDlc
    {
        [Required]
        public Guid ParentGameId { get; set; }
        
        [Required]
        public Guid DlcGameId { get; set; }

        [ForeignKey("ParentGameId")]
        public virtual Game ParentGame { get; set; } = null!;
        
        [ForeignKey("DlcGameId")]
        public virtual Game DlcGame { get; set; } = null!;
    }
}