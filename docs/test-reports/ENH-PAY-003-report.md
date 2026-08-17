# ENH-PAY-003 Test Report — Idempotency-Key Header
**Date:** 2026-05-22
**Tested by:** TEST Agent (automated)
**Branch:** enhancement/additional-feature-improvement

## Summary
PASS

All six acceptance criteria from SOW v2.1 FR-PAY-012 / TE-005 are satisfied. The `IdempotencyFilter` implementation is correct and complete. All five unit tests in `IdempotencyFilterTests.cs` cover the required scenarios, and the filter is registered as a global action filter in `Program.cs`.

---

## Acceptance Criteria

| Criterion | Status | Evidence |
|---|---|---|
| 24h TTL in cache options | PASS | `IdempotencyFilter.cs:18-19` — `AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)` |
| Duplicate key → cached response (no second order created) | PASS | `IdempotencyFilter.cs:41-56` — cache hit short-circuits `next()` and returns stored body |
| X-Idempotent-Replayed: true on replay | PASS | `IdempotencyFilter.cs:48` — `context.HttpContext.Response.Headers[ReplayHeader] = "true"` |
| Invalid UUID → HTTP 400 | PASS | `IdempotencyFilter.cs:33-36` — `Guid.TryParse` failure yields `BadRequestObjectResult` with `errorCode = "INVALID_IDEMPOTENCY_KEY"` |
| Missing key → passes through | PASS | `IdempotencyFilter.cs:26-29` — `if (rawKey is null) { await next(); return; }` |
| Server error (5xx) → NOT cached | PASS | `IdempotencyFilter.cs:61` — cache write guarded by `StatusCode: >= 200 and < 500` pattern match |

---

## Findings

### IdempotencyFilter.cs
- **24h TTL**: Defined as a `static readonly` field `CacheOptions` with `AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)`. Correct.
- **Cache key format**: `idempotency:{keyGuid:N}` (no-hyphen 32-char hex). The `Guid.TryParse` step normalises any standard GUID format (N, D, B, P) into the same canonical key, so alternate-format UUIDs correctly hit the same cache slot — verified by test `UuidInAlternateFormat_TreatedAsTheSameKey`.
- **Replay header**: `X-Idempotent-Replayed: true` is set on `HttpContext.Response.Headers` before returning the `ContentResult`. Correct.
- **Status replay**: Cache-hit responses are always replayed with HTTP 200 (`StatusCode = StatusCodes.Status200OK`). This means the original status code (e.g. 201 Created on first call) is **not** preserved in the replay — the replay always returns 200. This is a minor deviation from RFC 8792 guidance (which suggests preserving the original status) but does not violate any SOW criterion as written. Flagged as a low-severity observation only.
- **5xx not cached**: The filter uses `ObjectResult { StatusCode: >= 200 and < 500 }` — any result with a null `StatusCode` (such as `OkResult` without a body) is also not cached since it won't match `ObjectResult`. This is safe and intentional.
- **No raw SQL / external side effects**: Filter uses only `IDistributedCache`, consistent with the no-raw-SQL rule.
- **CancellationToken**: Both `cache.GetAsync` and `cache.SetAsync` pass `context.HttpContext.RequestAborted`. Correct cancellation propagation.

### Program.cs Registration
- `IdempotencyFilter` is registered as a scoped DI service: `builder.Services.AddScoped<IdempotencyFilter>()` (line 40).
- Registered as a global filter via `o.Filters.AddService<IdempotencyFilter>()` (line 52). This is the correct way to register a DI-resolved filter globally in ASP.NET Core.
- Distributed cache is conditionally wired: Redis if `ConnectionStrings:Redis` is set, otherwise in-memory fallback — appropriate for dev/prod split.

### IdempotencyFilterTests.cs
All tests use `MemoryDistributedCache` (an in-process `IDistributedCache` implementation) — a valid, lightweight stand-in for unit tests. `NullLogger` is used for the logger parameter.

---

## Test Coverage

| Test Method | Scenario | Verdict |
|---|---|---|
| `NoHeader_PassesThrough_DelegateIsCalled` | Missing `Idempotency-Key` header — delegate is called, `context.Result` stays null | Covers AC: missing key passes through |
| `InvalidUuid_ReturnsBadRequest_DelegateNotCalled` | Header value `"not-a-uuid"` — expects `BadRequestObjectResult`, delegate never invoked | Covers AC: invalid UUID → HTTP 400 |
| `ValidKey_FirstCall_DelegateCalledAndResponseCached` | Valid new GUID — delegate called, cache entry written at `idempotency:{key:N}` | Covers AC: first call executes and caches |
| `ValidKey_DuplicateCall_ReturnsCachedBodyWithReplayHeader` | Pre-seeded cache entry — delegate must not be called, `ContentResult` returned with body and `X-Idempotent-Replayed: true` | Covers AC: duplicate key → cached response + replay header |
| `ServerError_ResponseIsNotCached` | Delegate returns `ObjectResult` with `StatusCode = 500` — cache entry must remain null | Covers AC: server error not cached |
| `UuidInAlternateFormat_TreatedAsTheSameKey` | GUID sent in braces format `{...}` — normalised to same cache key, cache hit returned | Extra coverage: UUID format normalisation |

All 6 tests align with the 5 required acceptance criteria (AC 3 "first call caches" and AC 1 "24h TTL" are partially validated structurally by the cache-hit test; TTL value is verified by code inspection since unit tests do not advance time).

---

## Observations / Minor Issues

1. **Replay status code fixed at 200**: The replay path hard-codes `StatusCode = StatusCodes.Status200OK` (`IdempotencyFilter.cs:53`). The original response status (e.g. 201) is not persisted in the cache. Clients relying on the status code of the original response may see 200 instead of 201 on replay. **Impact: low** — no SOW criterion requires status preservation. Recommend storing the original status alongside the body in a future improvement.

2. **`OkResult` (no body) not cached**: When the action returns `OkResult` (void body, no `ObjectResult`), the guard `ObjectResult { StatusCode: >= 200 and < 500 }` will not match, and the response is silently not cached. For POST endpoints returning 201 with a body (`CreatedAtActionResult` extends `ObjectResult`), this is fine. Endpoints returning `NoContentResult` (204) will not be cached — likely intentional.

3. **No test for `Different-Key_ProcessedNormally`**: There is no explicit test asserting that a second, distinct UUID is processed normally (not replayed). This is implicitly covered because the cache miss path calls `next()`, but an explicit test would improve documentation of intent.

---

## Verdict

**[x] DONE** — ENH-PAY-003 implementation passes all SOW v2.1 FR-PAY-012 / TE-005 acceptance criteria. The filter is correctly implemented, registered globally, and backed by five targeted unit tests. The minor observation about 200 vs. original status code on replay is noted but does not block sign-off.
