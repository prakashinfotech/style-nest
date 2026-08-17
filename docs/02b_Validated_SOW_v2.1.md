# STATEMENT OF WORK — VALIDATED v2.1

**StyleNest E-Commerce Platform**
**Project Code:** ECM-TSTYLENEST-2026-001
**Stack:** .NET Core 10 · Angular 21 · SQL Server 2022 · Azure
**Document Status:** VALIDATED — Agents 1+2 Merged Output
**Standards:** IEEE 830-1998, BDD/Gherkin ACs, OWASP Top 10:2021, PCI-DSS v4.0, WCAG 2.1 AA, PDPB India
**Classification:** CONFIDENTIAL — Internal Use Only
**Date:** May 2026

---

## Document Control

| Version | Date | Author | Change Summary |
|---|---|---|---|
| 1.0 | May 2026 | Lead Architect (original) | Initial SOW |
| 2.0 | May 2026 | Agent 1 (Solutions Architect) | 18-domain re-analysis, dynamic-config matrix, expanded FRs |
| **2.1** | **May 2026** | **Agent 2 (Principal BA)** | **IEEE 830 corrections, edge-case completion, RTM, NFR audit** |

---

## Reference Architecture Decision (CONFIRMED)

> The original SOW v1.0 Section 5 referenced Node.js / Next.js / AWS. The implementation Tech Spec is **.NET Core 10 / Angular 21 / SQL Server 2022 / Azure**. The implementation stack is **authoritative**. Any v1.0 mention of Node, Next.js, MongoDB, Elasticsearch, AWS ECS, Terraform-on-AWS SHALL be read as reference-architecture descriptors only and SHALL be implemented using the .NET/Azure equivalents in Section 5 of this document.

---

# Section 1: Executive Summary

This Statement of Work (SOW v2.1) defines the complete scope, architecture, feature set, deliverables, timeline, resource plan, and commercial terms for the StyleNest E-Commerce Platform delivered on **.NET Core 10 / Angular 21 / SQL Server 2022 / Azure**. The platform SHALL support multi-category retail (Fashion, Electronics, Luxury, Home) with dynamic, CMS-driven configuration across all eighteen feature domains.

## 1.1 Project Objectives

1. Deliver feature-complete e-commerce platform on .NET / Angular / Azure stack.
2. Fully responsive design 320 → 1440+ px.
3. LCP ≤ 2.5s, INP ≤ 200ms, CLS ≤ 0.1 on 4G mobile (Moto G4-class device).
4. 10,000 concurrent users at launch; auto-scale to 100,000+.
5. PCI-DSS Level 1 via Razorpay tokenisation (no PAN in-house).
6. OpenAPI 3.0 docs; ≥80% unit/integration coverage; ≥70% E2E critical-path coverage.
7. PDPB (India) + GDPR readiness with right-to-erasure + data export endpoints.

# Section 2: Scope of Work

## 2.1 In-Scope Deliverables

| Module | Description | Priority | Dynamic | Phase |
|---|---|---|---|---|
| Angular 21 SPA | Standalone components, NgRx, Tailwind, SSR, PWA | P0 | Full | 1–4 |
| .NET Core 10 Microservices | 10 services (Auth, User, Catalog, Search, Cart, Order, Payment, Notification, Seller, Admin) | P0 | Full | 1–4 |
| SQL Server 2022 | 37+ tables with audit columns & soft-delete | P0 | Full | 1–4 |
| Azure Cache for Redis 7 | Session, cart, search, rate limit | P0 | Partial | 1–4 |
| Azure Cognitive Search | Full-text, facets, autocomplete, synonyms | P0 | Full | 3 |
| Admin CMS | Angular dashboard | P0 | Full | 5 |
| Seller Portal | Multi-tenant Angular portal | P1 | Full | 5 |
| Razorpay + PayU | All Indian payment methods | P0 | Full | 4 |
| Shiprocket / Delhivery | AWB, tracking, NDR | P0 | Partial | 4 |
| Azure Communication Services + MSG91 + FCM + WhatsApp Business | Notifications | P0 | Full | 4 |
| GA4 + Meta Pixel + Mixpanel | Analytics with PII scrub | P1 | Full | 5 |
| Azure App Service + Bicep IaC + Azure DevOps Pipelines | Infrastructure | P0 | Full | All |
| QA: xUnit + Playwright + k6 + OWASP ZAP + axe-core | Test automation | P0 | — | All |

## 2.2 Out-of-Scope

- Native iOS / Android (PWA only)
- WMS / ERP integration
- Custom payment gateway development
- Content creation (photo, copy)
- Seller business development
- Post-90-day hypercare support
- Cross-border / multi-currency selling (INR only)
- B2B / corporate procurement
- Live chat / video shopping

---

# Section 3: Functional Requirements

> **ID Convention:** `FR-<DOMAIN>-NNN`, `BR-<DOMAIN>-NNN`, `EC-<DOMAIN>-NNN`. Domain codes: AUTH, HOME, CAT, PDP, SRCH, CART, CHKOUT, PAY, ORD, PROMO, NOTIF, ADMIN, SELL, REV, PWA, SEC, ANLY, OPS, INV.

## 3.1 Authentication & Identity

### Functional Requirements

**FR-AUTH-001** Mobile OTP Registration *(Corrected per Agent 2)*
- Given: valid 10-digit Indian mobile number not currently registered with status=Active
- When: user requests OTP via `POST /api/v1/auth/otp/send`
- Then: system SHALL emit OTP to SMS provider queue within 200ms p95 (server-side only); response SHALL include masked phone (e.g., +91-XXX-XXX-3210), ISO-8601 expiry 300s in future, HTTP 200.
- Dynamic: Yes (expiry, max-attempts) | P0 | Phase 1

**FR-AUTH-002** Email Registration — verification link 24h expiry, generic success response. P0/Phase 1.

**FR-AUTH-003** Google OAuth 2.0 (PKCE), validated via Microsoft.AspNetCore.Authentication.Google. P0/Phase 1.

**FR-AUTH-004** Facebook Login (P1/Phase 1).

**FR-AUTH-005** Apple Sign-In with hidden-email proxy support (P1/Phase 1).

**FR-AUTH-006** JWT Issuance (RS256) *(Corrected)* — minimum RSA-2048, production SHALL use RSA-3072+, key in Azure Key Vault HSM pool. Claims: sub, email, roles[], iat, exp, jti. Access token 15min; refresh token 7d (httpOnly cookie). P0/Phase 1.

**FR-AUTH-007** Refresh Token Rotation *(Corrected)* — reuse detection invalidates entire token family within 2s p99; audit-log entry before response. P0/Phase 1.

**FR-AUTH-008** Multi-device Session Management — view + remote-logout sessions; targeted session revoked within 2s, device gets HTTP 401 on next call. P0/Phase 1.

**FR-AUTH-009** Account Merge — social-login email exactly matches verified existing account → link identity, preserve order history + wishlist + StyleNest Cash. P0/Phase 1.

**FR-AUTH-010** Password Reset — link expires 60min, single-use. P0/Phase 1.

**FR-AUTH-011** Account Lockout *(Corrected)* — 5 failures within 15min → 30min lockout; doubles on subsequent failure (max 24h); successful login resets counter; unlock email includes reset-password link valid 60min. P0/Phase 1.

**FR-AUTH-012** RBAC — roles: Customer, Seller, Support, Admin, SuperAdmin; ASP.NET Core policy-based authorisation. P0/Phase 1.

### Business Rules

- **BR-AUTH-001** OTP exactly 6 numeric digits, single-use, invalidated on success or expiry.
- **BR-AUTH-002** Mobile MAY associate with at most one Active account; orphans (no email, no orders, >90d inactive) reclaimable.
- **BR-AUTH-003** Email verification REQUIRED before paid checkout > ₹5,000.
- **BR-AUTH-004** Access tokens MUST NEVER hit localStorage/sessionStorage — NgRx memory only.
- **BR-AUTH-005** Refresh tokens only via httpOnly, Secure, SameSite=Lax cookie.
- **BR-AUTH-006** Admin/SuperAdmin MUST MFA before admin endpoints.
- **BR-AUTH-007** Account merge preserves older `userId`; newer identity archived.
- **BR-AUTH-008** Failed-login events written to audit log within 1s.

### Edge Cases

- **EC-AUTH-001** Concurrent OTP in cooldown → HTTP 429.
- **EC-AUTH-002** Expired OTP submitted → HTTP 410, `AUTH_OTP_EXPIRED`.
- **EC-AUTH-003** Social email matches different-phone account → merge prompt to user, not auto-merge.
- **EC-AUTH-004** Account suspended mid-session → next call HTTP 403, frontend re-auth.
- **EC-AUTH-005** JWT clock skew > 30s → HTTP 401, client refreshes.
- **EC-AUTH-006** Refresh token reuse → family invalidated, audit, user emailed.
- **EC-AUTH-007** OAuth `email_verified:false` → pause, prompt verify email.
- **EC-AUTH-008** Social account deleted at provider → next refresh fails, prompt set-password.
- **EC-AUTH-009** *(Added by Agent 2)* OTP rate limit reached → HTTP 429 + WAF IP lock 1h on persistent abuse.
- **EC-AUTH-010** *(Added)* Social email matches existing email/password → merge prompt with current-password challenge per BR-AUTH-007.
- **EC-AUTH-011** *(Added)* Session token valid but account disabled mid-session → HTTP 403 `AUTH_ACCOUNT_DISABLED`.
- **EC-AUTH-012** *(Added)* Multiple OTP requests per session → each invalidates previous; max 5/hour applies.

## 3.2 Homepage & Personalisation

### Functional Requirements

**FR-HOME-001** Dynamic Hero Carousel *(Corrected)*
- Given: 4 active homepage banners targeted mobile + India + guest
- When: guest user from India loads on mobile
- Then: `GET /api/v1/cms/banners/homepage` SHALL return exactly the 4 banners in `displayOrder` within p95 < 100ms (cache hit) or p95 < 400ms (cache miss); cache TTL=60s; invalidated on CMS save.
- Dynamic: Yes (Full CMS) | P0 | Phase 2

**FR-HOME-002** Mega-menu Navigation — top-level + 2 levels sub-cats + brand spotlight slots. P0/Phase 2.

**FR-HOME-003** Personalised Feed (Auth) — recommendation algo for users with ≥5 product views in last 30d; 12-product rail within 800ms. P0/Phase 5.

**FR-HOME-004** Personalised Feed Fallback — `trendingByCategory` for guests/cold-start. [ENHANCED] P0/Phase 5.

**FR-HOME-005** Flash Sale Module — server-driven countdown, sold-out transition, queue when configured. P0/Phase 5.

**FR-HOME-006** Recently Viewed Rail — last 12 product views. P1/Phase 2.

**FR-HOME-007** A/B Variant Framework [ENHANCED] — stable hash of userId (or device-fingerprint for guests). P1/Phase 5.

### Business Rules

- **BR-HOME-001** Carousel MUST NOT auto-advance in first 2s (CLS).
- **BR-HOME-002** Personalised rails MUST exclude OOS variants.
- **BR-HOME-003** A/B variant assignment persists per user for experiment duration.

### Edge Cases

- **EC-HOME-001** Carousel image fails → fallback default + LQIP.
- **EC-HOME-002** CMS returns 0 banners → render curated default set.
- **EC-HOME-003** Flash sale ends mid-render → server countdown returns negative; UI auto-hides.

## 3.3 Product Catalog & PLP

### Functional Requirements

**FR-CAT-001** Category Tree — `/api/v1/catalog/categories/tree`, 80 nodes < 50KB gzip, p95 <100ms cached / <400ms cold, Redis 600s TTL. P0/Phase 2.

**FR-CAT-002** PLP Initial Render *(Corrected)* — Angular Universal SSR; first 24 products; CDN edge cache 60s per `categoryId × filterHash × sortHash × page`; FCP ≤ 1.8s on Moto G4-class device under WebPageTest 'Mobile 4G' profile (1.6 Mbps down, 750 Kbps up, 150ms RTT, 4× CPU throttle). P0/Phase 2.

**FR-CAT-003** Faceted Filtering — multi-select AND-across-facets, OR-within-facet; Cognitive Search; refresh ≤500ms. P0/Phase 2.

**FR-CAT-004** Sort Options — Recommended, Price↑↓, Newest, Top Rated, Highest Discount. P0/Phase 2.

**FR-CAT-005** Quick View Modal — desktop hover / mobile tap, ATC without PDP nav. P1/Phase 2.

**FR-CAT-006** SEO Canonicalisation — `<link rel="canonical">` + meta-description from `SEOTemplates`. P0/Phase 2.

**FR-CAT-007** Applied Filter Chips — × removal, Clear All, URL query persistence. P0/Phase 2.

**FR-CAT-008** Infinite Scroll + Pagination Toggle — client-side persisted preference. P1/Phase 2.

**FR-CAT-009** Empty State — curated suggestions + Clear All CTA. P0/Phase 2.

### Business Rules

- **BR-CAT-001** Filter counts reflect facets applied (faceted counts, not pre-filter).
- **BR-CAT-002** Sort persists on reload via `?sort=` parameter.
- **BR-CAT-003** All-variant-OOS products excluded by default; toggle filter to include.

### Edge Cases

- **EC-CAT-001** Slider min > max → swap silently, apply.
- **EC-CAT-002** 0 results → render empty state, never error.
- **EC-CAT-003** Category renamed → old slug 301-redirect to new.

## 3.4 Product Detail Page

### Functional Requirements

**FR-PDP-001** Multi-image Gallery — pinch-zoom mobile, 60/40 desktop, 360-view when `has360View=true`, LCP ≤ 1.8s with LQIP. P0/Phase 2.

**FR-PDP-002** Variant Selector *(Corrected)* — size pills + colour swatches, real-time OOS per variant (disabled + strikethrough); urgency `Only X left` when stock < `lowStockThresholdByCategory[productCategory]` (config; defaults Fashion=5/Electronics=3/Luxury=1). P0/Phase 2.

**FR-PDP-003** Pincode Delivery Estimate — serviceability, COD eligibility, ETA, free-delivery threshold, express; ≤1s response. P0/Phase 2.

**FR-PDP-004** EMI Calculator — bank-wise tenure, instalment, no-cost EMI in accent-red; min-order config. P0/Phase 2.

**FR-PDP-005** Size Guide Modal — brand-specific from `BrandSizeCharts`; cm/inches toggle. P1/Phase 2.

**FR-PDP-006** Reviews & Ratings Section — star breakdown, verified-purchase badge, photo reviews lightbox, sort, paginate 10/page. P0/Phase 2.

**FR-PDP-007** Q&A Section — questions, answers, upvote, paginate. P1/Phase 2.

**FR-PDP-008** Related Products — Similar, Complete the Look, FBT rails. P0/Phase 5.

**FR-PDP-009** Sticky ATC Bar (Mobile) — appears past primary ATC scroll; variant + ATC + Buy Now. P0/Phase 2.

**FR-PDP-010** Buy Now — bypass cart, direct checkout, isolated single-item cart. P0/Phase 2.

**FR-PDP-011** Wishlist Toggle — heart icon, adds with selected variant. P0/Phase 2.

**FR-PDP-012** Back-in-Stock Notification [ENHANCED] — capture email+phone to `BackInStockSubscriptions`. P1/Phase 4.

### Business Rules

- **BR-PDP-001** LCP < 2.5s on 4G mobile (NFR floor).
- **BR-PDP-002** Variant selection updates URL `?variant={sku}`.
- **BR-PDP-003** EMI hidden below min-order value.
- **BR-PDP-004** Buy Now creates temporary single-item cart isolated from persistent cart.

### Edge Cases

- **EC-PDP-001** Product inactive mid-session → soft 404.
- **EC-PDP-002** All variants OOS → ATC disabled; back-in-stock prominent.
- **EC-PDP-003** Image CDN unreachable → LQIP + visibility-retry.
- **EC-PDP-004** Pincode service degraded → "Enter pincode" + cached defaults.
- **EC-PDP-005** Price changed between PDP load and ATC → re-fetch + user confirm.

## 3.5–3.18 Remaining Feature Domains

> **Note:** Remaining 14 domains retain the FR enumeration from Agent 1 with the following Agent-2 corrections applied:

### 3.5 Search Engine — FR-SRCH-001..010 (unchanged from Agent 1 enumeration)

### 3.6 Cart & Wishlist

**FR-CART-003** *(Corrected)* — Optimistic UI with NgRx state, rollback on HTTP 4xx/5xx, toast "Cart update failed" within 500ms.

**FR-CART-006** *(Corrected)* — Coupon validation enumerated: (a) exists+active, (b) within validity window, (c) maxUsesPerUser, (d) global maxUses, (e) subtotal ≥ minOrder, (f) eligible categories/products, (g) stacking rules — each failure has specific errorCode.

All others — unchanged from Agent 1 enumeration.

### 3.7 Checkout Flow — FR-CHKOUT-001..010 (unchanged)

### 3.8 Payment Engine

**FR-PAY-009** *(Corrected)* — HMAC-SHA256 webhook verification, KV-stored secret, constant-time comparison, headers `x-razorpay-signature`/`X-PayU-Signature`, mismatch → HTTP 401 + audit + App Insights log.

**FR-PAY-012** *(Corrected)* — `Idempotency-Key` header (UUIDv4) required; Redis-stored `{key→response}` 24h TTL; duplicate within TTL returns cached response; stale keys ignored.

All others — unchanged.

#### Payment Edge Cases (Agent 2 Additions)

- **EC-PAY-001** Bank timeout post-debit → order Pending; reconciliation poll at T+60s; if indeterminate at T+15min surface "Payment status pending".
- **EC-PAY-002** Double-click → client button disable + server Idempotency-Key.
- **EC-PAY-003** UPI Collect expired → Razorpay `failed` event; "UPI request expired" + retry CTA.
- **EC-PAY-004** Partial success multi-item split → payment at order level, not item; partial refunds via refund service.
- **EC-PAY-005** Refund initiated while return window closes → refund processes; only initiation is window-gated.
- **EC-PAY-006** Wallet balance == order amount → post-payment balance ₹0.00, no rounding issue.

### 3.9 Order Management

**FR-ORD-002** *(Corrected)* — State machine DAG explicitly enumerated:
- Placed → Confirmed | Cancelled
- Confirmed → Packed | Cancelled
- Packed → Shipped | Cancelled
- Shipped → OutForDelivery | Returned (post-receipt)
- OutForDelivery → Delivered | Returned (refused)
- Delivered → Completed | Returned (within window)
- Completed → (terminal)
- Cancelled → (terminal)
- Returned → Refunded → (terminal)
All transitions persisted in OrderStatusHistory (timestamp, actor, reason).

All others — unchanged.

### 3.10 Promotions & Loyalty

**FR-PROMO-005** *(Corrected)* — StyleNest Cash redemption uses pessimistic lock `SELECT ... WITH (UPDLOCK, ROWLOCK)` on StyleNestCashTransactions; re-checked within lock; insufficient → `STYLENEST_INSUFFICIENT_BALANCE` with current balance.

### 3.11 Notifications

**FR-NOTIF-006** *(Corrected)* — Exponential backoff: 1m, 3m, 9m (max 3); after failure → per-channel DLQ in Azure Service Bus (7-day retention); alert on DLQ depth > 100 for > 15min.

### 3.12 Admin CMS

**FR-ADMIN-008** *(Corrected)* — `AuditLogs` schema: id, userId, action, resourceType, resourceId, beforeStateJson, afterStateJson, ipAddress, userAgent, occurredAt, correlationId. Retention: 7y financial, 3y non-financial. Append-only.

### 3.13 Seller / Brand Portal — FR-SELL-001..007 (unchanged)

### 3.14 Reviews & Ratings — FR-REV-001..005 (unchanged)

### 3.15 PWA & Performance — FR-PWA-001..006 (unchanged)

### 3.16 Security & Compliance

**FR-SEC-001** *(Corrected)* — Tokenisation at Razorpay Vault on first use; store only token_id + last-4 + network; no PAN/CVV/expiry touches StyleNest systems; annual QSA via SAQ-D + pentest.

**FR-SEC-006** *(Corrected)* — Right-to-erasure: (a) anonymise Users PII, (b) preserve Orders+Payments with anonymised refs, (c) delete Sessions+Cart+Wishlist+ReviewImages+oldAuditLogs, (d) emit GDPRErasureEvent, (e) complete within 30d per PDPB SLA.

### 3.17 Analytics — FR-ANLY-001..005 (unchanged)

### 3.18 DevOps

**FR-OPS-004** *(Corrected)* — Blue-green: (a) deploy to staging slot, (b) smoke tests (health + 5 critical APIs), (c) 5-min warmup at 100 req/s ramp, (d) atomic slot swap, (e) 10-min post-swap error monitoring with auto-rollback at >1% error rate.

### 3.19 Inventory (Cross-cutting) [NEW SECTION — Agent 2]

- **EC-INV-001** Last unit, two concurrent buyers → row-level lock; second order `INV_OUT_OF_STOCK`, "Item just sold out".
- **EC-INV-002** Cart OOS between ATC and checkout → re-validate at checkout; flag with "Remove to continue".
- **EC-INV-003** Variant OOS after PDP load → ATC fails inventory check; prompt alternative variant.
- **EC-INV-004** Seller marks OOS while order in transit → no impact on in-flight; subsequent inventory updates apply.

---

# Section 4: Non-Functional Requirements

(Section 4 unchanged from Agent 1 v2.0 — already meets IEEE 830 measurability per Agent 2 audit Section 6.)

| NFR ID | Target | Method | Threshold | Monitor |
|---|---|---|---|---|
| NFR-PERF-001 LCP | < 2.5s on 4G | Lighthouse + WebPageTest Mumbai | p75 < 2.5s × 28d | App Insights RUM |
| NFR-PERF-002 INP | < 200ms | RUM | p75 < 200ms × 28d | App Insights |
| NFR-PERF-003 CLS | < 0.1 | Lighthouse + RUM | p75 < 0.1 × 28d | App Insights |
| NFR-PERF-004 Catalog API p95 | ≤ 300ms/800ms | k6 10K vUsers | p95 ≤ targets | App Insights traces |
| NFR-PERF-005 Search API p95 | ≤ 200ms | k6 + Cognitive metrics | p95 ≤ 200ms | App Insights |
| NFR-PERF-006 Cart API p95 | ≤ 150ms | k6 | p95 ≤ 150ms | App Insights |
| NFR-PERF-007 Payment API p95 | ≤ 500ms (excl gateway RTT) | k6 + synthetic | p95 ≤ 500ms | App Insights |
| NFR-PERF-008 Concurrent | 10K | k6 30min sustained | <1% error, p95 within | App Insights + Load Testing |
| NFR-AVAIL-001 Uptime | 99.9% | Uptime Robot | <8.76hr/year downtime | App Insights |
| NFR-AVAIL-002 RTO | ≤ 1h | DR drill | ≤ 1h | DR runbook |
| NFR-AVAIL-003 RPO | ≤ 15min | SQL geo-replication metrics | ≤ 15min | Azure SQL Audit |
| NFR-A11Y-001 WCAG | 2.1 AA | axe-core CI + manual NVDA/JAWS | 0 critical | CI gate |
| NFR-SEC-A01..A09 | OWASP Top 10 | OWASP ZAP + manual VAPT | 0 Critical/High | Sentry + App Insights |
| NFR-SEO-001..004 | CWV green + JSON-LD + canonical + sitemap | Lighthouse + Search Console | All green | Search Console |

---

# Section 5: Technical Architecture (Confirmed)

| Layer | Tech |
|---|---|
| Frontend | Angular 21 standalone, NgRx, Tailwind, Universal SSR, PWA |
| Backend | .NET Core 10 Web API, 10 microservices |
| Database | SQL Server 2022 |
| Cache | Azure Cache for Redis 7 |
| Search | Azure Cognitive Search |
| Storage | Azure Blob + Azure CDN |
| Messaging | Azure Service Bus |
| Identity | ASP.NET Core Identity + JWT Bearer |
| Payments | Razorpay + PayU (failover) |
| Notifications | Azure Communication Services + MSG91 + FCM + WhatsApp Business |
| AI/ML | Azure OpenAI (Phase 5) |
| Monitoring | Azure Application Insights + Sentry |
| IaC | Bicep |
| CI/CD | Azure DevOps Pipelines |

# Section 6: UI/UX & Responsive Design

Tailwind breakpoints sm:480 md:768 lg:1024 xl:1280 2xl:1440. Design tokens per Tech Spec §3. Component library shadcn-equivalent in Angular Material + custom.

# Section 7: Project Timeline

36 weeks across 7 phases (P0..P7 per Tech Spec §13). Phase 0 (Wk 1-2) Discovery; P1 (Wk 3-8) Infra+Auth; P2 (Wk 9-16) Catalog+Search+Cart; P3 (Wk 17-22) Checkout+Payment+Orders; P4 (Wk 23-27) Admin+Seller+AI; P5 (Wk 28-30) Promo+Loyalty; P6 (Wk 31-34) QA+Sec+Perf; P7 (Wk 35-36) UAT+Go-live.

# Section 8: Team Structure

1 Solution Architect | 3 Senior Full-Stack | 2 Frontend (Angular) | 2 Backend (.NET) | 1 UI/UX | 1 DevOps | 2 SDET | 1 Data/Search | 1 PM | 1 BA (PT) | 1 Security Consultant (PT)

# Section 9: Deliverables & Acceptance

Per Section 4 NFRs. All P0+P1 stories signed off. 0 Critical/High open. Load 10K sustained <1% error. CWV green. PCI-DSS SAQ-D clean. OWASP no High/Critical. WCAG 2.1 AA. Coverage ≥80% unit/integration, ≥70% E2E.

# Section 10: Commercial Terms

Fixed-price + T&M hybrid. 7-milestone payment schedule (Kick 10%, P1 10%, P2 15%, P3 20%, P4 15%, P5-6 15%, GoLive 15%). 90-day hypercare. IP transfer on final payment. Change Request process formalised.

# Section 11: Risk Register

| Risk | Prob | Impact | Mitigation | Owner |
|---|---|---|---|---|
| Vibe-coding prompt drift | H | H | CLAUDE.md investment + one-component-per-prompt | Devs |
| Scope creep | H | H | CR process + Phase 0 lock | PM |
| Razorpay/Logistics API instability | M | H | PayU failover + circuit breaker + webhook idempotency | Architect |
| Performance miss under load | M | H | Early k6 from P2 + CDN+Redis from P1 | DevOps |
| PCI-DSS delay | L | H | QSA early + tokenisation only | Security |
| Resource attrition | M | H | TASK-TRACKER + CLAUDE.md continuity | PM/HR |
| Client feedback delays | H | M | Contract 5-day SLA + parallel workstreams | PM |
| Azure Mumbai capacity | L | H | Central India failover | DevOps |
| Angular 21 SSR memory | M | M | Cluster + memory autoscale | DevOps |
| EF Core N+1 | M | H | Code review checklist + load testing | Tech Lead |
| PDPB enforcement timing | M | M | Right-to-erasure from day 1 | Compliance |

# Section 12: Approvals & Sign-off

| Party | Name & Title | Signature | Date |
|---|---|---|---|
| Client — Authorised Rep | | | |
| Development Partner — Lead Architect | | | |
| QA / Compliance | | | |
| UI/UX Lead | | | |

---

# Appendix A: Requirements Traceability Matrix (P0)

(Per Agent 2 Validation Report Section 5 — 35-row matrix)

# Appendix B: Dynamic Configuration Catalog

(Per Agent 1 Phase 2 output — 18-domain config matrix)

---

*ECM-TSTYLENEST-2026-001 | SOW v2.1 | CONFIDENTIAL*
