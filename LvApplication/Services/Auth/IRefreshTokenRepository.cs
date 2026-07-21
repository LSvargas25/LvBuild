using LvDomain.Entities.Auth;

namespace LvApplication.Services.Auth;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task AddAsync(RefreshToken refreshToken);
    Task UpdateAsync(RefreshToken refreshToken);
    Task RevokeAllActiveTokensForUserAsync(int userId);
}
