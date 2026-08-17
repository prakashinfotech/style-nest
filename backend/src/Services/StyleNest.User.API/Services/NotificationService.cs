using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Persistence;
using StyleNest.SharedKernel.DTOs;

namespace StyleNest.User.API.Services;

public class NotificationService(AppDbContext db) : INotificationService
{
    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(
        Guid userId, int page, int pageSize, bool unreadOnly)
    {
        var query = db.NotificationLogs
            .Where(n => n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        query = query.OrderByDescending(n => n.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<NotificationDto>(
            items.Select(n => new NotificationDto(
                n.Id, n.Type, n.Subject, n.Message,
                n.IsRead, n.ReadAt, n.ActionUrl, n.ImageUrl, n.CreatedAt)).ToList(),
            total, page, pageSize);
    }

    public async Task MarkReadAsync(Guid userId, Guid notificationId)
    {
        var notification = await db.NotificationLogs
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification is not null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task MarkAllReadAsync(Guid userId)
    {
        var unread = await db.NotificationLogs
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        var now = DateTime.UtcNow;
        unread.ForEach(n => { n.IsRead = true; n.ReadAt = now; });
        await db.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId) =>
        await db.NotificationLogs.CountAsync(n => n.UserId == userId && !n.IsRead);
}
