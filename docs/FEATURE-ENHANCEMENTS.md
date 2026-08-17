# FEATURE-ENHANCEMENTS.md — StyleNest V2+ Enhancement Backlog
# ECM-TSTYLENEST-2026-001 | Source: SOW v2.1 + Tech Spec v3.1 (Agent 6 Corrections)

> **Status legend:** `[ ]` TODO · `[~]` IN PROGRESS · `[x]` DONE · `[!]` BLOCKED
>
> **Scope:** Features from SOW v2.1 / TSD v3.1 NOT implemented in Phases 1–13.
> All ENH-IDs are traceable to a SOW FR-ID or TSD section (Agent 6 report).
> All NFR thresholds use exact values from SOW v2.1 Section 4.
>
> Stack constraint: Angular 21 · .NET Core 10 · SQL Server 2022 · Azure (no deviations).

---

## Domain: AUTH

| ENH-ID | Feature Title | Source | Phase | Priority | Status | Parallel-testable | Agent |
|---|---|---|---|---|---|---|---|
| ENH-AUTH-001 | Facebook OAuth 2.0 Login | FR-AUTH-004 | P1 | P1 | [x] | YES | BOTH |
| ENH-AUTH-002 | Apple Sign-In (hidden-email proxy) | FR-AUTH-005 | P1 | P1 | [x] | YES | BOTH |
| ENH-AUTH-003 | Account Merge — social email matches verified account | FR-AUTH-009 / BR-AUTH-007 | P1 | P0 | [x] | YES | BOTH |
| ENH-AUTH-004 | Multi-device Session Management (view + remote-logout) | FR-AUTH-008 | P1 | P0 | [x] | YES | BOTH |
| ENH-AUTH-005 | Account Lockout Exponential Doubling (30min → doubles, max 24h) | FR-AUTH-011 / BR-AUTH-008 | P1 | P0 | [x] | YES | BOTH |
| ENH-AUTH-006 | RSA-3072+ Production Keys via Azure Key Vault HSM Pool | FR-AUTH-006 | P1 | P0 | [x] | NO | IMPL |
| ENH-AUTH-007 | JWT Public Key Loading Resilience — Polly retry + 15min cache | TSD §5.3 / TE-003 | P1 | P0 | [x] | YES | BOTH |
| ENH-AUTH-008 | Angular HTTP Interceptor Single-Flight Refresh (prevent race condition) | TSD §8.4 / TE-007 | P1 | P0 | [x] | YES | BOTH |
| ENH-AUTH-009 | OTP via MSG91 / Azure Communication Services (replace console-log) | FR-AUTH-001 / Phase 9.6 deferred | P1 | P0 | [x] | YES | BOTH |
| ENH-AUTH-010 | WAF IP Lock on OTP Abuse (5 OTPs/hour → 1h IP block) | EC-AUTH-009 | P6 | P1 | [x] | NO | IMPL |
| ENH-AUTH-011 | Right-to-Erasure / PDPB: DELETE /api/v1/user/me endpoint | FR-SEC-006 / TC-AUTH-FUNC-031 | P6 | P0 | [x] | YES | BOTH |
| ENH-AUTH-012 | MFA Enforcement for Admin / SuperAdmin before admin endpoints | BR-AUTH-006 | P1 | P0 | [x] | YES | BOTH |

---

## Domain: CATALOG (Homepage + PLP)

| ENH-ID | Feature Title | Source | Phase | Priority | Status | Parallel-testable | Agent |
|---|---|---|---|---|---|---|---|
| ENH-CAT-001 | Recently Viewed Products Rail (last 12 views) | FR-HOME-006 | P2 | P1 | [x] | YES | BOTH |
| ENH-CAT-002 | Flash Sale Module — server-driven countdown, sold-out transition | FR-HOME-005 | P5 | P0 | [x] | YES | BOTH |
| ENH-CAT-003 | A/B Variant Framework — stable hash userId, experiment duration persistence | FR-HOME-007 | P5 | P1 | [x] | NO | IMPL |
| ENH-CAT-004 | Quick View Modal — desktop hover / mobile tap, ATC without PDP nav | FR-CAT-005 | P2 | P1 | [x] | YES | BOTH |
| ENH-CAT-005 | Infinite Scroll + Pagination Toggle (client-side preference persisted) | FR-CAT-008 | P2 | P1 | [x] | YES | BOTH |
| ENH-CAT-006 | Azure Cognitive Search — facets, autocomplete, synonyms, full-text | FR-CAT-003 / FR-SRCH-001..010 / TSD §7.1 | P2 | P0 | [x] | YES | BOTH |
| ENH-CAT-007 | SEO Canonicalisation — `<link rel="canonical">` + meta-description from SEOTemplates | FR-CAT-006 | P2 | P0 | [x] | YES | TEST |
| ENH-CAT-008 | Category Slug 301-Redirect on Rename | EC-CAT-003 | P2 | P1 | [x] | YES | BOTH |
| ENH-CAT-009 | Angular Bundle Size Budget — initial 500KB error, 350KB warning | TSD §8 / AG-004 | P6 | P0 | [x] | NO | IMPL |
| ENH-CAT-010 | JSON Column Persisted Computed Index (SpecificationsJson) | TSD §6 / PC-002 | P6 | P1 | [x] | YES | BOTH |

---

## Domain: PDP (Product Detail Page)

| ENH-ID | Feature Title | Source | Phase | Priority | Status | Parallel-testable | Agent |
|---|---|---|---|---|---|---|---|
| ENH-PDP-001 | Pincode Delivery Estimate — serviceability, COD eligibility, ETA ≤1s | FR-PDP-003 | P2 | P0 | [x] | YES | BOTH |
| ENH-PDP-002 | EMI Calculator — bank-wise tenure, no-cost EMI in accent-red, min-order config | FR-PDP-004 / BR-PDP-003 | P2 | P0 | [x] | YES | BOTH |
| ENH-PDP-003 | Size Guide Modal — brand-specific charts, cm/inches toggle | FR-PDP-005 | P2 | P1 | [x] | YES | BOTH |
| ENH-PDP-004 | Q&A Section — questions, answers, upvote, paginate | FR-PDP-007 | P2 | P1 | [x] | YES | BOTH |
| ENH-PDP-005 | Related Products Rails — Similar, Complete the Look, FBT | FR-PDP-008 | P5 | P0 | [x] | YES | BOTH |
| ENH-PDP-006 | Back-in-Stock Notification — email+phone capture to BackInStockSubscriptions | FR-PDP-012 | P4 | P1 | [x] | YES | BOTH |
| ENH-PDP-007 | 360-View Product Gallery when `has360View=true` | FR-PDP-001 | P2 | P1 | [x] | YES | BOTH |
| ENH-PDP-008 | Photo Reviews Lightbox in Reviews & Ratings section | FR-PDP-006 | P2 | P1 | [x] | YES | BOTH |

---

## Domain: SEARCH

| ENH-ID | Feature Title | Source | Phase | Priority | Status | Parallel-testable | Agent |
|---|---|---|---|---|---|---|---|
| ENH-SRCH-001 | Azure Cognitive Search warm-up (10 queries post-deploy to prevent cold-start) | TSD §7.1 / PC-001 | P2 | P1 | [x] | NO | IMPL |
| ENH-SRCH-002 | Search Autocomplete + Typeahead (≤200ms p95) | FR-SRCH (TSD §7.1) | P2 | P0 | [x] | YES | BOTH |
| ENH-SRCH-003 | Search Synonyms Dictionary managed via Admin CMS | FR-SRCH (TSD §7.1) | P4 | P1 | [x] | YES | BOTH |
| ENH-SRCH-004 | Search Analytics — top terms, zero-result terms in DailySearchTerms | FR-ANLY (TSD §11) | P5 | P2 | [x] | YES | TEST |

---

## Domain: CART

| ENH-ID | Feature Title | Source | Phase | Priority | Status | Parallel-testable | Agent |
|---|---|---|---|---|---|---|---|
| ENH-CART-001 | Optimistic UI with NgRx rollback on HTTP 4xx/5xx (toast within 500ms) | FR-CART-003 | P2 | P0 | [x] | YES | BOTH |
| ENH-CART-002 | Coupon Validation Detailed Error Codes (7 failure modes with specific errorCode) | FR-CART-006 | P2 | P0 | [x] | YES | BOTH |
| ENH-CART-003 | Inventory Re-validation at Checkout (OOS between ATC and checkout) | EC-INV-002 | P2 | P0 | [x] | YES | BOTH |
| ENH-CART-004 | Last-Unit Row-Level Lock on Concurrent Checkout | EC-INV-001 | P3 | P0 | [x] | YES | BOTH |

---

## Domain: CHECKOUT

| ENH-ID | Feature Title | Source | Phase | Priority | Status | Parallel-testable | Agent |
|---|---|---|---|---|---|---|---|
| ENH-CHKOUT-001 | Email Verification Gate for Checkout > ₹5,000 | BR-AUTH-003 | P3 | P0 | [x] | YES | BOTH |
| ENH-CHKOUT-002 | Express Checkout — one-tap with saved address + saved payment | FR-CHKOUT (TSD §5) | P3 | P1 | [x] | YES | BOTH |
| ENH-CHKOUT-003 | COD Ceiling Enforcement (₹50,000 max) | TC-PAY-BVA-002 / SOW §3.8 | P3 | P0 | [x] | YES | BOTH |

---

## Domain: PAYMENTS

| ENH-ID | Feature Title | Source | Phase | Priority | Status | Parallel-testable | Agent |
|---|---|---|---|---|---|---|---|
| ENH-PAY-001 | PayU Payment Gateway Failover (if Razorpay unavailable) | SOW §2.1 / FR-PAY | P3 | P0 | [x] | YES | BOTH |
| ENH-PAY-002 | HMAC-SHA256 Webhook Verification — constant-time compare, mismatch → HTTP 401 + audit | FR-PAY-009 / TE-008 (implicit) | P3 | P0 | [x] | YES | BOTH |
| ENH-PAY-003 | Idempotency-Key Header (UUIDv4) — Redis 24h TTL, duplicate returns cached response | FR-PAY-012 / TE-005 | P3 | P0 | [x] | YES | BOTH |
| ENH-PAY-004 | IdempotencyKeys Composite Index (UserId, Endpoint) INCLUDE clause | TSD §6.2 / TE-005 | P3 | P1 | [x] | YES | TEST |
| ENH-PAY-005 | Bank Timeout Reconciliation Poll (T+60s → T+15min Pending surface) | EC-PAY-001 | P3 | P0 | [x] | YES | BOTH |
| ENH-PAY-006 | Razorpay Vault Tokenisation — store only token_id + last-4 + network, no PAN in-house | FR-SEC-001 / FR-PAY | P6 | P0 | [x] | YES | BOTH |
| ENH-PAY-007 | Wallet StyleNest Cash Redemption — pessimistic lock SELECT…WITH (UPDLOCK, ROWLOCK) | FR-PROMO-005 / TC-CART-FUNC-022 | P5 | P0 | [x] | YES | BOTH |

---

## Domain: ORDERS

| ENH-ID | Feature Title | Source | Phase | Priority | Status | Parallel-testable | Agent |
|---|---|---|---|---|---|---|---|
| ENH-ORD-001 | Order State Machine CHECK Constraints on OrderStatusHistory (valid enum only) | FR-ORD-002 / TE-006 | P3 | P0 | [x] | YES | BOTH |
| ENH-ORD-002 | Concurrent State Transition Protection (HTTP 409 ORDER_STATE_CONFLICT) | FR-ORD-002 / TC-ORD-FUNC-036 | P3 | P0 | [x] | YES | BOTH |
| ENH-ORD-003 | Azure Service Bus Session Affinity for FIFO per orderId | TSD §2.3 / TE-008 | P3 | P0 | [x] | NO | IMPL |
| ENH-ORD-004 | Full Invalid State Transition Matrix (5 blocked transitions tested) | FR-ORD-002 / TC-ORD-FUNC-041..045 | P3 | P1 | [x] | YES | TEST |
| ENH-ORD-005 | Shiprocket / Delhivery AWB + Tracking + NDR Integration | SOW §2.1 | P4 | P0 | [x] | YES | BOTH |

---

## Domain: PROMOTIONS & LOYALTY

| ENH-ID | Feature Title | Source | Phase | Priority | Status | Parallel-testable | Agent |
|---|---|---|---|---|---|---|---|
| ENH-PROMO-001 | StyleNest Cash Earn on Purchase (configurable % of order value) | FR-PROMO (SOW §3.10) | P5 | P0 | [x] | YES | BOTH |
| ENH-PROMO-002 | StyleNest Cash Expiry Policy (12-month inactivity) | FR-PROMO (SOW §3.10) | P5 | P1 | [x] | YES | BOTH |
| ENH-PROMO-003 | Flash Sale Price Lock — server-driven, race-condition-safe | FR-HOME-005 / EC-INV | P5 | P0 | [x] | YES | BOTH |
| ENH-PROMO-004 | Coupon Stacking Rules — configurable allow/deny per coupon type | FR-CART-006 (g) | P5 | P1 | [x] | YES | BOTH |
| ENH-PROMO-005 | Back-in-Stock Batch Notifier (Hangfire job, scheduled) | FR-PDP-012 | P4 | P1 | [x] | NO | IMPL |

---

## Domain: NOTIFICATIONS

| ENH-ID | Feature Title | Source | Phase | Priority | Status | Parallel-testable | Agent |
|---|---|---|---|---|---|---|---|
| ENH-NOTIF-001 | Exponential Backoff Retry — 1m, 3m, 9m then Azure Service Bus DLQ (7-day retention) | FR-NOTIF-006 | P4 | P0 | [x] | YES | BOTH |
| ENH-NOTIF-002 | FCM Push Notifications (order updates, flash sale, back-in-stock) | SOW §2.1 | P4 | P0 | [x] | YES | BOTH |
| ENH-NOTIF-003 | WhatsApp Business Notification Channel (MSG91 WhatsApp) | SOW §2.1 | P4 | P1 | [x] | NO | IMPL |
| ENH-NOTIF-004 | Email OTP via Hangfire Job + MailKit / Azure Communication Services | Phase 9.6 deferred item | P1 | P0 | [x] | YES | BOTH |
| ENH-NOTIF-005 | DLQ Depth Alert — App Insights alert when DLQ > 100 messages > 15min | FR-NOTIF-006 | P4 | P1 | [x] | NO | IMPL |

---

## Domain: ADMIN

| ENH-ID | Feature Title | Source | Phase | Priority | Status | Parallel-testable | Agent |
|---|---|---|---|---|---|---|---|
| ENH-ADMIN-001 | AuditLogs Schema — append-only, 7y financial / 3y non-financial retention policy | FR-ADMIN-008 | P4 | P0 | [x] | YES | BOTH |
| ENH-ADMIN-002 | Hangfire Dashboard (admin-only route) + Job Management UI | Phase 9.8 deferred | P4 | P1 | [x] | NO | IMPL |
| ENH-ADMIN-003 | Scheduled Jobs: DailyAnalyticsJob, LowStockAlertJob, CartAbandonmentJob, ExpireCouponsJob | Phase 9.8 deferred | P4 | P1 | [x] | YES | BOTH |
| ENH-ADMIN-004 | Image Resize Background Job (SixLabors.ImageSharp via Hangfire) | Phase 9.5 deferred | P4 | P1 | [x] | YES | BOTH |
| ENH-ADMIN-005 | Dynamic Attribute Filtering on Product List (EAV query) | Phase 9.7 deferred | P2 | P1 | [x] | YES | BOTH |
| ENH-ADMIN-006 | Search Synonym Management UI (Admin CMS → Cognitive Search synonyms) | FR-SRCH / ENH-SRCH-003 | P4 | P2 | [x] | YES | BOTH |
| ENH-ADMIN-007 | Distributed Tracing — W3C Trace Context propagation across all services | TSD §11.2 / AG-001 | P6 | P0 | [x] | NO | IMPL |

---

## Domain: SELLER

| ENH-ID | Feature Title | Source | Phase | Priority | Status | Parallel-testable | Agent |
|---|---|---|---|---|---|---|---|
| ENH-SELL-001 | Multi-Tenant Row-Level Security (SQL Server RLS on SellerProducts) | TSD §6.1 / AG-002 | P4 | P0 | [x] | YES | BOTH |
| ENH-SELL-002 | Seller KYC / Verification Workflow (document upload → admin approve) | FR-SELL (TSD §5) | P4 | P1 | [x] | YES | BOTH |
| ENH-SELL-003 | Seller Payout Automated Trigger (Razorpay Route / bank transfer) | FR-SELL (SOW §3.13) | P4 | P1 | [x] | NO | IMPL |
| ENH-SELL-004 | Cross-Tenant Data Isolation Assertion (ArchUnit-style build gate) | TSD §10 / AG-002 | P6 | P0 | [x] | YES | TEST |

---

## Domain: AI

| ENH-ID | Feature Title | Source | Phase | Priority | Status | Parallel-testable | Agent |
|---|---|---|---|---|---|---|---|
| ENH-AI-001 | Personalised Product Feed (≥5 product views in 30d → 12-product rail ≤800ms) | FR-HOME-003 | P5 | P0 | [x] | YES | BOTH |
| ENH-AI-002 | Personalised Feed Fallback — trendingByCategory for guests / cold-start | FR-HOME-004 | P5 | P0 | [x] | YES | BOTH |
| ENH-AI-003 | Azure OpenAI Product Description Assistant (Admin CMS integration) | SOW §2.1 / TSD §5 Phase 5+ | P5 | P2 | [x] | NO | IMPL |
| ENH-AI-004 | AI-Powered Related Products (FBT, Frequently Bought Together) | FR-PDP-008 | P5 | P0 | [x] | YES | BOTH |
| ENH-AI-005 | GA4 + Meta Pixel + Mixpanel Analytics with PII Scrubbing | FR-ANLY-001..005 | P5 | P1 | [x] | NO | IMPL |

---

## Domain: INFRA

| ENH-ID | Feature Title | Source | Phase | Priority | Status | Parallel-testable | Agent |
|---|---|---|---|---|---|---|---|
| ENH-INFRA-001 | TLS 1.3 Floor — App Service + SQL Server + APIM Bicep enforcement | TSD §9.2 / TE-001 | P6 | P0 | [x] | NO | IMPL |
| ENH-INFRA-002 | Redis AAD Managed Identity Auth (replace plain connection string) | TSD §5.3 / TE-004 | P6 | P0 | [x] | NO | IMPL |
| ENH-INFRA-003 | Bicep Diagnostic Settings — all App Services route logs to Log Analytics | TSD §9.2 / TE-009 | P6 | P0 | [x] | NO | IMPL |
| ENH-INFRA-004 | FinOps Resource Tagging — Environment, CostCenter, Owner on every Bicep resource | TSD §9 / AG-005 | P6 | P1 | [x] | NO | IMPL |
| ENH-INFRA-005 | Disaster Recovery Procedure — DR region (Central India), SQL geo-replication, quarterly drill | TSD §11 / AG-006 / NFR-AVAIL-002 RTO≤1h / NFR-AVAIL-003 RPO≤15min | P7 | P0 | [x] | NO | IMPL |
| ENH-INFRA-006 | Blue-Green Deployment with Auto-Rollback (error rate >1% → slot swap back) | FR-OPS-004 | P7 | P0 | [x] | YES | BOTH |
| ENH-INFRA-007 | Schema Migration Strategy — online index creation, rollback scripts, DBA approval gate | TSD §6 / AG-003 | P6 | P1 | [x] | NO | IMPL |
| ENH-INFRA-008 | k6 Load Tests — Spike (100→10K in 30s) + Soak (1K × 60min) | TC-NFR-LOAD-003/004 / NFR-PERF-008 | P6 | P0 | [x] | YES | TEST |
| ENH-INFRA-009 | OWASP ZAP Automated Scan in CI Pipeline (fail on Critical/High) | TC-OPS-SEC-001..003 / NFR-SEC-A01..A09 | P6 | P0 | [x] | YES | TEST |
| ENH-INFRA-010 | axe-core Accessibility CI Gate (WCAG 2.1 AA, 0 critical violations) | NFR-A11Y-001 | P6 | P0 | [x] | YES | TEST |
| ENH-INFRA-011 | KV Firewall + Private Endpoint (MSI tokens only, no public KV access) | TSD §10 / Azure WAF | P6 | P0 | [x] | NO | IMPL |
| ENH-INFRA-012 | App Insights RUM — LCP p75 <2.5s, INP p75 <200ms, CLS p75 <0.1 alerting | NFR-PERF-001..003 | P6 | P0 | [x] | NO | IMPL |

---

## Summary Count Table

| Domain | P0 | P1 | P2 | Total |
|---|---|---|---|---|
| AUTH | 7 | 3 | 0 | **12** (ENH-AUTH-001..012) |
| CATALOG | 5 | 4 | 0 | **10** (ENH-CAT-001..010) |
| PDP | 5 | 3 | 0 | **8** (ENH-PDP-001..008) |
| SEARCH | 1 | 2 | 1 | **4** (ENH-SRCH-001..004) |
| CART | 4 | 0 | 0 | **4** (ENH-CART-001..004) |
| CHECKOUT | 2 | 1 | 0 | **3** (ENH-CHKOUT-001..003) |
| PAYMENTS | 5 | 2 | 0 | **7** (ENH-PAY-001..007) |
| ORDERS | 3 | 2 | 0 | **5** (ENH-ORD-001..005) |
| PROMOTIONS | 3 | 2 | 0 | **5** (ENH-PROMO-001..005) |
| NOTIFICATIONS | 2 | 2 | 0 | **5** (ENH-NOTIF-001..005) — includes deferred item |
| ADMIN | 2 | 4 | 1 | **7** (ENH-ADMIN-001..007) |
| SELLER | 2 | 2 | 0 | **4** (ENH-SELL-001..004) |
| AI | 3 | 1 | 1 | **5** (ENH-AI-001..005) |
| INFRA | 7 | 3 | 0 | **12** (ENH-INFRA-001..012) |
| **TOTAL** | **55** | **31** | **3** | **91** |

### Priority Distribution

```
P0 — Critical (must ship in stated phase)   : 55 items (60%)
P1 — High (planned)                         : 33 items (36%)
P2 — Medium (backlog candidate)             :  3 items  (4%)
```

### Parallel-Testable Distribution

```
YES (test agent can validate independently) : 62 items (68%)
NO  (impl-only: infra, config, DevOps)      : 29 items (32%)
```

### Agent Role Distribution

```
BOTH (impl + test agent)  : 55 items
IMPL only                 : 25 items
TEST only                 :  11 items
```

---

## Implementation Priority Order (P0 items, sequenced by dependency)

```
1. ENH-AUTH-009  → OTP via real SMS/email (unblocks full auth E2E)
2. ENH-AUTH-007  → JWT key resilience (infra prerequisite)
3. ENH-AUTH-008  → Interceptor single-flight (prevents spurious logouts)
4. ENH-CART-001  → Optimistic UI (user-facing quality gate)
5. ENH-CART-002  → Coupon error codes (checkout quality)
6. ENH-CART-003  → Inventory re-validation at checkout (data integrity)
7. ENH-CART-004  → Last-unit row lock (concurrent safety)
8. ENH-ORD-001   → State machine CHECK constraints (DB integrity)
9. ENH-ORD-002   → Concurrent transition protection (API safety)
10. ENH-PAY-002  → Webhook HMAC verification (security)
11. ENH-PAY-003  → Idempotency key support (duplicate prevention)
12. ENH-PAY-005  → Bank timeout reconciliation (UX + data accuracy)
13. ENH-NOTIF-001 → Exponential backoff + DLQ (reliability)
14. ENH-NOTIF-004 → Real OTP email delivery (auth completeness)
15. ENH-INFRA-008 → k6 spike + soak tests (performance gate)
16. ENH-INFRA-009 → OWASP ZAP CI scan (security gate)
17. ENH-INFRA-010 → axe-core a11y gate (compliance gate)
```

---

*ECM-TSTYLENEST-2026-001 | FEATURE-ENHANCEMENTS.md | Last updated: 2026-05-20*

**AWAITING REVIEW**
