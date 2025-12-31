using Backend.Models.DTO.Response;
using Backend.Models.DTO.User;

namespace Backend.Services
{
    public interface IUserService
    {
        Task<PagedResponse<UserDto>> GetAllUsersAsync(int page, int pageSize);
        Task<List<UserDto>> GetPopularUsersAsync(int limit);
        Task<List<UserDto>> GetFeaturedUsersAsync(int limit);
    }
}
