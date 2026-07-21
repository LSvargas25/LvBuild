using LvDomain.Entities.Auth;

namespace LvApplication.Services.Auth;

public class AccessTokenResult
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    DateTime GetRefreshTokenExpiresAt();
}
