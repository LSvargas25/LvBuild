using LvDomain.Common;
using LvDomain.Entities.Auth;
using LvDomain.Enums;

namespace LvDomain.Entities.Notifications;

public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }

    public User? User { get; set; }
}
