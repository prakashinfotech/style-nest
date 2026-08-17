using StyleNest.SharedKernel.DTOs;

namespace StyleNest.User.API.Services;

public record NotificationDto(
    Guid Id, string Type, string Subject, string Message,
    bool IsRead, DateTime? ReadAt, string? ActionUrl, string? ImageUrl, DateTime CreatedAt);

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, int page, int pageSize, bool unreadOnly);
    Task MarkReadAsync(Guid userId, Guid notificationId);
    Task MarkAllReadAsync(Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
}
