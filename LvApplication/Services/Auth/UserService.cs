using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Auth;
using LvDomain.Entities.Auth;
using LvDomain.Enums;

namespace LvApplication.Services.Auth;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<CreateUserDto> _createValidator;

    public UserService(IUserRepository userRepository, IValidator<CreateUserDto> createValidator)
    {
        _userRepository = userRepository;
        _createValidator = createValidator;
    }

    public async Task<UserResponseDto> CreateUserAsync(CreateUserDto request)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        if (await _userRepository.EmailExistsAsync(request.Email))
        {
            throw new ConflictException("Email is already registered.");
        }

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var roleId in request.RoleIds.Distinct())
        {
            user.UserRoles.Add(new UserRole { RoleId = roleId });
        }

        await _userRepository.AddAsync(user);

        var created = await _userRepository.GetByIdAsync(user.Id) ?? user;
        return MapToDto(created);
    }

    public async Task<UserResponseDto> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id) ?? throw new NotFoundException($"User {id} not found.");
        return MapToDto(user);
    }

    public async Task<PagedResult<UserResponseDto>> GetAllAsync(int pageNumber, int pageSize)
    {
        var (users, totalCount) = await _userRepository.GetPagedAsync(pageNumber, pageSize);

        return new PagedResult<UserResponseDto>
        {
            Items = users.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private static UserResponseDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        Status = user.Status.ToString(),
        Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
        LastLoginAt = user.LastLoginAt
    };
}
