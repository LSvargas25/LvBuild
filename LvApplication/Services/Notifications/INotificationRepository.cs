using LvDomain.Entities.Notifications;

namespace LvApplication.Services.Notifications;

public interface INotificationRepository
{
    Task AddRangeAsync(IEnumerable<Notification> notifications);
    Task<Notification?> GetByIdAsync(int id);
    Task UpdateAsync(Notification notification);
    Task<(List<Notification> Items, int TotalCount)> GetPagedForUserAsync(int userId, int pageNumber, int pageSize);
}
