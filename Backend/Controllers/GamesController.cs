using Microsoft.AspNetCore.Mvc;
using Backend.Services;

using System.Security.Claims;
using Backend.Models.DTO.Response;
using Backend.Models.DTO.Game;
using Backend.Models.DTO.Request;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/games")]
    public class GamesController : ControllerBase
    {
        private readonly IGameService _gameService;
        private readonly ILogger<GamesController> _logger;

        public GamesController(IGameService gameService, ILogger<GamesController> logger)
        {
            _gameService = gameService;
            _logger = logger;
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<PagedResponse<GameDto>>>> GetGames(
            [FromQuery] GameSearchParams searchParams)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var result = await _gameService.GetGamesAsync(searchParams,
                    Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null);
                if (result == null || result.Data == null || result.Data.Count <= 0)
                {
                    return NotFound(new ApiResponse<PagedResponse<GameDto>>
                    {
                        Success = false,
                        Message = "No games found",
                        Data = new PagedResponse<GameDto>()
                    });
                }
                return Ok(new ApiResponse<PagedResponse<GameDto>>
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching games");
                return StatusCode(500, new ApiResponse<PagedResponse<GameDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching the games",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<GameDto>>> GetGame(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var game = await _gameService.GetGameByIdAsync(id,
                    userId != null ? Guid.Parse(userId) : Guid.Empty);
                if (game == null)
                {
                    return NotFound(new ApiResponse<GameDto>
                    {
                        Success = false,
                        Message = "Game not found"
                    });
                }
                return Ok(new ApiResponse<GameDto>
                {
                    Success = true,
                    Data = game
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching game with ID {Id}", id);
                return StatusCode(500, new ApiResponse<GameDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching the game",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpGet("{slug}")]
        public async Task<ActionResult<ApiResponse<GameDto>>> GetGameBySlug(string slug)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var game = await _gameService.GetGameBySlugAsync(slug,
                    userId != null ? Guid.Parse(userId) : Guid.Empty);
                if (game == null)
                {
                    return NotFound(new ApiResponse<GameDto>
                    {
                        Success = false,
                        Message = "Game not found"
                    });
                }
                return Ok(new ApiResponse<GameDto>
                {
                    Success = true,
                    Data = game
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching game with slug {Slug}", slug);
                return StatusCode(500, new ApiResponse<GameDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching the game",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpGet("{id:int}/similar")]
        public async Task<ActionResult<ApiResponse<List<GameDto>>>> GetSimilarGames(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var similarGames = await _gameService.GetSimilarGamesAsync(id);
                if (similarGames == null || similarGames.Count <= 0)
                {
                    return NotFound(new ApiResponse<List<GameDto>>
                    {
                        Success = false,
                        Message = "No similar games found"
                    });
                }
                return Ok(new ApiResponse<List<GameDto>>
                {
                    Success = true,
                    Data = similarGames
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching similar games for game ID {Id}", id);
                return StatusCode(500, new ApiResponse<List<GameDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching similar games",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpGet("franchise/{franchiseId:int}")]
        public async Task<ActionResult<ApiResponse<List<GameDto>>>> GetGamesByFranchise(int franchiseId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var games = await _gameService.GetGamesByFranchiseAsync(franchiseId);
                if (games == null || games.Count <= 0)
                {
                    return NotFound(new ApiResponse<List<GameDto>>
                    {
                        Success = false,
                        Message = "No games found for this franchise"
                    });
                }
                return Ok(new ApiResponse<List<GameDto>>
                {
                    Success = true,
                    Data = games
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching games for franchise ID {FranchiseId}",
                    franchiseId);
                return StatusCode(500, new ApiResponse<List<GameDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching games for the franchise",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpGet("popular")]
        public async Task<ActionResult<ApiResponse<List<GameDto>>>> GetPopularGames(
            [FromQuery] String limit)
        {
            try
            {
                var popularGames = await _gameService.GetPopularGamesAsync(
                        Int32.TryParse(limit, out int popularGamesLimit) ?
                            popularGamesLimit : 20);
                if (popularGames == null || popularGames.Count <= 0)
                {
                    return NotFound(new ApiResponse<List<GameDto>>
                    {
                        Success = false,
                        Message = "No popular games found"
                    });
                }
                return Ok(new ApiResponse<List<GameDto>>
                {
                    Success = true,
                    Data = popularGames
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching popular games");
                return StatusCode(500, new ApiResponse<List<GameDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching popular games",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpGet("new")]
        public async Task<ActionResult<ApiResponse<List<GameDto>>>> GetNewGames(
            [FromQuery] String limit)
        {
            try
            {
                var newGames = await _gameService.GetNewGamesAsync(
                        Int32.TryParse(limit, out int gamesLimit) ?
                            gamesLimit : 20);
                if (newGames == null || newGames.Count <= 0)
                {
                    return NotFound(new ApiResponse<List<GameDto>>
                    {
                        Success = false,
                        Message = "No popular games found"
                    });
                }
                return Ok(new ApiResponse<List<GameDto>>
                {
                    Success = true,
                    Data = newGames
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching popular games");
                return StatusCode(500, new ApiResponse<List<GameDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching popular games",
                    Errors = [ex.Message]
                });
            }
        }

        [HttpGet("genre/{genreId:int}")]
        public async Task<ActionResult<ApiResponse<List<GameDto>>>> GetGamesByGenre(int genreId, [FromQuery] int limit = 20)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var games = await _gameService.GetGamesByGenreAsync(genreId, limit);
                if (games == null || !games.Any())
                {
                    return NotFound(new ApiResponse<List<GameDto>>
                    {
                        Success = false,
                        Message = "No games found for this genre"
                    });
                }
                return Ok(new ApiResponse<List<GameDto>>
                {
                    Success = true,
                    Data = games
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching games for genre ID {GenreId}", genreId);
                return StatusCode(500, new ApiResponse<List<GameDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching games for the genre",
                    Errors = [ex.Message]
                });
            }
        }
    }
}