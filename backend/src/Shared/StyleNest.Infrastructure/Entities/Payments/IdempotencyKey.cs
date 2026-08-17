/**
 * ENH-PAY-004 — IdempotencyKeys table with composite covering index.
 * Persists idempotency entries to the database so that:
 *   • On-call engineers can audit duplicate-rejection history
 *   • The system can rebuild the Redis hot-cache from DB after a cache flush
 *   • Analytics can detect clients that fire duplicate requests excessively
 *
 * Index strategy (TSD §6.2 / TE-005):
 *   PRIMARY lookup : IX_IdempotencyKeys_KeyId (unique)
 *   ANALYTICAL     : IX_IdempotencyKeys_UserId_Endpoint  (composite, covering)
 *                    INCLUDE (KeyId, StatusCode, ExpiresAt)
 *                    — lets admin/analyst queries scan all keys for a user+endpoint
 *                      without touching the clustered index
 */

using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Payments;

/// <summary>
/// ENH-PAY-004 — Durable idempotency-key record.
/// One row per (KeyId, Endpoint) request; replayed via <see cref="StatusCode"/> + <see cref="ResponseBody"/>.
/// </summary>
public class IdempotencyKey : BaseEntity<Guid>
{
    /// <summary>The client-supplied UUIDv4 from the <c>Idempotency-Key</c> HTTP header.</summary>
    public Guid KeyId { get; set; }

    /// <summary>
    /// User who submitted the request (null for unauthenticated endpoints).
    /// Part of the composite analytical index.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// HTTP method + path, e.g. <c>POST /api/v1/orders</c>.
    /// Part of the composite analytical index.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>HTTP status code of the first (canonical) response.</summary>
    public int StatusCode { get; set; }

    /// <summary>JSON-serialised response body of the first response (replayed on duplicates).</summary>
    public string ResponseBody { get; set; } = string.Empty;

    /// <summary>When this record expires and can be pruned (default: 24 h after creation).</summary>
    public DateTime ExpiresAt { get; set; }
}
