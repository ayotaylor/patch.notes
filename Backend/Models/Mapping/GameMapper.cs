using Backend.Models.DTO.Game;
using Backend.Models.DTO.Social;
using Backend.Models.Game;
using Backend.Models.Game.ReferenceModels;
using Game.Models.ReferenceModels;

namespace Backend.Mapping
{
    public static class GameMapper
    {
        public static GameDto ToDto(this Backend.Models.Game.Game game)
        {
            return new GameDto
            {
                IgdbId = game.IgdbId,
                Name = game.Name,
                Slug = game.Slug,
                Storyline = game.Storyline,
                Summary = game.Summary,
                FirstReleaseDate = game.FirstReleaseDate,
                Hypes = game.Hypes,
                IgdbRating = game.Rating,
                IgdbTotalRating = game.TotalRating,
                IgdbTotalRatingCount = game.TotalRatingCount,
                Genres = game.GameGenres.Select(g => g.Genre.ToDto()).ToList(),
                AltNames = game.AltNames.Select(an => an.ToDto()).ToList(),
                Platforms = game.ReleaseDates
                    .Select(rd => rd.Platform.ToDto()).Distinct().ToList(),
                AgeRatings = game.GameAgeRatings
                    .Select(ar => ar.AgeRating.ToDto()).ToList(),
                GameType = game.GameType.ToDto(),
                Covers = game.Covers.Select(c => c.ToDto()).ToList(),
                Screenshots = game.Screenshots.Select(s => s.ToDto()).ToList(),
                ReleaseDates = game.ReleaseDates
                    .Select(rd => rd.ToDto()).ToList(),
                Franchises = game.GameFranchises
                    .Select(f => f.Franchise.ToDto()).ToList(),
                GameModes = game.GameModes
                    .Select(gm => gm.GameMode.ToDto()).ToList(),
                Companies = game.GameCompanies
                    .Select(gc => gc.Company.ToDto(game.Id)).ToList(),
                PlayerPerspectives = game.GamePlayerPerspectives
                    .Select(pp => pp.PlayerPerspective.ToDto()).ToList(),
                Themes = game.GameThemes
                    .Select(gt => gt.Theme.ToDto()).ToList(),
                Dlcs = game.DlcGames.Select(d => d.DlcGame.ToRelationshipDto()).ToList(),
                Expansions = game.ExpansionGames
                    .Select(e => e.ExpansionGame.ToRelationshipDto()).ToList(),
                Ports = game.PortGames.Select(p => p.PortGame.ToRelationshipDto()).ToList(),
                Remakes = game.RemakeGames.Select(r => r.RemakeGame.ToRelationshipDto()).ToList(),
                Remasters = game.RemasterGames
                    .Select(rm => rm.RemasterGame.ToRelationshipDto()).ToList(),
                SimilarGames = game.SimilarGames
                    .Select(sg => sg.SimilarGameRef.ToRelationshipDto()).ToList(),
                LikesCount = game.Likes.Count,
                FavoritesCount = game.Favorites.Count,
                // IsLikedByUser = game.Likes.Any(l => l.UserId == null), // assuming UserId is nullable
                // IsFavoriteByUser = game.Favorites.Any(f => f.User
            };
        }

        // Simple DTO for related games to avoid circular references and deep nesting
        // public static GameSimpleDto ToSimpleDto(this Backend.Models.Game.Game game)
        // {
        //     return new GameSimpleDto
        //     {
        //         Id = game.Id,
        //         IgdbId = game.IgdbId,
        //         Name = game.Name,
        //         Slug = game.Slug,
        //         Summary = game.Summary,
        //         FirstReleaseDate = game.FirstReleaseDate,
        //         IgdbRating = game.IgdbRating,
                
        //         // Only basic info for related games
        //         Covers = game.Covers.Take(1).Select(c => c.ToSimpleDto()).ToList() // Just primary cover
        //     };
        // }

        public static GenreDto ToDto(this Genre genre)
        {
            return new GenreDto
            {
                IgdbId = genre.IgdbId,
                Name = genre.Name,
                Slug = genre.Slug
            };
        }

        public static ThemeDto ToDto(this Theme theme)
        {
            return new ThemeDto
            {
                IgdbId = theme.IgdbId,
                Name = theme.Name,
                Slug = theme.Slug
            };
        }

        public static AltNameDto ToDto(this AltName altName)
        {
            return new AltNameDto
            {
                Name = altName.Name
            };
        }

        public static PlatformDto ToDto(this Platform platform)
        {
            return new PlatformDto
            {
                IgdbId = platform.IgdbId,
                Name = platform.Name,
                Slug = platform.Slug,
                Abbreviation = platform.Abbreviation,
                AlternativeName = platform.AlternativeName,
            };
        }

        public static AgeRatingDto ToDto(this AgeRating ageRating)
        {
            return new AgeRatingDto
            {
                Name = ageRating.AgeRatingCategory.Rating,
                RatingOrganization = ageRating.AgeRatingCategory?.RatingOrganization?.ToDto(),
            };
        }

        public static GameTypeDto ToDto(this GameType gameType)
        {
            ArgumentNullException.ThrowIfNull(gameType);

            return new GameTypeDto
            {
                IgdbId = gameType.IgdbId,
                Type = gameType.Type
            };
        }

        public static RatingOrganizationDto ToDto(this RatingOrganization ratingOrganization)
        {
            return new RatingOrganizationDto
            {
                Name = ratingOrganization.Name
            };
        }

        public static GameCoverDto ToDto(this Cover cover)
        {
            return new GameCoverDto
            {
                ImageId = cover.ImageId,
                Url = cover.Url,
                Width = cover.Width,
                Height = cover.Height
            };
        }

        public static GameScreenshotDto ToDto(this Screenshot screenshot)
        {
            return new GameScreenshotDto
            {
                ImageId = screenshot.ImageId,
                Url = screenshot.Url,
                Width = screenshot.Width,
                Height = screenshot.Height
            };
        }

        public static ReleaseDateDto ToDto(this ReleaseDate releaseDate)
        {
            return new ReleaseDateDto
            {
                Platform = releaseDate.Platform.ToDto(),
                Date = releaseDate.Date,
                Region = releaseDate.ReleaseDateRegion.ToDto(),
            };
        }

        public static RegionDto ToDto(this ReleaseDateRegion region)
        {
            return new RegionDto
            {
                Name = region.Region,
            };
        }

        public static FranchiseDto ToDto(this Franchise franchise)
        {
            return new FranchiseDto
            {
                IgdbId = franchise.IgdbId,
                Name = franchise.Name,
                Slug = franchise.Slug
            };
        }

        public static GameModeDto ToDto(this GameMode gameMode)
        {
            return new GameModeDto
            {
                IgdbId = gameMode.IgdbId,
                Name = gameMode.Name,
                Slug = gameMode.Slug
            };
        }
        
        public static CompanyDto ToDto(this Company company)
        {
            var roles = new List<string>();

            if (company.GameCompanies.Count > 0)
            {
                var gameCompanies = company.GameCompanies.FirstOrDefault();
                if (gameCompanies != null)
                {
                    if (gameCompanies.Developer)
                    {
                        roles.Add(CompanyRole.Developer.ToString());
                    }
                    if (gameCompanies.Publisher)
                    {
                        roles.Add(CompanyRole.Publisher.ToString());
                    }
                    if (gameCompanies.Porting)
                    {
                        roles.Add(CompanyRole.Porting.ToString());
                    }
                    if (gameCompanies.Supporting)
                    {
                        roles.Add(CompanyRole.Supporting.ToString());
                    }
                }
            }
            
            return new CompanyDto
            {
                IgdbId = company.IgdbId,
                Name = company.Name,
                Country = company.CountryCode,
                Description = company.Description,
                Url = company.Url,
                Roles = roles
            };
        }

        public static CompanyDto ToDto(this Company company, Guid gameId)
        {
            var gameCompany = company.GameCompanies.FirstOrDefault(gc => gc.GameId == gameId);
            var roles = new List<string>();

            if (gameCompany != null)
            {
                if (gameCompany.Developer)
                {
                    roles.Add(CompanyRole.Developer.ToString());
                }
                if (gameCompany.Publisher)
                {
                    roles.Add(CompanyRole.Publisher.ToString());
                }
                if (gameCompany.Porting)
                {
                    roles.Add(CompanyRole.Porting.ToString());
                }
                if (gameCompany.Supporting)
                {
                    roles.Add(CompanyRole.Supporting.ToString());
                }
            }

            return new CompanyDto
            {
                IgdbId = company.IgdbId,
                Name = company.Name,
                Country = company.CountryCode,
                Description = company.Description,
                Url = company.Url,
                Roles = roles
            };
        }

        public static PlayerPerspectiveDto ToDto(this PlayerPerspective playerPerspective)
        {
            return new PlayerPerspectiveDto
            {
                IgdbId = playerPerspective.IgdbId,
                Name = playerPerspective.Name,
                Slug = playerPerspective.Slug
            };
        }

        // New mapping methods for summary DTOs
        public static GameRelationshipDto ToRelationshipDto(this Backend.Models.Game.Game game)
        {
            return new GameRelationshipDto
            {
                IgdbId = game.IgdbId,
                Name = game.Name,
                Slug = game.Slug,
                CoverUrl = game.Covers?.FirstOrDefault()?.Url,
                IgdbRating = game.Rating,
                IgdbTotalRating = game.TotalRating,
                IgdbTotalRatingCount = game.TotalRatingCount,
                FirstReleaseDate = game.FirstReleaseDate
            };
        }

        public static GameSearchResultDto ToSearchResultDto(this Backend.Models.Game.Game game)
        {
            return new GameSearchResultDto
            {
                IgdbId = game.IgdbId,
                Name = game.Name,
                Slug = game.Slug,
                Summary = game.Summary,
                FirstReleaseDate = game.FirstReleaseDate,
                IgdbRating = game.Rating,
                IgdbTotalRating = game.TotalRating,
                IgdbTotalRatingCount = game.TotalRatingCount,
                Hypes = game.Hypes,
                Genres = game.GameGenres.Select(g => g.Genre.ToDto()).ToList(),
                Covers = game.Covers.Select(c => c.ToDto()).ToList(),
                Platforms = game.ReleaseDates
                    .Select(rd => rd.Platform.ToDto()).Distinct().ToList(),
                Companies = game.GameCompanies
                    .Select(gc => gc.Company.ToDto(game.Id)).ToList(),
                LikesCount = game.Likes.Count,
                FavoritesCount = game.Favorites.Count
            };
        }

        public static GameSummaryDto ToSummaryDto(this Backend.Models.Game.Game game)
        {
            return new GameSummaryDto
            {
                IgdbId = game.IgdbId,
                Name = game.Name,
                Slug = game.Slug,
                CoverUrl = game.Covers?.FirstOrDefault()?.Url
            };
        }
    }
}