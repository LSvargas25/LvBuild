using LvDomain.Common;

namespace LvDomain.Entities.Auth;

public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool Revoked { get; set; } = false;
    public string? CreatedByIp { get; set; }
}
