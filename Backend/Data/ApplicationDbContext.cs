using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Backend.Models;
using Backend.Models.Auth;

namespace Backend.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // configure user entity
            builder.Entity<User>(entity =>
            {
                entity.Property(e => e.FirstName).HasMaxLength(50);
                entity.Property(e => e.LastName).HasMaxLength(50);
                entity.Property(e => e.Provider).HasMaxLength(20);
                entity.Property(e => e.ProviderId).HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

                // add index for better performance
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => new { e.Provider, e.ProviderId });
            });
        }
    }
}