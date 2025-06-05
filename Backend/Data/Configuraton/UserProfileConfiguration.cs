using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configuration
{
    public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            // Optional: Add some constraints
            builder.Property(e => e.FirstName).HasMaxLength(50);
            builder.Property(e => e.LastName).HasMaxLength(50);
            builder.Property(e => e.DisplayName).HasMaxLength(100);
            builder.Property(e => e.Bio).HasMaxLength(500);

            // add index for better performance
            builder.HasIndex(e => e.UserId).IsUnique();
        }
    }
}