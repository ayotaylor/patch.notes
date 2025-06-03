using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Backend.Models;
using Backend.Models.Auth;
using Backend.Data.Configuration;

namespace Backend.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // configure user entity
            builder.ApplyConfiguration(new UserConfiguration());
            builder.ApplyConfiguration(new UserProfileConfiguration());
        }
    }
}