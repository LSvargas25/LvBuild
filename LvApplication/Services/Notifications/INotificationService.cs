using LvApplication.Common;
using LvApplication.DTOs.Notifications;
using LvDomain.Enums;

namespace LvApplication.Services.Notifications;

public interface INotificationService
{
    /// <summary>Raises the same notification for a set of recipients. Used internally by other modules' services (e.g. low-stock on invoice issuance).</summary>
    Task NotifyAsync(IEnumerable<int> userIds, NotificationType type, string message);

    Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(int userId, int pageNumber, int pageSize);
    Task MarkAsReadAsync(int id, int userId);
}
