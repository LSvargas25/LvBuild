using LvApplication.Services.Notifications;
using LvDomain.Entities.Notifications;
using LvInfrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Repositories.Notifications;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<Notification> notifications)
    {
        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();
    }

    public Task<Notification?> GetByIdAsync(int id) =>
        _context.Notifications.FirstOrDefaultAsync(n => n.Id == id);

    public async Task UpdateAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<Notification> Items, int TotalCount)> GetPagedForUserAsync(
        int userId,
        int pageNumber,
        int pageSize
    )
    {
        var query = _context.Notifications.Where(n => n.UserId == userId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
