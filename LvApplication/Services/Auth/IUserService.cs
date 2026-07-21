using LvApplication.Common;
using LvApplication.DTOs.Auth;

namespace LvApplication.Services.Auth;

public interface IUserService
{
    Task<UserResponseDto> CreateUserAsync(CreateUserDto request, IEnumerable<string> actingUserRoles);
    Task<UserResponseDto> GetByIdAsync(int id);
    Task<PagedResult<UserResponseDto>> GetAllAsync(int pageNumber, int pageSize);
}
