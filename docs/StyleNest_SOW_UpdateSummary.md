 

**STATEMENT OF WORK — UPDATE SUMMARY**

**StyleNest E-Commerce Platform — Full-Stack Clone**

| Field | Original (SOW v1.0) | Updated (as of May 2026) |
| :---- | :---- | :---- |
| Document Version | v1.0 — Initial Release | v2.0 — Post Phase 13 Update |
| Status | Approved for Development | Phases 1–13 Delivered · Phase 14 Pending |
| Stack | .NET Core 10 · Angular · SQL Server · Azure | .NET 10 · Angular 21 · SQL Server 2022 · Redis 7 · Docker · GitHub Actions |
| Delivery Model | 36-week enterprise squad | 2-week Vibe Coding sprint via Claude Code |

> **Note on StyleNest_SOW.docx:** The source file is a binary Word document and cannot be edited directly in this repository. All substantive updates are reflected in this summary and in the updated Technical Specification (`StyleNest_TechSpec_DotNet_Angular_SQL.md` v2.0). Apply these changes to the original `.docx` manually if a formal signed SOW revision is required.

---

# 1. Scope Changes

## 1.1 Features Added (not in original SOW)

| Feature | Phase | Notes |
| :---- | :---- | :---- |
| YARP API Gateway | Phase 9 | Single entry point `:5000`; rate limiting; JWT pre-validation; aggregated health |
| Media.API (file upload service) | Phase 9 | MinIO S3-compatible; MIME + magic bytes validation; image/video upload |
| EAV dynamic attribute system | Phase 9 | 14 attribute definitions; category-scoped attribute filtering on PLP |
| Seller portal — full Angular panel | Phase 10 | Dashboard KPIs, product CRUD with attributes, inventory, orders, payouts, analytics |
| ApexCharts analytics dashboards | Phase 10 | Revenue trend, orders donut, seller performance bar, user registration area charts |
| OTP / forgot-password flow | Phase 11 | `/forgot-password`, `/verify-otp`, `/reset-password` pages + Auth.API endpoints |
| StyleNest Cash wallet UI | Phase 11 | Balance card, add-money modal, transaction history in user account |
| Order return request flow | Phase 11 | Reason selector + submission on order detail page |
| Save-for-later in cart | Phase 11 | NgRx + sessionStorage; move-to-cart; remove-saved actions |
| Notification bell | Phase 11 | Header dropdown; mark-read; mark-all-read; unread count badge |
| Dynamic attribute filters on PLP | Phase 11 | EAV chip selectors loaded from category API |
| GitHub Actions CI/CD | Phase 13 | `ci.yml` (build + test + docker) + `deploy.yml` (Azure Container Apps) |
| Redis distributed cache | Phase 13 | Catalog products (10 min TTL), categories (60 min TTL), cache invalidation on writes |
| Security headers middleware | Phase 13 | 6 HTTP security headers on all APIs via `SecurityHeadersMiddleware` |
| Health checks on all services | Phase 13 | `GET /health` + Docker healthcheck stanzas |
| shared-types/ package | Phase 9.1 | TypeScript interface contracts shared across both Angular apps |

## 1.2 Features Deferred to Phase 14+

| Feature | Original Phase | Deferral Reason |
| :---- | :---- | :---- |
| Email OTP via Mailhog/MailKit | Phase 9 | Console log used in dev; Hangfire job scaffolded |
| Hangfire background jobs | Phase 9/10 | DailyAnalyticsJob, LowStockAlert, ExpireCoupons — scaffolded |
| Azure Cognitive Search | Phase 2 (original) | SQL Server LIKE-based search functional in V1; deferred to Phase 14+ |
| Azure Cosmos DB | Phase 2 (original) | EAV system in SQL Server covers V1 variant attributes |
| Razorpay payment integration | Phase 3 (original) | Payment.API scaffolded; deferred to Phase 14 |
| Azure OpenAI / AI features | Phase 4b (original) | Deferred to Phase 14+ (V2 scope) |
| k6 load testing | Phase 6 (original) | Requires running Azure staging environment (Phase 14) |
| OWASP VAPT | Phase 6 (original) | Requires staging deploy (Phase 14) |
| Angular SSR / Universal | Phase 6 (original) | Deferred — current build is CSR SPA |
| Azure Blob CDN pipeline | Phase 2 (original) | MinIO used in dev; Azure Blob deferred to Phase 14 |

---

# 2. Architecture Changes vs Original SOW

| Dimension | Original SOW | Actual Implementation |
| :---- | :---- | :---- |
| API entry point | Azure API Management (APIM) | YARP Gateway (`:5000`) — APIM deferred to Phase 14 |
| Catalog storage | Cosmos DB for MongoDB API | SQL Server `[catalog]` schema with EAV attributes |
| Cache | Azure Cache for Redis | Redis 7 Alpine (self-hosted in Docker; Azure Redis in production) |
| File storage | Azure Blob Storage + Azure CDN | MinIO S3-compatible (dev); Azure Blob deferred to Phase 14 |
| Message bus | Azure Service Bus | Console log + Hangfire scaffolded; Service Bus deferred to Phase 14 |
| Payments | Razorpay + PayU | Scaffolded; payment flow simulated — Razorpay integration deferred |
| Frontend apps | 1 Angular SPA | 2 Angular 21 apps: `user-panel/` (`:4200`) + `admin-panel/` (`:4201`) |
| Deployment | Azure AKS + Bicep/Terraform | Docker Compose (dev) + Azure Container Apps (CI/CD, Phase 13) |
| Test coverage | > 80% xUnit + Karma/Jest target | 62 xUnit tests (5 projects, 0 failures); 10+ Angular specs; 3 Playwright E2E specs |

---

# 3. Seeded Test Accounts

All passwords: `Test@123`

| Role | Email | Count |
| :---- | :---- | :---- |
| Super Admin | superadmin@mailinator.com | 1 |
| Admin | admin1–4@mailinator.com | 4 |
| Seller | seller01–20@mailinator.com | 20 |
| Customer | user01–15@mailinator.com | 15 |
| **Total** | | **40 accounts** |

Also seeded: 18 categories · 20 brands · 14 attribute definitions · 600 products with variants · 6 banners · 5 coupons.

---

# 4. Updated Service Port Map

| Service | Port | Status |
| :---- | :---- | :---- |
| Gateway.API (YARP) | 5000 | Implemented |
| Auth.API | 5001 | Implemented |
| User.API | 5002 | Implemented |
| Catalog.API | 5003 | Implemented |
| Cart.API | 5004 | Implemented |
| Order.API | 5005 | Implemented |
| Admin.API | 5009 | Implemented |
| Seller.API | 5010 | Implemented |
| Media.API | 5011 | Implemented |
| User Storefront | 4200 | Implemented |
| Admin Panel | 4201 | Implemented |
| SQL Server | 1433 | Implemented |
| Redis | 6379 | Implemented |
| MinIO Console | 9001 | Implemented |

---

# 5. Remaining Deliverables (Phase 14)

- [ ] Deploy full stack to Azure staging environment
- [ ] End-to-end UAT for all 4 roles (Super Admin, Admin, Seller, Customer)
- [ ] Lighthouse performance audit — target score > 90 (mobile)
- [ ] OWASP Top 10 security checklist
- [ ] DNS cutover to production domain
- [ ] 24/7 hypercare monitoring for 2 weeks post-launch

---

*This document is a supplement to `StyleNest_SOW.docx` (v1.0). For formal client sign-off, update the original Word document with the changes described above.*

*Cross-reference: [StyleNest_TechSpec_DotNet_Angular_SQL.md](StyleNest_TechSpec_DotNet_Angular_SQL.md) v2.0 | [FEATURE_ROADMAP.md](../FEATURE_ROADMAP.md)*
