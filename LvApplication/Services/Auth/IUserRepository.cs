using LvDomain.Entities.Auth;

namespace LvApplication.Services.Auth;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<bool> EmailExistsAsync(string email);
    Task<(List<User> Users, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
    Task<List<User>> GetByRoleNamesAsync(IEnumerable<string> roleNames);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}
