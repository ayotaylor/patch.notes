using Backend.Models.DTO.Game;
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
                Id = game.Id,
                IgdbId = game.IgdbId,
                Name = game.Name,
                Slug = game.Slug,
                Storyline = game.Storyline,
                Summary = game.Summary,
                FirstReleaseDate = game.FirstReleaseDate,
                Hypes = game.Hypes,
                IgdbRating = game.IgdbRating,
                Genres = game.GameGenres.Select(g => g.Genre.ToDto()).ToList(),
                AltNames = game.AltNames.Select(an => an.ToDto()).ToList(),
                Platforms = game.GamePlatforms.Select(p => p.Platform.ToDto()).ToList(),
                AgeRatings = game.GameAgeRatings.Select(ar => ar.AgeRating.ToDto()).ToList(),
                GameTypes = game.GameTypes.Select(gt => gt.GameType.ToDto()).ToList(),
                Covers = game.Covers.Select(c => c.ToDto()).ToList(),
                Screenshots = game.Screenshots.Select(s => s.ToDto()).ToList(),
                ReleaseDates = game.ReleaseDates.Select(rd => rd.ToDto()).ToList(),
                Franchises = game.GameFranchises.Select(f => f.Franchise.ToDto()).ToList(),
                GameModes = game.GameModes.Select(gm => gm.GameMode.ToDto()).ToList(),
                Companies = game.GameCompanies.Select(gc => gc.Company.ToDto()).ToList(),
                PlayerPerspectives = game.GamePlayerPerspectives.Select(pp => pp.PlayerPerspective.ToDto()).ToList(),
                Dlcs = game.DlcGames.Select(d => d.DlcGame.ToDto()).ToList(),
                Expansions = game.ExpansionGames.Select(e => e.ExpansionGame.ToDto()).ToList(),
                Ports = game.PortGames.Select(p => p.PortGame.ToDto()).ToList(),
                Remakes = game.RemakeGames.Select(r => r.RemakeGame.ToDto()).ToList(),
                Remasters = game.RemasterGames.Select(rm => rm.RemasterGame.ToDto()).ToList(),
                // TODO: figure out how to handle similar games
                SimilarGames = game.SimilarGames.Select(sg => sg.Game.ToDto()).ToList(),
                LikesCount = game.Likes.Count,
                FavoritesCount = game.Favorites.Count,
                // IsLikedByUser = game.Likes.Any(l => l.UserId == null), // assuming UserId is nullable
                // IsFavoriteByUser = game.Favorites.Any(f => f.User
            };
        }

        public static GenreDto ToDto(this Genre genre)
        {
            return new GenreDto
            {
                Id = genre.Id,
                IgdbId = genre.IgdbId,
                Name = genre.Name,
                Slug = genre.Slug
            };
        }

        public static AltNameDto ToDto(this AltName altName)
        {
            return new AltNameDto
            {
                Id = altName.Id,
                Name = altName.Name
            };
        }

        public static PlatformDto ToDto(this Platform platform)
        {
            return new PlatformDto
            {
                Id = platform.Id,
                IgdbId = platform.IgdbId,
                Name = platform.Name,
                Slug = platform.Slug
            };
        }

        public static AgeRatingDto ToDto(this AgeRating ageRating)
        {
            return new AgeRatingDto
            {
                Id = ageRating.Id,
                Rating = ageRating.Name,
                RatingOrganization = ageRating.RatingOrganization.ToDto()
            };
        }

        public static GameTypeDto ToDto(this GameType gameType)
        {
            return new GameTypeDto
            {
                Id = gameType.Id,
                IgdbId = gameType.IgdbId,
                Type = gameType.Type
            };
        }

        public static RatingOrganizationDto ToDto(this RatingOrganization ratingOrganization)
        {
            return new RatingOrganizationDto
            {
                Id = ratingOrganization.Id,
                Name = ratingOrganization.Name
            };
        }

        public static GameCoverDto ToDto(this Cover cover)
        {
            return new GameCoverDto
            {
                Id = cover.Id,
                IgdbImageId = cover.IgdbImageId,
                Url = cover.Url,
                Width = cover.Width,
                Height = cover.Height
            };
        }

        public static GameScreenshotDto ToDto(this Screenshot screenshot)
        {
            return new GameScreenshotDto
            {
                Id = screenshot.Id,
                IgdbImageId = screenshot.IgdbImageId,
                Url = screenshot.Url,
                Width = screenshot.Width,
                Height = screenshot.Height
            };
        }

        public static ReleaseDateDto ToDto(this ReleaseDate releaseDate)
        {
            return new ReleaseDateDto
            {
                Id = releaseDate.Id,
                Platform = releaseDate.Platform.ToDto(),
                Date = releaseDate.Date,
                Region = releaseDate.Region.ToDto(),
            };
        }

        public static RegionDto ToDto(this Region region)
        {
            return new RegionDto
            {
                Id = region.Id,
                RegionName = region.RegionName,
            };
        }

        public static FranchiseDto ToDto(this Franchise franchise)
        {
            return new FranchiseDto
            {
                Id = franchise.Id,
                IgdbId = franchise.IgdbId,
                Name = franchise.Name,
                Slug = franchise.Slug
            };
        }

        public static GameModeDto ToDto(this GameMode gameMode)
        {
            return new GameModeDto
            {
                Id = gameMode.Id,
                IgdbId = gameMode.IgdbId,
                Name = gameMode.Name
            };
        }

        public static CompanyDto ToDto(this Company company)
        {
            return new CompanyDto
            {
                Id = company.Id,
                IgdbId = company.IgdbId,
                Name = company.Name,
                Country = company.Country,
                Description = company.Description,
                Url = company.Url,
                // TODO: come back to this. how to handle Published?
                Role = company.Published ?
                    CompanyRole.Publisher.ToString() : CompanyRole.Developer.ToString()
            };
        }

        public static PlayerPerspectiveDto ToDto(this PlayerPerspective playerPerspective)
        {
            return new PlayerPerspectiveDto
            {
                Id = playerPerspective.Id,
                IgdbId = playerPerspective.IgdbId,
                Name = playerPerspective.Name,
                Slug = playerPerspective.Slug
            };
        }
    }
}