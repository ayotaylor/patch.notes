using Backend.Models.Game.Associations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configuration
{
    public class GameConfiguration : IEntityTypeConfiguration<Backend.Models.Game.Game>
    {
        public void Configure(EntityTypeBuilder<Backend.Models.Game.Game> builder)
        {
            builder.HasIndex(g => g.Slug).IsUnique();
            builder.HasIndex(g => g.IgdbId).IsUnique().HasFilter("[IgdbId] IS NOT NULL");

            // Configure indexes for performance
            builder.HasIndex(g => g.Name);
            builder.HasIndex(g => new { g.Hypes, g.Rating });
            builder.HasIndex(g => g.Hypes);
            builder.HasIndex(g => g.Rating);
            // Configure decimal precision
            builder.Property(g => g.Rating)
                .HasPrecision(4, 1);
            builder.HasOne(g => g.GameType)
                .WithMany(gt => gt.Games)
                .HasForeignKey(g => g.GameTypeId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
            //builder.HasIndex(g => g.Hypes);
        }
    }
}