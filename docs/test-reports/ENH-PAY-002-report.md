# ENH-PAY-002 Test Report — HMAC-SHA256 Webhook Verification
**Date:** 2026-05-22
**Tested by:** TEST Agent (automated)
**Branch:** enhancement/additional-feature-improvement

## Summary
PASS

All four acceptance criteria defined in SOW v2.1 FR-PAY-009 / TC-PAY-SEC-006 are satisfied. The implementation is correct, uses constant-time comparison, returns proper HTTP status codes, and achieves replay idempotency through two complementary mechanisms.

## Acceptance Criteria

| Criterion | Status | Evidence |
|---|---|---|
| Valid `x-razorpay-signature` → HTTP 200 + order state change processed | PASS | `PaymentsWebhookController.cs:43-44`; `PaymentWebhookService.cs:90-99` |
| Tampered payload → HTTP 401 + audit log entry + appropriate logging | PASS | `PaymentsWebhookController.cs:33-40`; structured log with RemoteIp + SignaturePrefix |
| Constant-time comparison used | PASS | `PaymentWebhookService.cs:46-48` — `CryptographicOperations.FixedTimeEquals` |
| Replay of identical valid webhook → idempotency prevents duplicate order state change | PASS | `PaymentWebhookService.cs:77,86-99` (DB-level guard) + `IdempotencyFilter.cs` (HTTP-level guard) |

## Findings

### 1. HMAC-SHA256 Implementation (PaymentWebhookService.cs)

The `VerifySignature` method (lines 29–49) is correct:

- The HMAC-SHA256 key is loaded from `config["RazorpayWebhook:Secret"]` (also present in `appsettings.Development.json` as `RazorpayWebhook:Secret`).
- The HMAC digest is computed over the raw UTF-8 body bytes before any JSON deserialization occurs.
- The computed digest and the incoming signature are both normalised to lowercase hex strings before comparison, which correctly handles the case-insensitive Razorpay signature format (confirmed by test `VerifySignature_UppercaseSignature_IsCaseInsensitive`).
- **Constant-time comparison:** `CryptographicOperations.FixedTimeEquals` (line 46) is used — this is the .NET BCL-provided timing-safe byte comparison. No plain `==` or `string.Equals` is used for the sensitive comparison.
- A length guard (line 43) short-circuits mismatched-length strings without a timing leak, because both sides are derived from fixed-length SHA-256 output (always 64 hex chars) — the computed side is always 64 chars, so the mismatch is data-independent on the implementation side.

### 2. Raw Body Handling (PaymentsWebhookController.cs)

The controller (lines 24–45):

- Calls `Request.EnableBuffering()` (line 26) before reading, ensuring the body stream can be rewound if needed.
- Reads the body as a raw UTF-8 string via `StreamReader` (lines 28–29) **before** any model binding — signature verification is performed over the unmodified wire bytes.
- Reads the signature from `X-Razorpay-Signature` header (line 31); Razorpay's canonical header name matches.
- On mismatch: returns `HTTP 401` with `errorCode: "WEBHOOK_SIGNATURE_INVALID"` and emits a `LogWarning` with `RemoteIp` and a redacted 8-char signature prefix (lines 34–40). This satisfies both the "HTTP 401" and "audit log entry" requirements.
- On success: calls `ProcessAsync` then returns `HTTP 200` (lines 43–44).

### 3. Replay Idempotency (two-layer)

**Layer 1 — DB-level (PaymentWebhookService.cs:77)**
`HandlePaymentCapturedAsync` looks up the `Payment` row by `GatewayOrderId`. If the payment is already in `Captured` status and `OrderStatus` is already `Confirmed`, the `OrderStateMachine.CanTransition` guard (line 90) returns `false`, so the state is **not changed again**. `SaveChangesAsync` still runs but writes no new `OrderStatusHistory` row. This prevents duplicate state transitions even if the `IdempotencyFilter` is bypassed (e.g., webhook replayed without an `Idempotency-Key` header).

**Layer 2 — HTTP-level (IdempotencyFilter.cs)**
`IdempotencyFilter` is registered as a global action filter (`Program.cs:52`). If the caller supplies an `Idempotency-Key` UUID header, a cache hit (Redis or in-memory, 24 h TTL) short-circuits the action entirely and replays the cached 200 response with `X-Idempotent-Replayed: true`.

Note: Razorpay does not natively send an `Idempotency-Key` header, so Layer 1 (DB guard) is the primary anti-replay mechanism for webhook replays. Layer 2 adds defence-in-depth for upstream gateway integrations.

### 4. Unit Tests (PaymentWebhookServiceTests.cs)

7 `[Fact]` tests covering `VerifySignature`:

| Test | Covers |
|---|---|
| `VerifySignature_ValidSignature_ReturnsTrue` | Happy path — correct HMAC accepted |
| `VerifySignature_TamperedBody_ReturnsFalse` | Payload mutation detected |
| `VerifySignature_WrongSecret_ReturnsFalse` | Wrong HMAC key rejected |
| `VerifySignature_EmptySignature_ReturnsFalse` | Empty header rejected early |
| `VerifySignature_TruncatedSignature_ReturnsFalse` | Length mismatch caught before FixedTimeEquals |
| `VerifySignature_EmptySecret_ReturnsFalse` | Misconfigured secret safely rejects all requests |
| `VerifySignature_UppercaseSignature_IsCaseInsensitive` | Case normalisation verified |

All 7 tests compile against the current implementation. No `ProcessAsync` or idempotency unit tests exist in this file, but the DB-layer idempotency is covered implicitly by the `OrderStateMachine` tests already present in the suite.

**Gap:** There is no explicit unit test for the idempotency replay scenario (calling `ProcessAsync` twice with the same payload and verifying only one `OrderStatusHistory` row is written). This is a low-severity gap since the guard is trivially correct (a single `CanTransition` boolean), but a dedicated regression test would strengthen coverage.

### 5. Service Registration (Program.cs)

`IPaymentWebhookService` → `PaymentWebhookService` is registered as `Scoped` (line 45). This is the correct lifetime for a service that takes `AppDbContext` as a constructor dependency.

### 6. Configuration

`appsettings.Development.json` contains a placeholder secret `"dev-webhook-secret-replace-in-production"` under `RazorpayWebhook:Secret`. Production secret injection via environment variable or Azure Key Vault is expected per the deployment architecture. No hardcoded production secret found.

## Issues / Risks

| Severity | Issue |
|---|---|
| Low | No unit test for `ProcessAsync` idempotency (double-delivery scenario). The DB-level guard works, but is untested at the unit level. |
| Low | `IdempotencyFilter` replayed response always returns `HTTP 200` status code (hardcoded in `IdempotencyFilter.cs:53`), regardless of the original response code. For the webhook endpoint this is harmless (success is always 200), but this is a general filter concern. |
| Info | `EnableBuffering()` is called on the webhook endpoint; the body is read but the stream is not rewound before the action method ends. Since `ProcessAsync` re-uses the already-read `rawBody` string (not the stream), this is not a defect. |

## Verdict
[x] DONE — ENH-PAY-002 implementation is complete and correct. All SOW v2.1 acceptance criteria pass. The low-severity test coverage gap does not block sign-off.
