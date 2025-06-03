using Backend.Models.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configuration
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(e => e.FirstName).HasMaxLength(50);
            builder.Property(e => e.LastName).HasMaxLength(50);
            builder.Property(e => e.Provider).HasMaxLength(20);
            builder.Property(e => e.ProviderId).HasMaxLength(100);
            builder.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            // add index for better performance
            builder.HasIndex(e => e.Email).IsUnique();
            builder.HasIndex(e => new { e.Provider, e.ProviderId });

            // configure one-to-one relationship with UserProfile
            builder.HasOne(u => u.UserProfile)
                .WithOne(p => p.User)
                .HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}