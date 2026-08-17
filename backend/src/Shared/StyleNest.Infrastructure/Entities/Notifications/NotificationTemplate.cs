using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Notifications;

public enum NotificationType { Email, Sms, Push, InApp }

public class NotificationTemplate : BaseEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsActive { get; set; } = true;
}
