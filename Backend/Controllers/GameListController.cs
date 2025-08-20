using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Backend.Services;
using Backend.Models.DTO.Response;
using Backend.Models.DTO.Social;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/lists")]
    public class GameListController : ControllerBase
    {
        private readonly IGameListService _gameListService;
        private readonly ILogger<GameListController> _logger;

        public GameListController(IGameListService gameListService, ILogger<GameListController> logger)
        {
            _gameListService = gameListService;
            _logger = logger;
        }

        [HttpGet("public")]
        public async Task<ActionResult<ApiResponse<PagedResponse<GameListDto>>>> GetPublicGameLists([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var gameLists = await _gameListService.GetPublicGameListsAsync(page, pageSize);
                return Ok(new ApiResponse<PagedResponse<GameListDto>>
                {
                    Success = true,
                    Data = gameLists,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching public game lists");
                return StatusCode(500, new ApiResponse<PagedResponse<GameListDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching public game lists",
                });
            }
        }

        [HttpGet("user/{userId:guid}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<GameListDto>>>> GetUserGameLists(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var gameLists = await _gameListService.GetUserGameListsAsync(userId, page, pageSize);
                return Ok(new ApiResponse<PagedResponse<GameListDto>>
                {
                    Success = true,
                    Data = gameLists,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching game lists for user {UserId}", userId);
                return StatusCode(500, new ApiResponse<PagedResponse<GameListDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching user game lists",
                });
            }
        }

        [HttpGet("{listId:guid}")]
        public async Task<ActionResult<ApiResponse<GameListDto>>> GetGameList(Guid listId)
        {
            try
            {
                var gameList = await _gameListService.GetGameListAsync(listId);
                if (gameList == null)
                {
                    return NotFound(new ApiResponse<GameListDto>
                    {
                        Success = false,
                        Message = "Game list not found",
                    });
                }

                return Ok(new ApiResponse<GameListDto>
                {
                    Success = true,
                    Data = gameList,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching game list {ListId}", listId);
                return StatusCode(500, new ApiResponse<GameListDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching the game list",
                });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ApiResponse<GameListDto>>> CreateGameList([FromBody] CreateGameListRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    return Unauthorized(new ApiResponse<GameListDto>
                    {
                        Success = false,
                        Message = "User not authenticated",
                    });
                }

                var gameList = await _gameListService.CreateGameListAsync(userGuid, request.Title, request.Description, request.IsPublic, request.GameIds);
                if (gameList == null)
                {
                    return BadRequest(new ApiResponse<GameListDto>
                    {
                        Success = false,
                        Message = "Failed to create game list",
                    });
                }

                return CreatedAtAction(nameof(GetGameList), new { listId = gameList.Id }, new ApiResponse<GameListDto>
                {
                    Success = true,
                    Data = gameList,
                    Message = "Game list created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating game list");
                return StatusCode(500, new ApiResponse<GameListDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the game list",
                });
            }
        }

        [HttpPut("{listId:guid}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<GameListDto>>> UpdateGameList(Guid listId, [FromBody] UpdateGameListRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    return Unauthorized(new ApiResponse<GameListDto>
                    {
                        Success = false,
                        Message = "User not authenticated",
                    });
                }

                if (!await _gameListService.IsUserListOwnerAsync(listId, userGuid))
                {
                    return Forbid();
                }

                var gameList = await _gameListService.UpdateGameListAsync(listId, request.Title, request.Description, request.IsPublic);
                if (gameList == null)
                {
                    return NotFound(new ApiResponse<GameListDto>
                    {
                        Success = false,
                        Message = "Game list not found",
                    });
                }

                return Ok(new ApiResponse<GameListDto>
                {
                    Success = true,
                    Data = gameList,
                    Message = "Game list updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating game list {ListId}", listId);
                return StatusCode(500, new ApiResponse<GameListDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the game list",
                });
            }
        }

        [HttpDelete("{listId:guid}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> DeleteGameList(Guid listId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated",
                    });
                }

                if (!await _gameListService.IsUserListOwnerAsync(listId, userGuid))
                {
                    return Forbid();
                }

                var success = await _gameListService.DeleteGameListAsync(listId);
                if (!success)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Game list not found",
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Game list deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting game list {ListId}", listId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the game list",
                });
            }
        }

        [HttpPost("{listId:guid}/games/{gameId:int}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> AddGameToList(Guid listId, int gameId, [FromBody] AddGameToListRequest? request = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated",
                    });
                }

                if (!await _gameListService.IsUserListOwnerAsync(listId, userGuid))
                {
                    return Forbid();
                }

                var success = await _gameListService.AddGameToListAsync(listId, gameId, request?.Note);
                if (!success)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Failed to add game to list. Game may already be in the list or not found.",
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Game added to list successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding game {GameId} to list {ListId}", gameId, listId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while adding the game to the list",
                });
            }
        }

        [HttpDelete("{listId:guid}/games/{gameId:int}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> RemoveGameFromList(Guid listId, int gameId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated",
                    });
                }

                if (!await _gameListService.IsUserListOwnerAsync(listId, userGuid))
                {
                    return Forbid();
                }

                var success = await _gameListService.RemoveGameFromListAsync(listId, gameId);
                if (!success)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Failed to remove game from list. Game may not be in the list.",
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Game removed from list successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing game {GameId} from list {ListId}", gameId, listId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while removing the game from the list",
                });
            }
        }
    }

    public class CreateGameListRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; } = true;
        public List<int>? GameIds { get; set; }
    }

    public class UpdateGameListRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool? IsPublic { get; set; }
    }

    public class AddGameToListRequest
    {
        public string? Note { get; set; }
    }
}