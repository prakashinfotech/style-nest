# Test Report — ENH-AUTH-009: OTP via MSG91 / Azure Communication Services

**Report date:** 2026-05-21  
**Session type:** Standalone TEST agent (read-only, no production code changes)  
**ENH-ID:** ENH-AUTH-009  
**Overall verdict:** `[!] BLOCKED — 4 of 5 unit-test acceptance criteria FAIL; endpoint not implemented`

---

## 1. Executive Summary

ENH-AUTH-009 is **not ready for sign-off**. The current codebase implements an
**email-based password-reset OTP** (`POST /api/v1/auth/forgot-password`) but does **not**
implement the **phone-based SMS OTP** required by FR-AUTH-001 / BR-AUTH-001 / EC-AUTH-001
/ EC-AUTH-002 / EC-AUTH-012.

| Test type | Tests written | PASS | FAIL | Notes |
|---|---|---|---|---|
| Unit (xUnit) | 5 | 1 | 4 | See §3 |
| Integration (HTTP) | 1 (planned) | — | 1 | Endpoint returns 404 |
| Performance (k6) | 1 (script provided) | — | N/A | Cannot run; no endpoint |

**dotnet test result (StyleNest.Auth.Tests):**  
`Failed! — Failed: 4, Passed: 19 (16 pre-existing + 1 new), Total: 23`

---

## 2. SOW Requirements Validated

| FR/BR/EC ID | Requirement text | Tested? | Result |
|---|---|---|---|
| FR-AUTH-001 | OTP emitted to SMS queue ≤ 200ms p95; response = masked phone + ISO-8601 expiry 300s | Yes | FAIL |
| BR-AUTH-001 | OTP = exactly 6 numeric digits, single-use, invalidated on success or expiry | Partial | 1 PASS, 1 FAIL |
| EC-AUTH-001 | Concurrent OTP in cooldown → HTTP 429 | Yes | FAIL |
| EC-AUTH-002 | Expired OTP submitted → HTTP 410 `AUTH_OTP_EXPIRED` | Yes | FAIL |
| EC-AUTH-012 | Max 5 OTPs/hour per mobile → 6th returns HTTP 429 | Yes | FAIL |

---

## 3. Unit Test Results

**Test file:** `backend/tests/StyleNest.Auth.Tests/OtpSmsAcceptanceTests.cs`  
**Run:** `dotnet test tests/StyleNest.Auth.Tests/StyleNest.Auth.Tests.csproj`

### AC1 — OTP is exactly 6 numeric digits `[BR-AUTH-001]`

**Test:** `GenerateCode_ProducesSixDigitNumericString_Always`  
**Result:** ✅ PASS  
**Evidence:** `OtpService.GenerateCode()` uses `Random.Shared.Next(100000, 999999).ToString()`.
100 iterations all matched regex `^\d{6}$`.

---

### AC2 — Single-use: second verify attempt must fail `[BR-AUTH-001]`

**Test:** `VerifyOtp_AfterSuccessfulVerify_SecondCallMustFail`  
**Result:** ❌ FAIL  
**Assertion error:**
```
Expected second.IsFailure to be True because second verify must fail — OTP is single-use
per BR-AUTH-001; IMPL GAP: VerifyOtpAsync does not mark IsUsed=true on success,
but found False.
```
**Root cause:** `OtpService.VerifyOtpAsync` (line 46–69) returns `Result.Success()` without
setting `otp.IsUsed = true`. The OTP record remains re-usable indefinitely until expiry.
Only `ResetPasswordAsync` (line 72–104) marks IsUsed; the standalone `VerifyOtpAsync`
does not.

**Fix required:** Add `otp.IsUsed = true; await db.SaveChangesAsync();` after the expiry
guard in `VerifyOtpAsync`.

---

### AC3 — Expired OTP → error code `AUTH_OTP_EXPIRED` `[EC-AUTH-002]`

**Test:** `VerifyOtp_ExpiredOtp_MustReturnAuthOtpExpiredErrorCode`  
**Result:** ❌ FAIL  
**Assertion error:**
```
Expected result.Error.Code to be "AUTH_OTP_EXPIRED" with a length of 16 because
EC-AUTH-002: expired OTP must return errorCode AUTH_OTP_EXPIRED
(IMPL GAP: current value is 'OTP.Expired'), but "OTP.Expired" has a length of 11.
```
**Root cause:** `OtpService.VerifyOtpAsync` line 66:
```csharp
return Result.Failure(new Error("OTP.Expired", "OTP has expired."));
```
SOW EC-AUTH-002 requires the error code to be `AUTH_OTP_EXPIRED`.  
Additionally, the HTTP controller maps this `Result.Failure` to HTTP 400 (`BadRequest`),
not HTTP 410 (`Gone`) as required.

**Fix required (two changes):**
1. Error code: `"OTP.Expired"` → `"AUTH_OTP_EXPIRED"`
2. Controller mapping: map `AUTH_OTP_EXPIRED` to HTTP 410

---

### AC4 — OTP expiry = 300s `[FR-AUTH-001]`

**Test:** `SendOtp_OtpExpiryMustBe300Seconds_NotFifteenMinutes`  
**Result:** ❌ FAIL  
**Assertion error:**
```
Expected otp!.ExpiresAt to be before <2026-05-21 06:46:32> because FR-AUTH-001 requires
OTP expiry = now + 300s; IMPL GAP: OtpService uses AddMinutes(15) = 900s,
but found <2026-05-21 06:56:23>.
```
**Root cause:** `OtpService.SendForgotPasswordOtpAsync` line 35:
```csharp
ExpiresAt = DateTime.UtcNow.AddMinutes(15),
```
FR-AUTH-001 states: "ISO-8601 expiry 300s in future". Current value is 900s (15 minutes).

**Fix required:** Change `AddMinutes(15)` to `AddSeconds(300)`.

---

### AC5 — 6th OTP within 1h → rate-limit failure `[EC-AUTH-012]`

**Test:** `SendOtp_SixthRequestWithinOneHour_MustReturnRateLimitFailure`  
**Result:** ❌ FAIL  
**Assertion error:**
```
Expected sixth.IsFailure to be True because 6th OTP within 1h must fail with rate-limit
error per EC-AUTH-012; IMPL GAP: OtpService.SendForgotPasswordOtpAsync has no
rate-limit check, but found False.
```
**Root cause:** `OtpService` has zero rate-limiting logic. The 6th (and any subsequent)
request within 1 hour succeeds unconditionally.

**Fix required:** Before creating a new OTP, query `OtpCodes` for the same identity
within a 1-hour rolling window. If count ≥ 5, return `Result.Failure(new Error(
"OTP.RateLimitExceeded", "Too many OTP requests."))` and map to HTTP 429.

---

## 4. Integration Test — POST /api/v1/auth/otp/send

**Status:** ❌ CANNOT RUN — endpoint does not exist

`OtpController` exposes:
- `POST /api/v1/auth/forgot-password`
- `POST /api/v1/auth/verify-otp`
- `POST /api/v1/auth/reset-password`

**Missing:** `POST /api/v1/auth/otp/send` (phone-based SMS OTP send) is not defined in
any controller. A request to this path returns HTTP 404.

**Additional gaps:**
- `OtpCode` entity has no `PhoneNumber` field — cannot store phone-based OTP
- `IOtpDeliveryChannel.DeliverAsync` signature uses `email`, not `phoneNumber`
- `AzureCommunicationOtpDeliveryChannel` sends email via ACS Email, not SMS via ACS SMS
- No `ISmsDeliveryChannel` or MSG91 integration exists
- Response object does not include `maskedPhone` field in any DTO

**Planned integration test** (for implementation sign-off — file: `OtpSendEndpoint_Integration_Tests.cs`):
```csharp
// POST /api/v1/auth/otp/send { phoneNumber: "+919876543210" }
// → HTTP 200
// → body.maskedPhone matches regex: ^\+91-XXX-XXX-\d{4}$
// → body.expiresAt is ISO-8601, approximately now + 300s
// → OtpCodes table has 1 row with ExpiresAt ≈ now + 300s

// POST /api/v1/auth/verify-otp { phone: "+919876543210", code: "<from DB>" }
// → HTTP 200

// POST /api/v1/auth/verify-otp (same code again)
// → HTTP 410 { errorCode: "AUTH_OTP_EXPIRED" }

// Send OTP 6 times for same phone within 1h
// → 6th POST /api/v1/auth/otp/send → HTTP 429
```

---

## 5. Performance Test — k6

**Status:** ❌ CANNOT RUN — target endpoint does not exist

**Script:** `docs/test-reports/enh-auth-009-k6-perf.js`  
**Target:** 100 req/s × 30s → `http_req_duration p(95) ≤ 200ms`

Script is ready and will execute once `POST /api/v1/auth/otp/send` is implemented.  
The script excludes SMS gateway RTT by measuring `http_req_duration` (server-side only).

---

## 6. Masked Phone Regex Validation

**Pass criterion:** Response `maskedPhone` field must match `\+91-XXX-XXX-\d{4}`

**Status:** NOT IMPLEMENTED  
Example valid values: `+91-XXX-XXX-3210`, `+91-XXX-XXX-9999`  
No DTO field, no masking logic, no phone number parsing exists in codebase.

---

## 7. Implementation Gap Summary

| Gap ID | Component | Description | Impact |
|---|---|---|---|
| G1 | `OtpController` | Missing `POST /api/v1/auth/otp/send` endpoint | Blocks all phone OTP flows |
| G2 | `OtpCode` entity | No `PhoneNumber` field | Cannot store phone-based OTP |
| G3 | `IOtpService` | No `SendPhoneOtpAsync` or equivalent method | No phone OTP entry point |
| G4 | `IOtpDeliveryChannel` | Interface is email-only, no SMS channel | Cannot deliver to phone |
| G5 | ACS integration | `AzureCommunicationOtpDeliveryChannel` sends email, not SMS | Wrong transport |
| G6 | `OtpService.VerifyOtpAsync` | Doesn't set `IsUsed=true` on success | AC2 FAIL |
| G7 | Error code | `"OTP.Expired"` ≠ `"AUTH_OTP_EXPIRED"` | AC3 FAIL |
| G8 | HTTP status code | Expired/used OTP → 400 not 410 | EC-AUTH-002 FAIL |
| G9 | OTP TTL | 15 min (900s) ≠ required 300s | AC4 FAIL |
| G10 | Rate limiting | No per-identity OTP throttle | AC5 FAIL |
| G11 | Masked phone | No `maskedPhone` in DTO or response | FR-AUTH-001 FAIL |

---

## 8. Pre-existing Tests Status

All 16 pre-existing `OtpServiceTests` tests continue to pass (email-based password reset
flows are unaffected by this test session).

```
Passed! — Failed: 0, Passed: 16 — (baseline before ENH-AUTH-009 tests)
```

---

## 9. Sign-off Criteria Checklist

| Criterion | Status |
|---|---|
| k6 p95 ≤ 200ms server-side (http_req_duration, excl. SMS RTT) | ❌ BLOCKED — endpoint missing |
| Masked phone matches `\+91-XXX-XXX-\d{4}` | ❌ BLOCKED — not implemented |
| OTP = exactly 6 numeric digits | ✅ PASS |
| Single-use: second verify → HTTP 410 | ❌ FAIL — `IsUsed` not set on success |
| 6th OTP in 1h window → HTTP 429 | ❌ FAIL — no rate limiting |
| Expired OTP → HTTP 410 `AUTH_OTP_EXPIRED` | ❌ FAIL — code "OTP.Expired" + HTTP 400 |

---

## 10. Recommended Implementation Order (for IMPL agent)

1. Add `PhoneNumber` field to `OtpCode` entity + EF migration
2. Add `ISmsDeliveryChannel` interface (separate from `IOtpDeliveryChannel` email)
3. Implement SMS delivery via MSG91 or ACS SMS (not email)
4. Add `SendPhoneOtpAsync(phoneNumber)` to `IOtpService` and `OtpService`
   - Generate 6-digit code
   - Rate-check: count OtpCodes for same phone in last 1h; if ≥5 → 429
   - Set `ExpiresAt = DateTime.UtcNow.AddSeconds(300)` (not 15 min)
   - Persist and deliver via SMS channel
   - Return masked phone (`+91-XXX-XXX-{last4}`) + ISO-8601 expiry
5. Fix `VerifyOtpAsync`: set `IsUsed = true` on successful verification
6. Fix error code: `"OTP.Expired"` → `"AUTH_OTP_EXPIRED"`, map to HTTP 410
7. Add `POST /api/v1/auth/otp/send` endpoint to `OtpController`
8. Re-run this test suite — all 5 unit tests should pass
9. Run integration tests and k6 script

---

*Test agent: Claude Sonnet 4.6 | Session: ENH-AUTH-009-TEST | 2026-05-21*
