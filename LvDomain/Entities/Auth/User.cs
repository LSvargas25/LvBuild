using LvDomain.Common;
using LvDomain.Entities.Storage;
using LvDomain.Enums;

namespace LvDomain.Entities.Auth;

public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? BlockedAt { get; set; }
    public int? ProfilePhotoFileId { get; set; }
    public StoredFile? ProfilePhotoFile { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
