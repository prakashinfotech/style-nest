# TEST-AGENT-PROMPTS.md — Parallel Test Agent Prompt Library
# ECM-TSTYLENEST-2026-001 | Source: docs/FEATURE-ENHANCEMENTS.md (P0 + Parallel-testable: YES)

> **Usage:** Each block below is a self-contained prompt for a standalone TEST agent session.
> Launch in a NEW Claude Code session — never in the same session as the IMPL agent.
> Test output → `docs/test-reports/<ENH-ID>-report.md`
> Status update → mark `[x]` or `[!]` in `docs/FEATURE-ENHANCEMENTS.md` after sign-off.
>
> **Test tooling:** xUnit (.NET unit/integration) · Playwright (E2E) · k6 (load/perf) · axe-core (a11y) · OWASP ZAP (security)
> **NFR thresholds** are exact values from SOW v2.1 Section 4 — do not round or adjust.

---

## Quick-Reference Index

| ENH-ID | Feature | Domain | Phase |
|---|---|---|---|
| ENH-AUTH-003 | Account Merge | AUTH | P1 |
| ENH-AUTH-004 | Multi-device Session Management | AUTH | P1 |
| ENH-AUTH-005 | Account Lockout Exponential Doubling | AUTH | P1 |
| ENH-AUTH-007 | JWT Key Loading Resilience | AUTH | P1 |
| ENH-AUTH-008 | Interceptor Single-Flight Refresh | AUTH | P1 |
| ENH-AUTH-009 | OTP via MSG91 / ACS | AUTH | P1 |
| ENH-AUTH-011 | Right-to-Erasure / PDPB | AUTH | P6 |
| ENH-AUTH-012 | MFA for Admin / SuperAdmin | AUTH | P1 |
| ENH-CAT-002 | Flash Sale Module | CATALOG | P5 |
| ENH-CAT-006 | Azure Cognitive Search | CATALOG | P2 |
| ENH-CAT-007 | SEO Canonicalisation | CATALOG | P2 |
| ENH-PDP-001 | Pincode Delivery Estimate | PDP | P2 |
| ENH-PDP-002 | EMI Calculator | PDP | P2 |
| ENH-PDP-005 | Related Products Rails | PDP | P5 |
| ENH-SRCH-002 | Search Autocomplete + Typeahead | SEARCH | P2 |
| ENH-CART-001 | Optimistic UI with NgRx Rollback | CART | P2 |
| ENH-CART-002 | Coupon Validation Error Codes | CART | P2 |
| ENH-CART-003 | Inventory Re-validation at Checkout | CART | P2 |
| ENH-CART-004 | Last-Unit Row-Level Lock | CART | P3 |
| ENH-CHKOUT-001 | Email Verification Gate >₹5,000 | CHECKOUT | P3 |
| ENH-CHKOUT-003 | COD Ceiling Enforcement | CHECKOUT | P3 |
| ENH-PAY-001 | PayU Failover | PAYMENTS | P3 |
| ENH-PAY-002 | HMAC-SHA256 Webhook Verification | PAYMENTS | P3 |
| ENH-PAY-003 | Idempotency-Key Header | PAYMENTS | P3 |
| ENH-PAY-005 | Bank Timeout Reconciliation | PAYMENTS | P3 |
| ENH-PAY-006 | Razorpay Vault Tokenisation | PAYMENTS | P6 |
| ENH-PAY-007 | StyleNest Cash Pessimistic Lock | PAYMENTS | P5 |
| ENH-ORD-001 | State Machine CHECK Constraints | ORDERS | P3 |
| ENH-ORD-002 | Concurrent State Transition Protection | ORDERS | P3 |
| ENH-PROMO-001 | StyleNest Cash Earn on Purchase | PROMOTIONS | P5 |
| ENH-PROMO-003 | Flash Sale Price Lock | PROMOTIONS | P5 |
| ENH-NOTIF-001 | Exponential Backoff Retry + DLQ | NOTIFICATIONS | P4 |
| ENH-NOTIF-002 | FCM Push Notifications | NOTIFICATIONS | P4 |
| ENH-NOTIF-004 | Email OTP via Hangfire + MailKit | NOTIFICATIONS | P1 |
| ENH-ADMIN-001 | AuditLogs Schema + Retention | ADMIN | P4 |
| ENH-SELL-001 | Multi-Tenant Row-Level Security | SELLER | P4 |
| ENH-AI-001 | Personalised Product Feed | AI | P5 |
| ENH-AI-002 | Personalised Feed Fallback | AI | P5 |
| ENH-AI-004 | AI-Powered Related Products (FBT) | AI | P5 |
| ENH-INFRA-006 | Blue-Green Deployment + Auto-Rollback | INFRA | P7 |
| ENH-INFRA-008 | k6 Spike + Soak Load Tests | INFRA | P6 |
| ENH-INFRA-009 | OWASP ZAP Automated CI Scan | INFRA | P6 |
| ENH-INFRA-010 | axe-core Accessibility CI Gate | INFRA | P6 |

---

## AUTH

---

## TEST AGENT: ENH-AUTH-003 — Account Merge (Social Email Matches Verified Account)

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-AUTH-009, BR-AUTH-007, EC-AUTH-003, EC-AUTH-010
**Stack under test:** .NET Core 10 API (xUnit) · Playwright E2E

### Acceptance Criteria to Validate
- Social login email exactly matches a verified existing email/password account → merge prompt issued (not auto-merge); user must confirm with current-password challenge (EC-AUTH-010)
- On confirmed merge: identities linked, order history + wishlist + StyleNest Cash preserved, older `userId` retained, newer identity archived (BR-AUTH-007)
- Social email matches different-phone account → merge prompt (not silent merge) (EC-AUTH-003)
- Audit log entry written within 1s of merge (BR-AUTH-008)

### Test Types to Execute
- **Unit:** `AccountMergeService` — mock social provider returning email matching seeded user; assert merge-prompt response vs auto-merge
- **Integration:** `POST /api/v1/auth/social/callback` with social token whose email matches `user01@mailinator.com` → HTTP 200 `{ action: "MERGE_REQUIRED" }`; then `POST /api/v1/auth/merge/confirm` with password → assert single merged account in DB
- **E2E (Playwright):** Google OAuth stub → matching email → merge prompt page renders → password entered → dashboard with preserved wishlist items

### Pass Criteria
- `MERGE_REQUIRED` response on unconfirmed match; auto-merge never occurs without challenge
- Post-merge: older userId persists; newer OAuth identity record has `ArchivedAt` set; order count unchanged
- Audit row in `AuditLogs` with `action = "ACCOUNT_MERGE"` within 1s
- EC-AUTH-003: different-phone account → merge prompt shown, not blocked

### Output
- Test report → `docs/test-reports/ENH-AUTH-003-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md` (set `[x]` DONE or `[!]` BLOCKED)

---

## TEST AGENT: ENH-AUTH-004 — Multi-device Session Management

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-AUTH-008
**Stack under test:** .NET Core 10 API (xUnit) · Playwright E2E

### Acceptance Criteria to Validate
- `GET /api/v1/auth/sessions` returns all active sessions for authenticated user (device name, last-seen, IP)
- `DELETE /api/v1/auth/sessions/{sessionId}` revokes target session within 2s p99; that device gets HTTP 401 on next API call
- Other active sessions are unaffected by targeted revocation

### Test Types to Execute
- **Unit:** `SessionService.RevokeSessionAsync` — assert session token invalidated in Redis within 2s
- **Integration:** login from 3 test clients → `GET /sessions` returns 3; `DELETE /sessions/{id2}` → `GET /sessions` returns 2; client-2 calls any endpoint → HTTP 401
- **E2E (Playwright):** two browser contexts simulate two devices; revoke second from first → second context receives 401 on next request

### Pass Criteria
- Revocation latency p99 ≤ 2s (measured from `DELETE` response to 401 on next call)
- `GET /sessions` response excludes revoked session immediately
- Sessions of client-1 and client-3 return valid 200 after client-2 revoked

### Output
- Test report → `docs/test-reports/ENH-AUTH-004-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-AUTH-005 — Account Lockout Exponential Doubling

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-AUTH-011, EC-AUTH-009
**Stack under test:** .NET Core 10 API (xUnit) · Playwright E2E

### Acceptance Criteria to Validate
- 5 failed logins within 15min → 30min lockout; 6th attempt returns HTTP 423 with `lockoutDurationSeconds: 1800`
- Subsequent lockout after unlock → doubles (1800s → 3600s → … max 86400s / 24h)
- Successful login resets failure counter to 0
- Unlock email sent with password-reset link valid exactly 60min

### Test Types to Execute
- **Unit:** `LockoutService` — 5-failure timer logic; doubling up to 86400s max; reset on success
- **Integration:** `POST /api/v1/auth/login` with wrong password × 6 using `user02@mailinator.com` → 6th returns 423 `AUTH_ACCOUNT_LOCKED`; correct password after unlock → 200; failure counter = 0 in DB
- **E2E (Playwright):** repeated wrong passwords → lockout banner appears; wait timer display

### Pass Criteria
- 6th attempt: HTTP 423 + `lockoutDurationSeconds: 1800`
- After first unlock + 5 more failures: `lockoutDurationSeconds: 3600`
- Max lockout never exceeds 86400s
- Reset link in email expires at exactly `issuedAt + 3600s`

### Output
- Test report → `docs/test-reports/ENH-AUTH-005-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-AUTH-007 — JWT Public Key Loading Resilience

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-AUTH-006, TSD §5.3 TE-003
**Stack under test:** .NET Core 10 API (xUnit)

### Acceptance Criteria to Validate
- Key Vault outage (HTTP 503) at pod startup → Polly retry succeeds within configured retry window; pod starts
- Public key cached in-memory; subsequent token validations do not call Key Vault
- 15min cache refresh: after key rotation Key Vault returns new key; next refresh cycle picks it up
- `/health` endpoint returns HTTP 200 even when Key Vault is unreachable (liveness not blocked by KV)

### Test Types to Execute
- **Unit:** `KeyVaultJwtKeyProvider` — mock KV returning 503 × 2 then 200 → assert key loaded; mock KV permanently unavailable → pod uses cached key for ≤15min
- **Integration:** startup integration test with KV stub returning 503 for first 2 calls → pod ready within 30s; tokens signed with cached key validate correctly
- **Performance:** token validation duration — assert p95 < 5ms (no KV call on hot path; TC-PAY-SEC-006 pattern)

### Pass Criteria
- Pod starts despite KV returning 503 × 2 then 200
- `/health` returns 200 with KV permanently unavailable (cached key sufficient)
- Token validation p95 < 5ms over 1000 consecutive validations
- Cache refreshes with new key within 15min of rotation

### Output
- Test report → `docs/test-reports/ENH-AUTH-007-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-AUTH-008 — Angular HTTP Interceptor Single-Flight Refresh

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → TSD §8.4 TE-007, SOW v2.1 FR-AUTH-007
**Stack under test:** Angular 21 (Jasmine/Jest unit) · Playwright E2E

### Acceptance Criteria to Validate
- 3 concurrent API calls with expired access token → exactly 1 `POST /auth/refresh` issued (not 3)
- All 3 concurrent requests retry with the new access token and succeed
- No token-family revocation triggered (no spurious logout from reuse detection)

### Test Types to Execute
- **Unit:** `auth.interceptor.spec.ts` — mock HttpClient; fire 3 concurrent requests returning 401 simultaneously; assert `authApi.refresh()` called exactly once via spy; assert all 3 requests retry with new token
- **E2E (Playwright):** expire access token (set short TTL in test config); navigate to page that fires 3 parallel API calls; intercept network → assert single `/auth/refresh` request; all 3 data calls resolve with 200

### Pass Criteria
- `authApi.refresh` spy call count = 1 regardless of concurrent 401 count
- All retried requests resolve 200 (not 401)
- No `POST /auth/logout` or 401 on dashboard after concurrent refresh
- `refreshInFlight$` resets to `null` after all requests complete (`finalize` operator)

### Output
- Test report → `docs/test-reports/ENH-AUTH-008-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-AUTH-009 — OTP via MSG91 / Azure Communication Services

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-AUTH-001, BR-AUTH-001, EC-AUTH-001, EC-AUTH-002, EC-AUTH-012
**Stack under test:** .NET Core 10 API (xUnit) · k6 (performance)

### Acceptance Criteria to Validate
- `POST /api/v1/auth/otp/send` → OTP emitted to SMS provider queue within **200ms p95** server-side (SOW NFR implied by FR-AUTH-001)
- Response: masked phone (+91-XXX-XXX-NNNN), ISO-8601 expiry 300s in future, HTTP 200
- OTP exactly 6 numeric digits; single-use; invalidated on success or expiry
- Max 5 OTPs/hour per mobile → 6th returns HTTP 429 (EC-AUTH-012)
- Expired OTP submitted → HTTP 410 `AUTH_OTP_EXPIRED` (EC-AUTH-002)

### Test Types to Execute
- **Unit:** `OtpService` — mock SMS provider; assert 6-digit numeric format; single-use invalidation; 5th within 60min succeeds, 6th returns 429
- **Integration:** `POST /api/v1/auth/otp/send` → assert OTP row in `OtpCodes` table with `ExpiresAt = now + 300s`; `POST /api/v1/auth/verify-otp` with correct OTP → 200; retry → 410
- **Performance (k6):** 100 req/s concurrent OTP send → p95 ≤ 200ms server response time (exclude SMS gateway RTT)

### Pass Criteria
- p95 ≤ 200ms server-side (k6 `http_req_duration` excluding SMS delivery)
- Masked phone format matches `+91-XXX-XXX-NNNN` regex
- OTP invalidated after first use (second verify → 410)
- 6th OTP in 1h window → HTTP 429

### Output
- Test report → `docs/test-reports/ENH-AUTH-009-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-AUTH-011 — Right-to-Erasure / PDPB Compliance

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-SEC-006, TC-AUTH-FUNC-031
**Stack under test:** .NET Core 10 API (xUnit) · Playwright E2E

### Acceptance Criteria to Validate
- `DELETE /api/v1/user/me` (authenticated): (a) anonymise PII in Users table, (b) preserve Orders + Payments with anonymised refs, (c) delete Sessions + Cart + Wishlist + ReviewImages, (d) emit `GDPRErasureEvent` to Service Bus, (e) complete within 30 calendar days per PDPB SLA
- Re-login with erased account credentials → HTTP 401
- Orders remain in DB with `UserId = ANONYMISED_{hash}`

### Test Types to Execute
- **Unit:** `ErasureService` — assert each step (a)–(d) executes; DB assertions per field
- **Integration:** seed test user with 2 orders + wishlist + cart + session; `DELETE /api/v1/user/me`; assert: `Users.Email` = null, `Users.Phone` = null, `Users.FirstName` = "Deleted", Orders preserved with anonymised UserId, Cart rows = 0, Wishlist rows = 0, Service Bus message queued
- **E2E (Playwright):** login → trigger erasure → attempt re-login → 401 returned; account page inaccessible

### Pass Criteria
- HTTP 204 on erasure
- PII fields nulled in Users row (not deleted)
- Orders count unchanged, UserId anonymised
- 0 Cart + Wishlist + Session rows for erased user
- GDPRErasureEvent visible in Service Bus test queue
- Re-login returns HTTP 401

### Output
- Test report → `docs/test-reports/ENH-AUTH-011-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-AUTH-012 — MFA Enforcement for Admin / SuperAdmin

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 BR-AUTH-006, FR-AUTH-012
**Stack under test:** .NET Core 10 API (xUnit) · Playwright E2E

### Acceptance Criteria to Validate
- Admin/SuperAdmin JWT without MFA claim → HTTP 403 on any `/api/v1/admin/*` endpoint
- MFA challenge issued on admin login; TOTP or email code accepted
- After MFA: JWT includes `mfa_verified: true` claim; admin endpoints return 200
- Customer and Seller JWTs unaffected (no MFA gate on their endpoints)

### Test Types to Execute
- **Unit:** `MfaRequirementHandler` — JWT with `roles: ["Admin"]` but no `mfa_verified` → 403; same JWT + `mfa_verified: true` → 200
- **Integration:** login as `admin1@mailinator.com` without MFA step → `GET /api/v1/admin/dashboard` → HTTP 403; complete MFA → new token → same endpoint → 200
- **E2E (Playwright):** admin panel login flow → MFA prompt renders → code entered → dashboard loads

### Pass Criteria
- 100% of admin endpoints return 403 without `mfa_verified: true` claim
- MFA step completes in under 30s user interaction time
- Customer JWT (`roles: ["Customer"]`) passes customer endpoints with no MFA gate
- MFA failure (wrong code) returns 401 and does not issue admin token

### Output
- Test report → `docs/test-reports/ENH-AUTH-012-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## CATALOG

---

## TEST AGENT: ENH-CAT-002 — Flash Sale Module

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-HOME-005, BR-HOME-001, EC-HOME-003
**Stack under test:** .NET Core 10 API (xUnit) · Playwright E2E

### Acceptance Criteria to Validate
- Server-driven countdown renders accurately; sold-out transition disables ATC immediately on stock = 0
- Negative countdown (sale ended) → UI auto-hides within 1s (EC-HOME-003)
- No carousel auto-advance in first 2s (BR-HOME-001)
- Flash sale price locked at sale value even if product base price changes mid-sale

### Test Types to Execute
- **Unit:** `FlashSaleService.GetActiveSale` — sale in future returns countdown; sale expired returns null
- **Integration:** `GET /api/v1/cms/flash-sale` with active sale → returns `{ endsAt, products[], salePrice }`; with ended sale → 404 or empty; p95 < 100ms (cache hit)
- **E2E (Playwright):** flash sale page → countdown ticks; manipulate system time past sale end → UI component hides (or set `endsAt` to 2s from now in test DB)

### Pass Criteria
- Countdown accurate ±1s; sold-out flag flips ATC to disabled
- Negative `endsAt` → component hidden within 1s of detecting negative value
- Cache-hit response p95 < 100ms
- Sale price shown correctly even if base price updated during sale

### Output
- Test report → `docs/test-reports/ENH-CAT-002-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-CAT-006 — Azure Cognitive Search

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-CAT-003, FR-SRCH-001..010, NFR-PERF-005, TSD §7.1
**Stack under test:** .NET Core 10 API (xUnit) · k6 (performance)

### Acceptance Criteria to Validate
- Multi-select facets: AND-across-facets, OR-within-facet; results refresh ≤500ms (FR-CAT-003)
- Autocomplete suggestions returned; synonyms resolve (e.g. "sneaker" → "trainer")
- Zero-result query returns empty-state, not HTTP error
- Full-text search returns ranked results with highlights

### Test Types to Execute
- **Unit:** `SearchService.MapFiltersToAzureQuery` — assert correct `$filter` syntax for AND/OR facet combinations
- **Integration:** `GET /api/v1/search?q=shoes&facets=brand:Nike,Adidas&facets=size:42` → Cognitive Search request logged with correct OData filter; response includes facet counts
- **Performance (k6):** 500 vUsers · `GET /api/v1/search?q=kurta` → p95 ≤ **200ms** (NFR-PERF-005)

### Pass Criteria
- p95 ≤ 200ms at 500 vUsers
- Facet counts reflect AND-across/OR-within logic (not pre-filter total)
- Synonym "sneaker" returns results including "trainer" tagged products
- Zero-result → HTTP 200 `{ results: [], total: 0 }` (not 404/500)

### Output
- Test report → `docs/test-reports/ENH-CAT-006-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-CAT-007 — SEO Canonicalisation

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-CAT-006, NFR-SEO-001..004
**Stack under test:** Angular 21 SSR (Playwright) · structured-data CLI

### Acceptance Criteria to Validate
- Every PLP and PDP page rendered by Angular Universal SSR contains `<link rel="canonical" href="...">` pointing to the canonical URL
- `<meta name="description">` populated from `SEOTemplates` table (not empty)
- JSON-LD structured data present and validates against Schema.org (Product, BreadcrumbList)

### Test Types to Execute
- **Integration (SSR):** HTTP GET to each P0 route via SSR server → parse HTML response → assert `link[rel=canonical]` present and `href` matches expected URL pattern
- **E2E (Playwright):** `page.locator('link[rel="canonical"]').getAttribute('href')` on `/products/category/women-kurtas` → not null; `page.locator('meta[name="description"]').getAttribute('content')` → length > 10
- **Structured Data:** run `@google/structured-data-testing-tool` CLI against SSR-rendered PDP HTML → 0 errors

### Pass Criteria
- 0 P0 routes missing canonical tag
- 0 P0 routes with empty meta-description
- JSON-LD validates with 0 errors on Product schema
- Category rename: old slug issues 301 to new slug (EC-CAT-003)

### Output
- Test report → `docs/test-reports/ENH-CAT-007-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## PDP

---

## TEST AGENT: ENH-PDP-001 — Pincode Delivery Estimate

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-PDP-003, EC-PDP-004
**Stack under test:** .NET Core 10 API (xUnit) · k6 · Playwright E2E

### Acceptance Criteria to Validate
- Returns serviceability, COD eligibility, ETA, free-delivery threshold, express availability per pincode
- Response ≤ 1s (FR-PDP-003)
- Degraded service → "Enter pincode" prompt + cached defaults shown (EC-PDP-004)
- Test with all 12 seeded pincode types: serviceable, non-serviceable, COD-eligible, COD-blacklisted, express, standard

### Test Types to Execute
- **Unit:** `PincodeService` — 12 seeded pincodes; assert correct flags per type
- **Integration:** `GET /api/v1/delivery/estimate?pincode=400001&productId={id}` → response includes `{ serviceable, codEligible, etaDays, expressAvailable }`; non-serviceable → `{ serviceable: false }` with HTTP 200
- **Performance (k6):** 200 vUsers · p95 ≤ **1000ms**
- **E2E (Playwright):** PDP page → type pincode → ETA appears; type invalid pincode → graceful message

### Pass Criteria
- p95 ≤ 1000ms at 200 vUsers
- Non-serviceable pincode: HTTP 200 `{ serviceable: false }` (not 404)
- COD blacklisted: `codEligible: false`
- Degraded (stub returns 503): fallback cached response shown in UI, no crash

### Output
- Test report → `docs/test-reports/ENH-PDP-001-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-PDP-002 — EMI Calculator

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-PDP-004, BR-PDP-003, TC-PAY-BVA-003
**Stack under test:** .NET Core 10 API (xUnit) · Playwright E2E

### Acceptance Criteria to Validate
- Bank-wise tenures (3/6/9/12/24m) with correct instalment amounts
- No-cost EMI options highlighted with accent-red `#E31837`
- EMI panel hidden for orders below `minOrderForEmi` config value (BR-PDP-003)
- Boundary: order ₹2,999 (below min 3000) → EMI hidden (TC-PAY-BVA-003)

### Test Types to Execute
- **Unit:** `EmiCalculatorService` — assert formula: `instalment = principal × (rate/12) / (1 - (1 + rate/12)^-tenure)`; accurate to ₹1
- **Integration:** `GET /api/v1/payment/emi-options?amount=5000` → ≥1 bank with tenure list; `GET /api/v1/payment/emi-options?amount=2999` → empty list or `{ eligible: false }`
- **E2E (Playwright):** PDP with product priced ₹5,000 → EMI section visible; product priced ₹2,500 → EMI section absent; no-cost EMI entry has `color: rgb(227,24,55)` (accent-red)

### Pass Criteria
- Instalment maths accurate ±₹1 vs reference calculation
- `color` of no-cost EMI = `#E31837` (CSS computed)
- EMI absent for amount < config `minOrderForEmi` (default ₹3,000)
- API p95 ≤ 300ms (NFR-PERF-004 catalog budget)

### Output
- Test report → `docs/test-reports/ENH-PDP-002-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-PDP-005 — Related Products Rails (Similar / Look / FBT)

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-PDP-008, BR-HOME-002
**Stack under test:** .NET Core 10 API (xUnit) · k6 · Playwright E2E

### Acceptance Criteria to Validate
- Three rails: Similar (same category), Complete the Look (style-compatible), FBT (co-purchase)
- OOS variants excluded from all rails (BR-HOME-002)
- Rail renders ≤ 800ms (FR-HOME-003 pattern)

### Test Types to Execute
- **Unit:** `RelatedProductsService` — Similar: same category, exclude current product, exclude OOS; FBT: co-purchase co-occurrence lookup
- **Integration:** `GET /api/v1/products/{id}/related?type=similar` → all items same category as product {id}; `?type=fbt` → items frequently co-purchased; `?type=look` → items with compatible style tag; OOS product never appears in any rail
- **Performance (k6):** 300 vUsers · p95 ≤ **800ms**
- **E2E (Playwright):** PDP renders → three `[data-testid="rail-*"]` elements with ≥1 product card each; `@defer` defers until viewport

### Pass Criteria
- p95 ≤ 800ms at 300 vUsers
- 0 OOS products in any rail
- Similar results: all same `categoryId` as source product
- Defer fires only when rail scrolls into viewport (Lighthouse LCP not impacted)

### Output
- Test report → `docs/test-reports/ENH-PDP-005-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## SEARCH

---

## TEST AGENT: ENH-SRCH-002 — Search Autocomplete + Typeahead

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-SRCH, NFR-PERF-005, TSD §7.1
**Stack under test:** .NET Core 10 API (xUnit) · k6 · Playwright E2E

### Acceptance Criteria to Validate
- Suggestions appear after ≥ 2 characters with 300ms debounce
- p95 ≤ 200ms server response (NFR-PERF-005)
- Exactly one HTTP request fires per debounce window (no rapid-fire)
- Autocomplete list is keyboard-navigable (ARIA `role="listbox"`)

### Test Types to Execute
- **Unit:** `search-autocomplete.component.spec.ts` — assert debounceTime(300); no request emitted for 1-char input; exactly 1 request per debounce window across rapid keystrokes
- **Integration:** `GET /api/v1/search/suggest?q=sho` → array of ≥1 suggestion strings; `?q=x` → empty array (not error)
- **Performance (k6):** 500 vUsers · p95 ≤ **200ms** (NFR-PERF-005)
- **E2E (Playwright):** type "sho" in search bar → `[role="listbox"]` appears within 600ms wall-clock; keyboard ↓↑ navigates; Enter navigates to result

### Pass Criteria
- p95 ≤ 200ms (k6 `http_req_duration`)
- No request before 2 chars
- Exactly 1 request per 300ms debounce window (Playwright network intercept count)
- ARIA `role="listbox"` + `role="option"` present; keyboard navigation functional

### Output
- Test report → `docs/test-reports/ENH-SRCH-002-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## CART

---

## TEST AGENT: ENH-CART-001 — Optimistic UI with NgRx Rollback

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-CART-003
**Stack under test:** Angular 21 (Jasmine unit) · Playwright E2E

### Acceptance Criteria to Validate
- ATC / quantity-update updates NgRx store immediately (optimistic)
- On HTTP 4xx/5xx: NgRx state rolls back to pre-optimistic value; toast "Cart update failed" appears within 500ms of error response
- No duplicate cart entries created

### Test Types to Execute
- **Unit:** `cart.effects.spec.ts` — mock HTTP 500 on `updateQuantity`; assert `CartActions.updateQuantityFailure` dispatched; assert NgRx store reverts to previous quantity
- **E2E (Playwright):** cart page → intercept `PUT /api/v1/cart/items/*` → force HTTP 500 → assert toast `[data-testid="toast-error"]` appears within 500ms → assert displayed quantity unchanged from pre-attempt value

### Pass Criteria
- Toast appears ≤ 500ms from 500 response (measured via Playwright `page.waitForSelector`)
- NgRx `cart.items` quantity = original value after rollback
- No extra cart item row in DOM after rollback
- HTTP 200 → optimistic value persists (no rollback on success)

### Output
- Test report → `docs/test-reports/ENH-CART-001-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-CART-002 — Coupon Validation Detailed Error Codes

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-CART-006
**Stack under test:** .NET Core 10 API (xUnit)

### Acceptance Criteria to Validate
Seven distinct failure modes each return HTTP 400 with specific `errorCode`:
1. `COUPON_NOT_FOUND` — coupon code does not exist or inactive
2. `COUPON_EXPIRED` — outside validity window
3. `COUPON_USER_LIMIT` — user has reached `maxUsesPerUser`
4. `COUPON_GLOBAL_LIMIT` — global `maxUses` exhausted
5. `COUPON_MIN_ORDER` — cart subtotal < `minOrder`
6. `COUPON_INELIGIBLE_PRODUCT` — no eligible items in cart
7. `COUPON_STACKING_CONFLICT` — another coupon already applied and stacking disallowed

### Test Types to Execute
- **Unit:** `CouponValidationService` — 7 test methods, one per failure mode, using seeded coupon fixture data
- **Integration:** `POST /api/v1/cart/coupon` × 7 scenarios → assert HTTP 400 + `{ errorCode: "COUPON_*" }` per case; valid coupon → HTTP 200 + `{ discountAmount }`

### Pass Criteria
- Each of the 7 failure modes returns its documented `errorCode` and no other
- Valid coupon returns HTTP 200 with correct `discountAmount` (not 0)
- Error response shape: `{ errorCode: string, message: string }` (ProblemDetails compatible)

### Output
- Test report → `docs/test-reports/ENH-CART-002-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-CART-003 — Inventory Re-validation at Checkout

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 EC-INV-002, EC-INV-003
**Stack under test:** .NET Core 10 API (xUnit) · Playwright E2E

### Acceptance Criteria to Validate
- Items that go OOS between ATC and checkout are flagged with `"Remove to continue"` message
- Checkout flow blocked until flagged items removed or back in stock
- Non-OOS items in same cart unaffected

### Test Types to Execute
- **Unit:** `CartValidationService.ValidateInventoryAsync` — seed cart with item A (stock=1) + item B (stock=5); mark item A stock=0; call validate → returns `{ invalid: [itemA], valid: [itemB] }`
- **Integration:** `POST /api/v1/checkout/validate` with cart containing one OOS item → HTTP 422 `{ invalidItems: [{ productVariantId, reason: "OUT_OF_STOCK" }] }`; all in-stock cart → HTTP 200
- **E2E (Playwright):** add item → update stock to 0 via admin API → proceed to checkout → `[data-testid="oos-warning"]` visible with correct product name; checkout button disabled

### Pass Criteria
- HTTP 422 + `invalidItems` array when any cart item is OOS
- `reason: "OUT_OF_STOCK"` in each invalid item
- Checkout button remains disabled while OOS item in cart
- Removing flagged item → re-validate → HTTP 200 → checkout enabled

### Output
- Test report → `docs/test-reports/ENH-CART-003-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-CART-004 — Last-Unit Row-Level Lock on Concurrent Checkout

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 EC-INV-001
**Stack under test:** .NET Core 10 API (xUnit) · k6

### Acceptance Criteria to Validate
- Two concurrent checkout attempts for last unit: exactly one succeeds (HTTP 200), the other returns HTTP 422 `INV_OUT_OF_STOCK` — "Item just sold out"
- Inventory count = 0 after both requests complete; no oversell

### Test Types to Execute
- **Integration:** seed product variant with `stock = 1`; fire `Promise.all([placeOrder(client1), placeOrder(client2)])` → assert exactly one HTTP 200 + one HTTP 422 with `errorCode: "INV_OUT_OF_STOCK"`; assert `ProductVariants.Stock = 0` in DB
- **Performance (k6):** 10 vUsers simultaneously placing orders for `stock = 10` unit → assert exactly 10 HTTP 200 orders, remainder 422; DB stock = 0

### Pass Criteria
- 0 oversell in any concurrent scenario
- Losing request body: `{ errorCode: "INV_OUT_OF_STOCK", message: "Item just sold out" }`
- DB `ProductVariants.Stock` = 0 after all concurrent requests settle
- No deadlock or 500 under concurrent load

### Output
- Test report → `docs/test-reports/ENH-CART-004-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## CHECKOUT

---

## TEST AGENT: ENH-CHKOUT-001 — Email Verification Gate for Checkout > ₹5,000

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 BR-AUTH-003
**Stack under test:** .NET Core 10 API (xUnit) · Playwright E2E

### Acceptance Criteria to Validate
- Unverified email + order > ₹5,000 → HTTP 403 `CHECKOUT_EMAIL_UNVERIFIED`
- Unverified email + order ≤ ₹5,000 → checkout proceeds (HTTP 200)
- Verified email + any amount → checkout proceeds (HTTP 200)

### Test Types to Execute
- **Unit:** `CheckoutAuthorizationService.ValidateEmailVerification` — 4 combinations: (unverified, ₹6000)→403; (unverified, ₹4999)→pass; (verified, ₹6000)→pass; (verified, ₹4999)→pass
- **Integration:** `POST /api/v1/checkout/initiate` with seeded unverified user + ₹6,000 cart → HTTP 403 `CHECKOUT_EMAIL_UNVERIFIED`; verified user + ₹6,000 cart → HTTP 200
- **E2E (Playwright):** unverified user → add ₹6,000 item → checkout → email verification prompt displayed

### Pass Criteria
- Exact threshold: ₹5,000 (not ₹4,999 or ₹5,001)
- 403 body: `{ errorCode: "CHECKOUT_EMAIL_UNVERIFIED", verifyEmailUrl: "..." }`
- UI shows email verification CTA, not generic error

### Output
- Test report → `docs/test-reports/ENH-CHKOUT-001-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-CHKOUT-003 — COD Ceiling Enforcement (₹50,000 max)

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW §3.8, TC-PAY-BVA-002
**Stack under test:** .NET Core 10 API (xUnit) · Playwright E2E

### Acceptance Criteria to Validate
- Cart total ₹50,001 → COD absent from payment options (TC-PAY-BVA-002)
- Cart total ₹50,000 → COD present in payment options
- COD radio button in UI disabled (not hidden) for ineligible carts

### Test Types to Execute
- **Unit:** `PaymentOptionsService.GetCodEligibility(amount)` — ₹50,000 → true; ₹50,001 → false
- **Integration:** `GET /api/v1/checkout/payment-options?cartTotal=50001` → response array does NOT include `{ method: "COD" }`; `?cartTotal=50000` → includes COD
- **E2E (Playwright):** cart with total ₹50,001 → checkout payment step → COD radio has `disabled` attribute (not `display:none`)

### Pass Criteria
- Boundary: ₹50,000 → COD eligible; ₹50,001 → COD ineligible (exact boundary, no rounding)
- UI: `input[value="COD"]` has `disabled` attribute in ineligible state
- API: COD absent from response array (not present with `eligible: false`)

### Output
- Test report → `docs/test-reports/ENH-CHKOUT-003-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## PAYMENTS

---

## TEST AGENT: ENH-PAY-001 — PayU Payment Gateway Failover

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 §2.1, FR-PAY
**Stack under test:** .NET Core 10 API (xUnit) · Playwright E2E

### Acceptance Criteria to Validate
- Razorpay returns HTTP 503 → PayU session created instead; user sees no error during checkout
- Circuit breaker opens after N consecutive Razorpay failures; subsequent requests go directly to PayU
- On Razorpay recovery: circuit breaker closes; Razorpay used again

### Test Types to Execute
- **Unit:** `PaymentGatewaySelector` — Razorpay circuit-open → PayU selected; circuit-closed → Razorpay selected
- **Integration:** mock Razorpay returning 503 → `POST /api/v1/payment/initiate` → response contains PayU session URL; Razorpay mock recovers → next request uses Razorpay
- **E2E (Playwright):** checkout with Razorpay stub disabled → PayU payment iframe loads; user completes payment → order confirmed

### Pass Criteria
- PayU session created within p95 ≤ 500ms when Razorpay unavailable (NFR-PERF-007)
- No HTTP 500 exposed to client during failover
- Circuit breaker state logged in App Insights (`gateway.failover` event)
- No double-charge risk: idempotency key preserved across failover

### Output
- Test report → `docs/test-reports/ENH-PAY-001-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-PAY-002 — HMAC-SHA256 Webhook Verification

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-PAY-009, TC-PAY-SEC-006
**Stack under test:** .NET Core 10 API (xUnit)

### Acceptance Criteria to Validate
- Valid `x-razorpay-signature` / `X-PayU-Signature` → HTTP 200 + order processing
- Tampered payload (signature mismatch) → HTTP 401 + audit log entry + App Insights log
- Constant-time comparison: timing variance < 5ms across 1000 signature pair comparisons (TC-PAY-SEC-006)
- Replay of identical valid webhook → idempotency prevents duplicate order state change

### Test Types to Execute
- **Unit:** `WebhookVerificationService` — compute HMAC-SHA256 with known secret → valid → 200; flip one payload byte → 401; timing test: 1000 valid vs 1000 invalid comparisons → StdDev < 5ms
- **Integration:** `POST /api/v1/webhooks/razorpay` with valid sig → HTTP 200; with invalid sig → HTTP 401 + `AuditLogs` row `action="WEBHOOK_SIG_MISMATCH"`

### Pass Criteria
- Valid sig → 200; invalid sig → 401 (never 500)
- Audit row written within 1s of invalid sig rejection
- Timing test: mean valid vs mean invalid difference < 5ms over 1000 trials
- Replay of valid webhook → idempotency returns cached 200 (no double-processing)

### Output
- Test report → `docs/test-reports/ENH-PAY-002-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-PAY-003 — Idempotency-Key Header Support

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-PAY-012, TE-005
**Stack under test:** .NET Core 10 API (xUnit) · k6

### Acceptance Criteria to Validate
- `Idempotency-Key` (UUIDv4) stored in Redis with 24h TTL
- Duplicate request within 24h TTL → returns cached response body + status (no second order created)
- Different key → new order created
- Missing `Idempotency-Key` header → HTTP 400
- Key older than 24h → treated as new request (stale key ignored)

### Test Types to Execute
- **Unit:** `IdempotencyService` — cache hit returns stored response; cache miss processes and stores
- **Integration:** `POST /api/v1/payment/initiate` with key `k1` → 200 + orderId A; repeat with `k1` → 200 + same orderId A; with `k2` → 200 + orderId B (new); missing key → 400
- **Performance (k6):** 100 vUsers firing `POST` with same key simultaneously → exactly 1 order created in DB

### Pass Criteria
- Same key → same `orderId` in both responses
- DB order count: 1 per unique Idempotency-Key
- Missing key → `{ error: "IDEMPOTENCY_KEY_REQUIRED" }` HTTP 400
- Redis TTL set to 86400s (verified via Redis CLI or test assertion)

### Output
- Test report → `docs/test-reports/ENH-PAY-003-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-PAY-005 — Bank Timeout Reconciliation Poll

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 EC-PAY-001, EC-PAY-003
**Stack under test:** .NET Core 10 API (xUnit) · Playwright E2E

### Acceptance Criteria to Validate
- Bank gateway timeout post-debit → order status = Pending; reconciliation poll at T+60s; if still indeterminate at T+15min surface "Payment status pending" (EC-PAY-001)
- UPI Collect expired → Razorpay `failed` event; "UPI request expired" + retry CTA (EC-PAY-003)
- No double-charge: reconciliation never triggers second debit

### Test Types to Execute
- **Unit:** `ReconciliationService` — indeterminate response → order transitions to Pending; resolved response → Completed or Failed
- **Integration:** use Razorpay test card `5104 0600 0000 0008` (network failure simulation) → `POST /api/v1/payment/callback` with timeout code → order status = Pending in DB; mock resolved poll → order status = Completed
- **E2E (Playwright):** complete payment with timeout card → order page shows "Payment Pending" banner (not error); after poll resolves → banner updates

### Pass Criteria
- Order status = `Pending` immediately after timeout (not `Failed`)
- "Payment status pending" UI message visible within 15min
- No second charge event logged
- UPI expired: toast "UPI request expired" + retry button renders

### Output
- Test report → `docs/test-reports/ENH-PAY-005-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-PAY-006 — Razorpay Vault Tokenisation

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-SEC-001, NFR-SEC-A01
**Stack under test:** .NET Core 10 API (xUnit) · OWASP ZAP

### Acceptance Criteria to Validate
- After save-card: DB stores only `{ tokenId, last4, network }` — no PAN, CVV, or expiry in any column
- OWASP ZAP scan of `/api/v1/payment/*` endpoints: 0 Critical/High data-exposure findings

### Test Types to Execute
- **Unit:** `PaymentTokenService` — after tokenisation mock, assert `SavedCard` entity has `Pan = null`, `Cvv = null`, `ExpiryMonth = null`; `TokenId` non-null
- **Integration:** `POST /api/v1/payment/save-card` → DB SELECT on `payments.SavedCards` → assert no PAN/CVV column populated; `tokenId` present
- **Security (OWASP ZAP):** active scan on payment endpoints → assert 0 High/Critical `Information Disclosure` or `PCI DSS` findings

### Pass Criteria
- DB schema: no column named `Pan`, `CardNumber`, `Cvv`, `Expiry*` with data
- OWASP ZAP: 0 Critical, 0 High findings on payment surface
- `tokenId` present and non-null in DB after save-card
- Charge-with-token flow succeeds using stored `tokenId` (no re-entry of card details)

### Output
- Test report → `docs/test-reports/ENH-PAY-006-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-PAY-007 — StyleNest Cash Pessimistic Lock

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-PROMO-005, TC-CART-FUNC-022, TC-CART-FUNC-022B
**Stack under test:** .NET Core 10 API (xUnit) · k6

### Acceptance Criteria to Validate
- `SELECT … WITH (UPDLOCK, ROWLOCK)` on `WalletTransactions` during redemption
- Insufficient balance → HTTP 409 `STYLENEST_INSUFFICIENT_BALANCE` with current balance in response body
- 10 parallel redemptions of ₹100 from ₹100 balance → exactly 1 succeeds (TC-CART-FUNC-022B)

### Test Types to Execute
- **Unit:** `WalletService.RedeemStyleNestCash` — balance ₹100, redeem ₹101 → `STYLENEST_INSUFFICIENT_BALANCE` with `currentBalance: 100`; balance ₹100, redeem ₹100 → 200 + balance = 0
- **Integration (concurrent):** `Promise.all(10 × POST /api/v1/wallet/redeem { amount: 100 })` against seeded ₹100 balance → assert exactly 1 HTTP 200, 9 HTTP 409
- **Performance (k6):** 50 vUsers concurrently redeeming → no deadlock; p95 ≤ **150ms** (NFR-PERF-006 cart budget)

### Pass Criteria
- Exactly 1 of 10 concurrent redemptions succeeds
- HTTP 409 body: `{ errorCode: "STYLENEST_INSUFFICIENT_BALANCE", currentBalance: 0 }`
- No deadlock or timeout under 50-vUser concurrent load
- p95 ≤ 150ms

### Output
- Test report → `docs/test-reports/ENH-PAY-007-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## ORDERS

---

## TEST AGENT: ENH-ORD-001 — Order State Machine CHECK Constraints

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-ORD-002, TSD §6.2 TE-006
**Stack under test:** .NET Core 10 API (xUnit) · SQL Server direct

### Acceptance Criteria to Validate
- `OrderStatusHistory` has SQL CHECK constraints on `FromStatus` and `ToStatus` limiting to valid enum values
- Direct SQL INSERT with invalid status string → SQL Server constraint violation (not application-level catch)
- All 8 valid state transitions succeed via API; all 5 invalid transitions return HTTP 409

### Test Types to Execute
- **Unit:** `OrderStateMachineService` — all valid transitions (Placed→Confirmed, Confirmed→Packed, Packed→Shipped, Shipped→OutForDelivery, OutForDelivery→Delivered, Delivered→Completed, any→Cancelled, Delivered→Returned, Returned→Refunded) succeed; invalid (e.g. Completed→Shipped) throws exception
- **Integration (SQL):** `INSERT INTO orders.OrderStatusHistory (ToStatus) VALUES ('InvalidStatus')` → SQL Server raises CHECK violation error
- **Integration (API):** TC-ORD-FUNC-041..045 — 5 invalid transition attempts via `PUT /api/v1/orders/{id}/status` → HTTP 409 `ORDER_STATE_CONFLICT`

### Pass Criteria
- 9 valid transitions: HTTP 200 + DB row created
- 5 invalid transitions: HTTP 409 `ORDER_STATE_CONFLICT`
- Direct SQL insert with invalid enum: `SqlException` raised by SQL Server (not caught by app)
- `CHECK` constraint visible in `sys.check_constraints` query

### Output
- Test report → `docs/test-reports/ENH-ORD-001-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-ORD-002 — Concurrent State Transition Protection

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-ORD-002, TC-ORD-FUNC-036
**Stack under test:** .NET Core 10 API (xUnit) · k6

### Acceptance Criteria to Validate
- `Promise.all([updateState('Shipped'), updateState('Cancelled')])` on same orderId → exactly one HTTP 200, one HTTP 409 `ORDER_STATE_CONFLICT`
- No orphaned intermediate state (order ends in exactly one terminal-or-valid state)

### Test Types to Execute
- **Integration:** seed order in `Packed` state; fire `Promise.all([PUT .../status {status:"Shipped"}, PUT .../status {status:"Cancelled"}])` → assert exactly one 200 + one 409; DB order status = one of {Shipped, Cancelled}
- **Performance (k6):** 20 vUsers competing on same orderId with two conflicting transitions → assert 1 winner per orderId; 0 orders in invalid/null state

### Pass Criteria
- Exactly 1 HTTP 200 per concurrent pair; other returns 409
- DB `Orders.Status` column has single valid value after race
- `ORDER_STATE_CONFLICT` in 409 response body
- 0 deadlocks or 500 responses

### Output
- Test report → `docs/test-reports/ENH-ORD-002-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## PROMOTIONS

---

## TEST AGENT: ENH-PROMO-001 — StyleNest Cash Earn on Purchase

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 §3.10 FR-PROMO
**Stack under test:** .NET Core 10 API (xUnit) · Playwright E2E

### Acceptance Criteria to Validate
- Configurable % of order value credited as StyleNest Cash after order transitions to `Delivered`
- Credit visible in `WalletTransactions` with `type = "STYLENEST_CASH_EARNED"` and correct amount
- Rate change in config reflected without code deploy

### Test Types to Execute
- **Unit:** `RewardService.CalculateEarnedStyleNestCash(orderValue, rate)` — ₹1,000 × 2% → ₹20; ₹1,000 × 0% → ₹0
- **Integration:** place order → transition to Delivered via admin API → assert `WalletTransactions` row with `amount = orderValue × configRate`; wallet balance increases
- **E2E (Playwright):** complete order journey → order delivered → My Wallet page shows new transaction with correct amount

### Pass Criteria
- Credit amount = `floor(orderValue × configuredRate)`
- Credit applied within 30s of Delivered transition
- `WalletTransactions.Type = "STYLENEST_CASH_EARNED"`; `ReferenceId = orderId`
- Config rate change (without deploy) → next delivered order uses new rate

### Output
- Test report → `docs/test-reports/ENH-PROMO-001-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-PROMO-003 — Flash Sale Price Lock

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-HOME-005, EC-HOME-003
**Stack under test:** .NET Core 10 API (xUnit) · Playwright E2E

### Acceptance Criteria to Validate
- Flash sale price locked server-side; product base price change mid-sale does not affect sale price
- Oversell impossible: only `flashSaleStock` units sold at flash price; remainder at base price
- ATC after sale end → HTTP 410 `SALE_ENDED`
- Negative countdown → UI auto-hides within 1s (EC-HOME-003)

### Test Types to Execute
- **Unit:** `FlashSaleService.LockPrice` — price locked at sale start; `IsValidSaleItem` false after end
- **Integration:** flash sale with `stock = 5`; 6 concurrent ATC calls → assert exactly 5 HTTP 200 at sale price + 1 HTTP 410 `SALE_ENDED` or `INV_OUT_OF_STOCK`; ATC 1s after sale end → HTTP 410
- **E2E (Playwright):** flash sale page → set `endsAt` = now+3s → wait 4s → countdown hides; ATC button disabled

### Pass Criteria
- Exactly `flashSaleStock` units sold at sale price; no oversell
- HTTP 410 `SALE_ENDED` after sale expiry
- UI hides countdown within 1s of negative server value
- Base price update mid-sale does not change `flashSalePrice` in response

### Output
- Test report → `docs/test-reports/ENH-PROMO-003-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## NOTIFICATIONS

---

## TEST AGENT: ENH-NOTIF-001 — Exponential Backoff Retry + Azure Service Bus DLQ

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-NOTIF-006
**Stack under test:** .NET Core 10 API (xUnit)

### Acceptance Criteria to Validate
- Retry schedule: 1m, 3m, 9m (max 3 attempts); after 3rd failure → message moves to per-channel DLQ
- Azure Service Bus DLQ retention: 7 days
- No retry after DLQ; notification marked `Failed` in `NotificationLogs`

### Test Types to Execute
- **Unit:** `NotificationRetryPolicy` — mock provider returning HTTP 500; assert 3 retries at 1m/3m/9m intervals (fast-clock test using `ISystemClock` stub); 4th call: DLQ enqueue asserted, no further provider call
- **Integration:** send notification to unavailable email channel → assert `NotificationLogs.Status = "Failed"` after 3 retries; assert Service Bus DLQ message count = 1 for that channel
- **Boundary:** 2 failures then 3rd success → `NotificationLogs.Status = "Delivered"` (no DLQ)

### Pass Criteria
- Exactly 3 retry attempts (not 2, not 4)
- DLQ populated only after 3rd failure (not earlier)
- `NotificationLogs.RetryCount = 3`, `Status = "Failed"` on DLQ path
- DLQ message has 7-day `TimeToLive` set (Azure Service Bus property)

### Output
- Test report → `docs/test-reports/ENH-NOTIF-001-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-NOTIF-002 — FCM Push Notifications

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 §2.1
**Stack under test:** .NET Core 10 API (xUnit) · Playwright E2E (Service Worker)

### Acceptance Criteria to Validate
- Order status change → FCM push dispatched with correct `orderId` and `status` in payload
- FCM device token stored per user/device; token refresh handled (old token replaced)
- Service Worker receives push event and shows notification in browser

### Test Types to Execute
- **Unit:** `FcmNotificationService` — mock FCM HTTP v1 API; assert correct `message.notification` + `message.data` payload per event type (OrderShipped, FlashSaleStarted, BackInStock)
- **Integration:** order transitions to Shipped → assert `FcmService.SendAsync` called with `{ orderId, status: "Shipped" }` in data payload; mock FCM API returns 200 → `NotificationLogs.Status = "Delivered"`
- **E2E (Playwright):** register Service Worker in test page; dispatch `push` event via `page.evaluate` → `Notification` API called with correct title/body

### Pass Criteria
- FCM payload includes `orderId` and `newStatus` for order events
- Stale token (FCM returns `registration-token-not-registered`) → token deleted from DB and not retried
- Service Worker `push` event triggers `self.registration.showNotification` (asserted via Playwright)
- Delivery receipt stored in `NotificationLogs`

### Output
- Test report → `docs/test-reports/ENH-NOTIF-002-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-NOTIF-004 — Email OTP via Hangfire + MailKit / ACS

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → Phase 9.6 deferred item, SOW v2.1 FR-AUTH-001
**Stack under test:** .NET Core 10 API (xUnit) · Playwright E2E (Mailhog)

### Acceptance Criteria to Validate
- `POST /api/v1/auth/otp/send` → Hangfire job enqueued within 200ms; email delivered to Mailhog (dev) within 10s
- Email body: 6-digit numeric OTP; `To` = correct user email; `Subject` contains "verification code"
- OTP never appears in application logs in Production environment mode

### Test Types to Execute
- **Unit:** `OtpEmailJob` — mock MailKit `SmtpClient`; assert `To`, `Subject`, and 6-digit `Body` OTP; assert no `ILogger.Log` call containing OTP digits
- **Integration:** `POST /api/v1/auth/otp/send` → Hangfire queue has 1 pending job within 200ms (assert via `IMonitoringApi.EnqueuedCount`); execute job → Mailhog API `GET /api/v2/messages` → latest message contains 6-digit OTP
- **E2E (Playwright):** register flow → check Mailhog UI at `http://localhost:8025` → email received with OTP → enter OTP → verified

### Pass Criteria
- Hangfire job enqueued < 200ms from request
- Mailhog receives email within 10s of job execution
- OTP = exactly 6 numeric digits (regex `^\d{6}$`)
- Production mode: no OTP in `ILogger` output (grep application logs)

### Output
- Test report → `docs/test-reports/ENH-NOTIF-004-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## ADMIN

---

## TEST AGENT: ENH-ADMIN-001 — AuditLogs Schema + Retention Policy

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-ADMIN-008
**Stack under test:** .NET Core 10 API (xUnit) · SQL Server direct

### Acceptance Criteria to Validate
- `AuditLogs` table has all 12 columns: `id, userId, action, resourceType, resourceId, beforeStateJson, afterStateJson, ipAddress, userAgent, occurredAt, correlationId, retentionCategory`
- Append-only: no `UPDATE` or `DELETE` via API or ORM; direct SQL DELETE rejected by DB-level constraint or trigger
- Financial logs (`retentionCategory = "Financial"`) not purged before 7 years; non-financial before 3 years

### Test Types to Execute
- **Unit:** `AuditLogService.LogAsync` — all 12 fields populated; `correlationId` matches Serilog `CorrelationId` from request context
- **Integration:** admin `PUT /api/v1/admin/products/{id}` → `AuditLogs` row with `beforeStateJson` (old product) + `afterStateJson` (new product) + `ipAddress` from request
- **Security (SQL):** `DELETE FROM admin.AuditLogs WHERE id = @testId` via test DB connection → assert denied (DENY permission or INSTEAD OF trigger raises error)

### Pass Criteria
- All 12 columns populated on every admin write operation
- Direct SQL DELETE raises error (not silently ignored)
- `correlationId` in audit row matches X-Correlation-ID response header
- DB query: `SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('admin.AuditLogs')` returns ≥12 rows

### Output
- Test report → `docs/test-reports/ENH-ADMIN-001-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## SELLER

---

## TEST AGENT: ENH-SELL-001 — Multi-Tenant Row-Level Security (SQL Server RLS)

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → TSD §6.1 AG-002, TC-SELL-FUNC-023
**Stack under test:** .NET Core 10 API (xUnit) · SQL Server direct

### Acceptance Criteria to Validate
- Seller-A JWT: `GET /api/v1/seller/products` returns only Seller-A's products; Seller-B product IDs absent (TC-SELL-FUNC-023)
- Admin JWT: `GET /api/v1/seller/products` returns all sellers' products (RLS bypassed)
- SQL Server `sys.security_policies` confirms RLS policy active on `sellers.SellerProducts`

### Test Types to Execute
- **Unit:** `RlsPolicyTest` — set `SESSION_CONTEXT(N'SellerId')` to seller-A-GUID; `SELECT * FROM sellers.SellerProducts` → 0 rows from seller-B
- **Integration (API):** as `seller01@mailinator.com` JWT → `GET /api/v1/seller/products` → assert none of the seeded `seller02` product IDs appear; as `admin1@mailinator.com` → same endpoint → all products visible
- **Security:** SQL injection attempt via malformed `SellerId` claim → RLS policy not bypassed; `SESSION_CONTEXT` uses parameterised set

### Pass Criteria
- 0 cross-tenant products in seller API response
- Admin sees full product list across all sellers
- `sys.security_policies` has active FILTER + BLOCK predicate on `sellers.SellerProducts`
- SQL injection via SellerId claim: no cross-tenant data returned

### Output
- Test report → `docs/test-reports/ENH-SELL-001-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## AI

---

## TEST AGENT: ENH-AI-001 — Personalised Product Feed

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-HOME-003, BR-HOME-002, NFR-PERF-004
**Stack under test:** .NET Core 10 API (xUnit) · k6 · Playwright E2E

### Acceptance Criteria to Validate
- Users with ≥5 product views in last 30 days → personalised 12-product rail within **800ms** (FR-HOME-003)
- OOS variants excluded from rail (BR-HOME-002)
- Users with < 5 views → fallback to `trendingByCategory` (FR-HOME-004)

### Test Types to Execute
- **Unit:** `RecommendationService` — seed 5 product view events for test user within 30d → `GetPersonalisedFeed` returns 12 products from viewed categories; seed 4 views → returns trending fallback; OOS product in candidates → excluded
- **Integration:** `GET /api/v1/recommendations/feed` (auth token, 5+ views) → HTTP 200 with 12 products, all `inStock: true`; p95 ≤ 800ms
- **Performance (k6):** 1,000 authenticated vUsers → p95 ≤ **800ms**
- **E2E (Playwright):** login with seeded 5-view user → homepage `[data-testid="personalised-rail"]` renders ≥1 card

### Pass Criteria
- p95 ≤ 800ms at 1,000 vUsers
- Exactly 12 products returned (or all available if < 12 in stock)
- 0 OOS products in rail
- User with 4 views: rail shows trending, not personalised

### Output
- Test report → `docs/test-reports/ENH-AI-001-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-AI-002 — Personalised Feed Fallback (trendingByCategory)

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-HOME-004
**Stack under test:** .NET Core 10 API (xUnit) · Playwright E2E

### Acceptance Criteria to Validate
- Guest (unauthenticated) → `GET /api/v1/recommendations/feed` returns trending products by category
- Authenticated user with < 5 views in last 30d → same trending fallback
- UX is identical whether personalised or fallback (no "fallback" label in UI)

### Test Types to Execute
- **Unit:** `RecommendationService.GetFallback` — returns top 12 products by view count per category in last 7d; OOS excluded
- **Integration:** guest `GET /api/v1/recommendations/feed` → HTTP 200 `{ source: "trending", products: [...] }`; cold-start auth user → same `source: "trending"`; 5-view user → `source: "personalised"`
- **E2E (Playwright):** guest → rail renders; login with 0 views → rail still renders (no blank state); login with 5 views → rail content differs from trending (different product set)

### Pass Criteria
- Guest and cold-start: `source = "trending"` in response
- 5-view user: `source = "personalised"` in response
- Both sources render same rail component (identical DOM structure)
- 0 OOS products in fallback rail

### Output
- Test report → `docs/test-reports/ENH-AI-002-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-AI-004 — AI-Powered Related Products (FBT / Similar / Look)

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-PDP-008, BR-HOME-002
**Stack under test:** .NET Core 10 API (xUnit) · k6 · Playwright E2E

### Acceptance Criteria to Validate
- `?type=similar` → same `categoryId` as source product; OOS excluded
- `?type=fbt` → products co-purchased with source product (co-occurrence model)
- `?type=look` → products tagged with compatible style attribute
- p95 ≤ 800ms for all three rail types

### Test Types to Execute
- **Unit:** `FbtRecommendationService` — seed order history: product A co-purchased with B and C → `GetFbt(A)` returns [B, C]
- **Integration:** `GET /api/v1/products/{id}/related?type=similar` → all `categoryId` match source; `?type=fbt` → co-purchase data drives results; `?type=look` → `styleTag` compatible
- **Performance (k6):** 300 vUsers · all 3 types → p95 ≤ **800ms**
- **E2E (Playwright):** PDP → all 3 `[data-testid="rail-*"]` visible; `@defer` triggers on scroll

### Pass Criteria
- Similar: `categoryId` identical to source for all returned products
- FBT: ≥1 product with shared purchase history (seeded data)
- Look: ≥1 product with matching `styleCompatibilityTag`
- 0 OOS products; 0 source product appearing in its own rails
- p95 ≤ 800ms at 300 vUsers

### Output
- Test report → `docs/test-reports/ENH-AI-004-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## INFRA

---

## TEST AGENT: ENH-INFRA-006 — Blue-Green Deployment with Auto-Rollback

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 FR-OPS-004, TC-OPS-FUNC-005
**Stack under test:** Azure DevOps Pipeline · Playwright (smoke) · k6 (post-swap load)

### Acceptance Criteria to Validate
- Deploy to staging slot → smoke tests (5 critical APIs) → 5-min ramp warmup at 100 req/s → atomic slot swap → 10-min post-swap monitoring → auto-rollback if error rate > 1% (FR-OPS-004 / V12 correction)

### Test Types to Execute
- **Integration (CI pipeline):** trigger deploy pipeline → assert smoke test stage passes (health + 5 critical API calls return 200); slot swap completes without dropped requests
- **Performance (k6):** 30 req/s × 10min post-swap → `http_req_failed` rate < **1%**; p95 within NFR-PERF-004..007 budgets
- **E2E (Playwright smoke):** 5 critical paths post-swap: homepage load, product search, PDP, add-to-cart, user login — all return 200

### Pass Criteria
- Smoke test suite: 5/5 API calls return 200 before slot swap
- Slot swap: 0 HTTP 5xx during swap window (App Insights `requests/failed` = 0 during swap)
- Post-swap k6: error rate < 1% sustained 10min; p95 catalog ≤ 300ms / cart ≤ 150ms
- Auto-rollback verified: inject 2% error rate artificially → pipeline triggers slot swap back within 2min

### Output
- Test report → `docs/test-reports/ENH-INFRA-006-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-INFRA-008 — k6 Spike + Soak Load Tests

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 NFR-PERF-008, TC-NFR-LOAD-003, TC-NFR-LOAD-004
**Stack under test:** k6 (load) · Azure App Insights (metrics)

### Acceptance Criteria to Validate
- **Spike:** 100 → 10,000 vUsers in 30s; p95 catalog ≤ 300ms, cart ≤ 150ms, error rate < 1% (NFR-PERF-008)
- **Soak:** 1,000 vUsers × 60min; no memory leak; p95 ≤ 110% of baseline throughout run (TC-NFR-LOAD-004)

### Test Types to Execute
- **Spike (k6):**
  ```js
  stages: [
    { duration: '30s', target: 10000 },
    { duration: '5m', target: 10000 },
    { duration: '30s', target: 0 }
  ]
  thresholds: {
    'http_req_duration{endpoint:catalog}': ['p(95)<300'],
    'http_req_duration{endpoint:cart}': ['p(95)<150'],
    'http_req_failed': ['rate<0.01']
  }
  ```
- **Soak (k6):**
  ```js
  stages: [{ duration: '60m', target: 1000 }]
  ```
  Monitor: App Insights `performanceCounters/memoryAvailableBytes` — assert no downward trend > 20% over 60min

### Pass Criteria
- Spike: p95 catalog ≤ 300ms, cart ≤ 150ms, error rate < 1%
- Soak: p95 at 60min ≤ 110% of p95 at 5min (no degradation); 0 OOM kills; GC pressure flat
- Both scenarios: no HTTP 503 from App Service

### Output
- Test report → `docs/test-reports/ENH-INFRA-008-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-INFRA-009 — OWASP ZAP Automated CI Scan

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 NFR-SEC-A01..A09, TC-OPS-SEC-001..003
**Stack under test:** OWASP ZAP · Snyk / Dependabot · Azure DevOps Pipeline

### Acceptance Criteria to Validate
- ZAP active scan against running stack: 0 Critical findings, 0 High findings (NFR-SEC-A01..A09)
- All 10 OWASP Top 10:2021 categories covered (A01–A10); A08 (Software & Data Integrity) included via supply-chain checks
- NuGet + npm package signatures verified in CI; pipeline fails on Critical/High CVE (TC-OPS-SEC-001)

### Test Types to Execute
- **Security (ZAP baseline):** `docker run -t owasp/zap2docker-stable zap-baseline.py -t http://gateway:5000` → assert report `HIGH = 0`, `CRITICAL = 0`
- **Security (ZAP active):** full active scan on auth + payment + admin surfaces → same pass criteria
- **Supply chain (Snyk):** `snyk test --severity-threshold=high` on `/backend` → 0 High/Critical CVEs; `npm audit --audit-level=high` on Angular projects → 0 High
- **Pipeline integrity (A08):** verify CI pipeline YAML has package signature check step; pipeline fails build on unsigned package

### Pass Criteria
- ZAP report: 0 Critical, 0 High findings
- Snyk: 0 High/Critical CVE in NuGet packages
- npm audit: 0 High vulnerabilities in Angular projects
- A08 gate: CI build fails if unsigned package introduced
- Scan completes within 15min in CI pipeline

### Output
- Test report → `docs/test-reports/ENH-INFRA-009-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

## TEST AGENT: ENH-INFRA-010 — axe-core Accessibility CI Gate

**Session type:** Standalone test session (do NOT run alongside IMPL session)
**Read first:** CLAUDE.md → docs/FEATURE-ENHANCEMENTS.md → SOW v2.1 NFR-A11Y-001
**Stack under test:** Playwright + @axe-core/playwright · Angular 21

### Acceptance Criteria to Validate
- WCAG 2.1 AA: 0 critical violations on all P0 routes (NFR-A11Y-001)
- Keyboard navigation: all interactive elements reachable via Tab; logical focus order
- All images have non-empty `alt` text; all form inputs have associated `<label>`

### Test Types to Execute
- **E2E (Playwright + axe-core):** run `checkA11y(page)` on P0 routes: `/`, `/products`, `/products/{id}`, `/cart`, `/checkout`, `/account/login`; assert `violations.filter(v => v.impact === 'critical').length === 0`
- **Keyboard nav (Playwright):** Tab through homepage → assert `document.activeElement` follows logical reading order; Enter on product card → navigates to PDP
- **Structural:** `page.locator('img:not([alt])')` → count = 0; `page.locator('input:not([id])')` without matching label → count = 0

### Pass Criteria
- 0 axe-core `critical` violations on all 6 P0 routes
- 0 axe-core `serious` violations on P0 routes
- All images have `alt` attribute (empty string acceptable for decorative)
- All form inputs have programmatically associated label
- Tab order: no focus trap; no skip-navigation missing

### Output
- Test report → `docs/test-reports/ENH-INFRA-010-report.md`
- Status update → `docs/FEATURE-ENHANCEMENTS.md`

---

*ECM-TSTYLENEST-2026-001 | TEST-AGENT-PROMPTS.md | 43 prompt blocks | Last updated: 2026-05-20*

**AWAITING REVIEW**
