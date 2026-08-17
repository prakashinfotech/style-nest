using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Notifications;

public class NotificationLog : BaseEntity<Guid>
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public string? ActionUrl { get; set; }
    public string? ImageUrl { get; set; }
}
