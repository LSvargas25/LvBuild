using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Notifications;
using LvDomain.Entities.Notifications;
using LvDomain.Enums;

namespace LvApplication.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task NotifyAsync(IEnumerable<int> userIds, NotificationType type, string message)
    {
        var now = DateTime.UtcNow;
        var notifications = userIds
            .Distinct()
            .Select(userId => new Notification
            {
                UserId = userId,
                Type = type,
                Message = message,
                IsRead = false,
                CreatedAt = now
            })
            .ToList();

        if (notifications.Count == 0)
            return;

        await _notificationRepository.AddRangeAsync(notifications);
    }

    public async Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(int userId, int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _notificationRepository.GetPagedForUserAsync(userId, pageNumber, pageSize);

        return new PagedResult<NotificationDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task MarkAsReadAsync(int id, int userId)
    {
        var notification = await _notificationRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Notification {id} not found.");

        if (notification.UserId != userId)
            throw new NotFoundException($"Notification {id} not found.");

        if (notification.IsRead)
            return;

        notification.IsRead = true;
        notification.UpdatedAt = DateTime.UtcNow;
        await _notificationRepository.UpdateAsync(notification);
    }

    private static NotificationDto MapToDto(Notification notification) => new()
    {
        Id = notification.Id,
        Type = notification.Type,
        Message = notification.Message,
        IsRead = notification.IsRead,
        CreatedAt = notification.CreatedAt
    };
}
