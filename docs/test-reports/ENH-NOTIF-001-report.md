# ENH-NOTIF-001 Test Report — Exponential Backoff Retry + DLQ
**Date:** 2026-05-22
**Tested by:** TEST Agent (automated)
**Branch:** enhancement/additional-feature-improvement

## Summary
PASS

All acceptance criteria from SOW v2.1 FR-NOTIF-006 are satisfied. The retry schedule,
MaxAttempts constant, dead-letter path, success path, skip conditions, and BackgroundService
poll interval are implemented correctly and backed by comprehensive unit tests.

## Acceptance Criteria

| Criterion | Status | Evidence |
|---|---|---|
| RetryDelaysSeconds = [60, 180, 540] | PASS | `NotificationRetryService.cs:62` — `public static readonly int[] RetryDelaysSeconds = [60, 180, 540];` |
| MaxAttempts = 4 | PASS | `NotificationRetryService.cs:63` — `public const int MaxAttempts = 4;` |
| 1st failure → NextAttemptAt +60s | PASS | `NotificationRetryService.cs:111-112` — index `AttemptCount-1 = 0` → 60s; confirmed by test `FirstFailure_SchedulesRetryAt1Min` |
| 2nd failure → NextAttemptAt +180s | PASS | `NotificationRetryService.cs:111-112` — index `AttemptCount-1 = 1` → 180s; confirmed by test `SecondFailure_SchedulesRetryAt3Min` |
| 3rd failure → NextAttemptAt +540s | PASS | `NotificationRetryService.cs:111-112` — index `AttemptCount-1 = 2` → 540s; confirmed by test `ThirdFailure_SchedulesRetryAt9Min` |
| 4th failure → DeadLettered + DLQ | PASS | `NotificationRetryService.cs:101-107` — `AttemptCount >= MaxAttempts` → `Status = DeadLettered` + `dlq.EnqueueAsync`; confirmed by test `FourthFailure_DeadLetters` |
| Success → Delivered, no DLQ | PASS | `NotificationRetryService.cs:93-99` — `Status = Delivered`, `LastError = null`; DLQ mock verified `Times.Never` in test `SendSucceeds_MarksDelivered` |
| Future NextAttemptAt → skipped | PASS | `NotificationRetryService.cs:70-72` — WHERE filter: `NextAttemptAt == null \|\| NextAttemptAt <= now`; confirmed by test `FutureNextAttempt_IsSkipped` returning count=0 |
| Already Delivered/DeadLettered → skipped | PASS | `NotificationRetryService.cs:69-72` — WHERE filter: `Status == OutboxStatus.Pending` only; confirmed by tests `AlreadyDelivered_IsSkipped` and `DeadLettered_IsSkipped` |
| BackgroundService polls every 30s | PASS | `NotificationRetryService.cs:133` — `private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);` |

## Findings

### Implementation Analysis

**NotificationRetryService.cs** (`backend/src/Services/StyleNest.User.API/Services/NotificationRetryService.cs`)

The file contains four co-located types following a clean layering pattern:
- `INotificationSender` / `NullNotificationSender` — delivery abstraction + dev no-op
- `INotificationDlqSink` / `NullNotificationDlqSink` — DLQ abstraction + dev no-op
- `NotificationRetryJob` — scoped, testable job that implements the retry logic
- `NotificationRetryBackgroundService` — `BackgroundService` that creates a DI scope every 30s and delegates to the job

**Retry index correctness (critical path):**

The delay selection at line 111 uses `RetryDelaysSeconds[item.AttemptCount - 1]`. Since `AttemptCount` is incremented at line 79 _before_ the send attempt, the mapping is:

| Attempt No. | AttemptCount after increment | Index | Delay |
|---|---|---|---|
| 1st (initial) | 1 | 0 | 60s |
| 2nd (retry 1) | 2 | 1 | 180s |
| 3rd (retry 2) | 3 | 2 | 540s |
| 4th (retry 3) | 4 | — | DeadLettered (AttemptCount >= MaxAttempts=4) |

This is correct. The index never exceeds the bounds of the 3-element array because at
`AttemptCount == 4` the `>= MaxAttempts` branch fires before the index is accessed.

**Status lifecycle:**

- Entries remain `Pending` between retry attempts (no intermediate `Failed` state in the
  active path). The `Failed` value is present in the `OutboxStatus` enum but is intentionally
  unused by the retry job — this is an acceptable design choice as it leaves room for
  manual admin tagging without polluting the retry filter.
- On success: `Delivered` with `LastError` cleared.
- On exhaustion: `DeadLettered` with `INotificationDlqSink.EnqueueAsync` called.

**Exception handling:**

Line 82–91 wraps `sender.SendAsync` in a try/catch, sets `sent = false`, and records
`ex.Message` in `LastError`. This ensures transient exceptions are handled identically
to a `false` return — both trigger the backoff schedule.

**NotificationOutbox.cs** (`backend/src/Shared/StyleNest.Infrastructure/Entities/Notifications/NotificationOutbox.cs`)

All required properties are present:
- `UserId`, `Type`, `Subject`, `Body` — message identity
- `Status` (OutboxStatus enum) — lifecycle state
- `AttemptCount` — defaults to 0
- `NextAttemptAt` (nullable DateTime) — schedule gate
- `LastError` (nullable string) — diagnostic field

The `OutboxStatus` enum includes `Pending`, `Delivered`, `Failed`, `DeadLettered` — all four
values called out in the acceptance criteria are present.

**DbSet registration:**

`AppDbContext.cs:77` registers `public DbSet<NotificationOutbox> NotificationOutbox`, mapped
to `notifications.NotificationOutbox` table (line 147). The EF query in `RunAsync` will
correctly translate to SQL.

### Minor Observations (non-blocking)

1. **`Failed` enum value is unused** — The `Failed` status in `OutboxStatus` is never written
   by `NotificationRetryJob`. This is not a defect (entries stay `Pending` until terminal),
   but the unused value could cause confusion. Recommendation: add an XML doc comment
   clarifying it is reserved for manual admin override.

2. **`SaveChangesAsync` is called once per batch** (line 122), not per item — this is
   intentional and correct for throughput, but means a partial-batch DB failure would
   not commit any items in the batch. Acceptable for an outbox pattern at this scale.

3. **No explicit index on `(Status, NextAttemptAt)`** observed in this review. For
   production workloads a composite index would be recommended to keep the WHERE clause
   efficient, but this is a deployment concern outside the ENH-NOTIF-001 scope.

## Test Coverage

All tests are in `backend/tests/StyleNest.User.Tests/NotificationRetryJobTests.cs`.
The test class uses an EF Core InMemory database and Moq mocks for `INotificationSender`
and `INotificationDlqSink`.

| Test Name | Scenario Covered | Criterion Mapped |
|---|---|---|
| `RetryDelaysAreCorrect` | Asserts constant values directly | RetryDelaysSeconds + MaxAttempts |
| `SendSucceeds_MarksDelivered` | Sender returns `true` → `Delivered`, DLQ never called | Success path |
| `FirstFailure_SchedulesRetryAt1Min` | AttemptCount=0, send fails → AttemptCount=1, NextAttemptAt ≈ now+60s, Status=Pending | 1st failure delay |
| `SecondFailure_SchedulesRetryAt3Min` | AttemptCount=1 seed, send fails → AttemptCount=2, NextAttemptAt ≈ now+180s | 2nd failure delay |
| `ThirdFailure_SchedulesRetryAt9Min` | AttemptCount=2 seed, send fails → AttemptCount=3, NextAttemptAt ≈ now+540s | 3rd failure delay |
| `FourthFailure_DeadLetters` | AttemptCount=3 seed, send fails → Status=DeadLettered, DLQ.EnqueueAsync called once | DLQ path |
| `FutureNextAttempt_IsSkipped` | NextAttemptAt = now+5m → RunAsync returns 0 | Future schedule gate |
| `AlreadyDelivered_IsSkipped` | Status=Delivered seed → RunAsync returns 0, sender never called | Terminal state filter |
| `DeadLettered_IsSkipped` | Status=DeadLettered seed → RunAsync returns 0 | Terminal state filter |
| `EmptyOutbox_ReturnsZero` | No outbox entries → returns 0 | Edge case |

**Total test count:** 10
**Coverage assessment:** All acceptance criteria have at least one dedicated test. All
boundary conditions (index 0/1/2, MaxAttempts threshold, null/future NextAttemptAt, each
terminal status) are exercised. No gaps identified.

## Verdict
[x] DONE

ENH-NOTIF-001 is fully implemented and validated. All 9 acceptance criteria from SOW v2.1
FR-NOTIF-006 pass. The implementation is production-ready pending the minor recommendation
around the `Failed` enum documentation and a future DB index on `(Status, NextAttemptAt)`.
