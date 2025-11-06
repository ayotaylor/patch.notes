using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Backend.Models.Auth;
using Backend.Data.Configuration;
using Backend.Models.Game.ReferenceModels;
using Game.Models.ReferenceModels;
using Backend.Models.Game;
using Backend.Models.Game.Associations;
using Backend.Models.Game.Relationships;
using Backend.Models;
using Backend.Models.Social;

namespace Backend.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; } = null!;
        // game related entities
        public DbSet<Backend.Models.Game.Game> Games { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<RatingOrganization> RatingOrganizations { get; set; }
        public DbSet<AgeRating> AgeRatings { get; set; }
        public DbSet<AgeRatingCategory> AgeRatingCategories { get; set; }
        public DbSet<Franchise> Franchises { get; set; }
        public DbSet<GameMode> GameModes { get; set; }
        public DbSet<GameType> GameTypes { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Platform> Platforms { get; set; }
        public DbSet<PlayerPerspective> PlayerPerspectives { get; set; }
        public DbSet<ReleaseDateRegion> Regions { get; set; }
        public DbSet<Theme> Themes { get; set; }
        public DbSet<ExternalReviewer> ExternalReviewers { get; set; }
        public DbSet<ExternalReviews> ExternalReviews { get; set; }
        // Game-specific data
        public DbSet<AltName> AltNames { get; set; }
        public DbSet<Cover> Covers { get; set; }
        public DbSet<Screenshot> Screenshots { get; set; }
        public DbSet<ReleaseDate> ReleaseDates { get; set; }
        // Junction tables
        public DbSet<GameAgeRating> GameAgeRatings { get; set; }
        public DbSet<GameGenre> GameGenres { get; set; }
        public DbSet<GameFranchise> GameFranchises { get; set; }
        public DbSet<GameModeGame> GameModeGames { get; set; }
        // public DbSet<GameTypeGame> GameTypeGames { get; set; }
        public DbSet<GameCompany> GameCompanies { get; set; }
        public DbSet<GamePlatform> GamePlatforms { get; set; }
        public DbSet<GamePlayerPerspective> GamePlayerPerspectives { get; set; }
        public DbSet<GameTheme> GameThemes { get; set; }
        // Game relationships
        public DbSet<GameDlc> GameDlcs { get; set; }
        public DbSet<GameExpansion> GameExpansions { get; set; }
        public DbSet<GamePort> GamePorts { get; set; }
        public DbSet<GameRemake> GameRemakes { get; set; }
        public DbSet<GameRemaster> GameRemasters { get; set; }
        public DbSet<SimilarGame> SimilarGames { get; set; }
        // Social features
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<GameList> GameLists { get; set; }
        public DbSet<GameListItem> GameListItems { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<ReviewLike> ReviewLikes { get; set; }
        public DbSet<GameListLike> GameListLikes { get; set; }
        public DbSet<CommentLike> CommentLikes { get; set; }
        public DbSet<Follow> Follows { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Ignore<BaseEntity>();

            // Global UTC DateTime handling - treats all DateTime values as UTC
            // This ensures System.Text.Json serializes with 'Z' suffix
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(
                            new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                                v => v.ToUniversalTime(),
                                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(
                            new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
                                v => v.HasValue ? v.Value.ToUniversalTime() : v,
                                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v));
                    }
                }
            }

            // Configure auto-updating timestamps for entities that inherit from BaseEntity
            // Apply this configuration to each specific entity that inherits from BaseEntity
            // Using DATETIME instead of TIMESTAMP to avoid MySQL timezone conversions
            // All dates are stored as UTC and should be serialized with 'Z' suffix
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    builder.Entity(entityType.ClrType)
                        .Property("UpdatedAt")
                        .HasColumnType("datetime(6)")
                        .ValueGeneratedOnAddOrUpdate();

                    builder.Entity(entityType.ClrType)
                        .Property("CreatedAt")
                        .HasColumnType("datetime(6)")
                        .ValueGeneratedOnAdd();
                }
            }

            // configure user, profile and game entities
            builder.ApplyConfiguration(new UserConfiguration());
            builder.ApplyConfiguration(new UserProfileConfiguration());
            builder.ApplyConfiguration(new GameConfiguration());

            // TODO: move these to differnent files
            // Configure game related entities
            // Configure composite keys for junction tables
            builder.Entity<GameGenre>()
                .HasKey(gg => new { gg.GameId, gg.GenreId });

            builder.Entity<GameFranchise>()
                .HasKey(gf => new { gf.GameId, gf.FranchiseId });

            builder.Entity<GameModeGame>()
                .HasKey(gmg => new { gmg.GameId, gmg.GameModeId });

            // builder.Entity<GameTypeGame>()
            //     .HasKey(gtg => new { gtg.GameId, gtg.GameTypeId });

            builder.Entity<GameTheme>()
                .HasKey(gg => new { gg.GameId, gg.ThemeId });

            builder.Entity<GameCompany>()
                .HasKey(gc => new { gc.GameId, gc.CompanyId });

            builder.Entity<GamePlatform>()
                .HasKey(gp => new { gp.GameId, gp.PlatformId });

            builder.Entity<GamePlayerPerspective>()
                .HasKey(gpp => new { gpp.GameId, gpp.PlayerPerspectiveId });

            // Game relationship composite keys
            builder.Entity<GameDlc>()
                .HasKey(gd => new { gd.ParentGameId, gd.DlcGameId });

            builder.Entity<GameExpansion>()
                .HasKey(ge => new { ge.ParentGameId, ge.ExpansionGameId });

            builder.Entity<GamePort>()
                .HasKey(gp => new { gp.OriginalGameId, gp.PortGameId });

            builder.Entity<GameRemake>()
                .HasKey(gr => new { gr.OriginalGameId, gr.RemakeGameId });

            builder.Entity<GameRemaster>()
                .HasKey(gr => new { gr.OriginalGameId, gr.RemasterGameId });

            builder.Entity<SimilarGame>()
                .HasKey(sg => new { sg.GameId, sg.SimilarGameId });

            builder.Entity<GameAgeRating>()
                .HasKey(gar => new { gar.GameId, gar.AgeRatingId });

            // Social features unique constraints
            builder.Entity<Favorite>()
                .HasIndex(f => new { f.UserId, f.GameId })
                .IsUnique();

            builder.Entity<Like>()
                .HasIndex(l => new { l.UserId, l.GameId })
                .IsUnique();

            builder.Entity<Review>()
                .HasIndex(r => new { r.UserId, r.GameId })
                .IsUnique();

            // GameList unique constraints and configurations
            builder.Entity<GameListItem>()
                .HasIndex(gli => new { gli.GameListId, gli.GameId })
                .IsUnique();

            builder.Entity<ReviewLike>()
                .HasIndex(rl => new { rl.UserId, rl.ReviewId })
                .IsUnique();

            builder.Entity<GameListLike>()
                .HasIndex(gll => new { gll.UserId, gll.GameListId })
                .IsUnique();

            builder.Entity<CommentLike>()
                .HasIndex(cl => new { cl.UserId, cl.CommentId })
                .IsUnique();

            builder.Entity<Follow>()
                .HasIndex(f => new { f.FollowerId, f.FollowingId })
                .IsUnique();

            // Comment constraints
            builder.Entity<Comment>()
                .ToTable(t => t.HasCheckConstraint("CK_Comment_Target",
                    "(ReviewId IS NOT NULL AND GameListId IS NULL) OR (ReviewId IS NULL AND GameListId IS NOT NULL)"));

            // game company indexes
            builder.Entity<GameCompany>()
                .HasIndex(gc => gc.Developer);
            builder.Entity<GameCompany>()
                .HasIndex(gc => gc.Publisher);
            builder.Entity<GameCompany>()
                .HasIndex(gc => gc.Porting);
            builder.Entity<GameCompany>()
                .HasIndex(gc => gc.Supporting);

            // Configure game relationship foreign keys
            builder.Entity<GameDlc>()
                .HasOne(gd => gd.ParentGame)
                .WithMany(g => g.ParentGameDlcs)
                .HasForeignKey(gd => gd.ParentGameId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GameDlc>()
                .HasOne(gd => gd.DlcGame)
                .WithMany(g => g.DlcGames)
                .HasForeignKey(gd => gd.DlcGameId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GameExpansion>()
                .HasOne(ge => ge.ParentGame)
                .WithMany(g => g.ParentGameExpansions)
                .HasForeignKey(ge => ge.ParentGameId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GameExpansion>()
                .HasOne(ge => ge.ExpansionGame)
                .WithMany(g => g.ExpansionGames)
                .HasForeignKey(ge => ge.ExpansionGameId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GamePort>()
                .HasOne(gp => gp.OriginalGame)
                .WithMany(g => g.OriginalGamePorts)
                .HasForeignKey(gp => gp.OriginalGameId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GamePort>()
                .HasOne(gp => gp.PortGame)
                .WithMany(g => g.PortGames)
                .HasForeignKey(gp => gp.PortGameId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GameRemake>()
                .HasOne(gr => gr.OriginalGame)
                .WithMany(g => g.OriginalGameRemakes)
                .HasForeignKey(gr => gr.OriginalGameId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GameRemake>()
                .HasOne(gr => gr.RemakeGame)
                .WithMany(g => g.RemakeGames)
                .HasForeignKey(gr => gr.RemakeGameId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GameRemaster>()
                .HasOne(gr => gr.OriginalGame)
                .WithMany(g => g.OriginalGameRemasters)
                .HasForeignKey(gr => gr.OriginalGameId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GameRemaster>()
                .HasOne(gr => gr.RemasterGame)
                .WithMany(g => g.RemasterGames)
                .HasForeignKey(gr => gr.RemasterGameId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SimilarGame>()
                .HasOne(sg => sg.Game)
                .WithMany(g => g.SimilarGames)
                .HasForeignKey(sg => sg.GameId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SimilarGame>()
                .HasOne(sg => sg.SimilarGameRef)
                .WithMany(g => g.SimilarToGames)
                .HasForeignKey(sg => sg.SimilarGameId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Cover>()
                .HasIndex(c => c.IgdbId);

            builder.Entity<ReleaseDate>()
                .HasIndex(grd => grd.GameId);

            builder.Entity<ReleaseDate>()
                .HasIndex(grd => grd.PlatformId);

            // configure unique constraints for reference models
            builder.Entity<Genre>()
                .HasIndex(g => g.IgdbId)
                .IsUnique();
            builder.Entity<Genre>()
                .HasIndex(c => c.Slug);
            builder.Entity<RatingOrganization>()
                .HasIndex(ro => ro.IgdbId)
                .IsUnique();
            builder.Entity<AgeRatingCategory>()
                .HasIndex(arc => arc.IgdbId)
                .IsUnique();
            builder.Entity<AgeRating>()
                .HasIndex(ar => ar.IgdbId)
                .IsUnique();
            builder.Entity<Franchise>()
                .HasIndex(f => f.Slug);
            builder.Entity<Franchise>()
                .HasIndex(f => f.IgdbId)
                .IsUnique();
            builder.Entity<GameMode>()
                .HasIndex(gm => gm.IgdbId)
                .IsUnique();
            builder.Entity<GameType>()
                .HasIndex(gt => gt.IgdbId)
                .IsUnique();
            builder.Entity<Company>()
                .HasIndex(c => c.IgdbId)
                .IsUnique();
            builder.Entity<Platform>()
                .HasIndex(p => p.IgdbId)
                .IsUnique();
            builder.Entity<Platform>()
                .HasIndex(p => p.Slug);
            builder.Entity<PlayerPerspective>()
                .HasIndex(pp => pp.IgdbId)
                .IsUnique();
            builder.Entity<ReleaseDateRegion>()
                .HasIndex(r => r.IgdbId)
                .IsUnique();
            builder.Entity<Theme>()
                .HasIndex(g => g.IgdbId)
                .IsUnique();
            builder.Entity<Theme>()
                .HasIndex(c => c.Slug);
            builder.Entity<ExternalReviewer>()
                .HasIndex(er => er.IgdbId);
            builder.Entity<ExternalReviewer>()
                .HasIndex(er => er.Source);
            builder.Entity<ExternalReviews>()
                .HasIndex(er => new { er.ExternalReviewerId, er.GameId }); 
            builder.Entity<ExternalReviews>()
                .HasIndex(er => er.GameId);  

            // // Configure enum conversion
            // builder.Entity<Company>()
            //     .Property(c => c.Role)
            //     .HasConversion<string>();
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (BaseEntity)entry.Entity;
                entity.UpdatedAt = DateTime.UtcNow;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}