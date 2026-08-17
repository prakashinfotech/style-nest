namespace StyleNest.Infrastructure.Entities.Admin;

public enum RetentionCategory
{
    NonFinancial = 0,  // 3-year retention
    Financial    = 1,  // 7-year retention (payments, orders, refunds)
}

/// <summary>
/// ENH-ADMIN-001 — Append-only audit trail.
/// Does NOT extend BaseEntity: no IsDeleted, no UpdatedAt (records are immutable).
/// RetentionCategory drives ExpiresAt: Financial → 7y, NonFinancial → 3y.
/// </summary>
public sealed class AuditLog
{
    public Guid      Id                { get; set; }
    public DateTime  Timestamp         { get; set; }   // UTC
    public Guid?     ActorId           { get; set; }   // null = system / background job
    public string?   ActorName         { get; set; }
    public string    Action            { get; set; } = string.Empty;     // e.g. "User.Erased"
    public string    EntityType        { get; set; } = string.Empty;     // e.g. "User"
    public string?   EntityId          { get; set; }
    public string?   OldValue          { get; set; }   // JSON snapshot
    public string?   NewValue          { get; set; }   // JSON snapshot
    public string?   IpAddress         { get; set; }
    public RetentionCategory RetentionCategory { get; set; }
    public DateTime  ExpiresAt         { get; set; }   // Timestamp + 7y or 3y
}
