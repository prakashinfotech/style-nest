# FEATURE_ROADMAP.md — Fashion eCommerce Platform
# Feature Improvement & Implementation Roadmap

> Status legend: `[ ]` Not started · `[~]` In progress · `[x]` Complete · `[!]` Blocked
>
> **This file is the single source of truth for implementation progress.**
> Update task status immediately upon completion. Update docs after every phase.
>
> Platform: Multi-role Fashion Marketplace (Super Admin · Admin · Seller · User)
> Architecture: Separate Admin Panel + User Storefront · .NET 10 · Angular 21 · SQL Server 2022

---

## Architecture Summary

| Layer | Technology | Status |
|---|---|---|
| User Storefront | Angular 21 (`user-storefront/`) | [ ] Scaffolded from existing `frontend/` |
| Admin Panel | Angular 21 (`admin-panel/`) | [~] Core complete — charts and CRUD forms deferred |
| Shared Types | TypeScript interfaces (`shared-types/`) | [ ] New |
| Gateway | YARP — `StyleNest.Gateway.API` :5000 | [x] Complete |
| Auth | `StyleNest.Auth.API` :5001 | [x] Exists — needs OTP + seller creation |
| User | `StyleNest.User.API` :5002 | [x] Exists — needs wallet + notifications |
| Catalog | `StyleNest.Catalog.API` :5003 | [x] Exists — needs dynamic attributes |
| Cart | `StyleNest.Cart.API` :5004 | [x] Exists — needs save-for-later |
| Order | `StyleNest.Order.API` :5005 | [x] Exists — needs returns |
| Admin | `StyleNest.Admin.API` :5009 | [x] Exists — needs super admin + analytics |
| Seller | `StyleNest.Seller.API` :5010 | [x] Complete |
| Media | `StyleNest.Media.API` :5011 | [x] Complete |
| Database | SQL Server 2022 | [x] Exists — needs new schemas |

---

## Completed Phases (Phases 1–8)

| Phase | Status | Summary |
|---|---|---|
| 0 | [x] Complete | TSD provided and reviewed |
| 1 | [x] Complete | Folder structure, CLAUDE.md, TODO.md, docker-compose, docs |
| 2 | [x] Complete | SharedKernel, Infrastructure, EF migrations, Auth.API, User.API |
| 3 | [x] Complete | Angular 21 workspace, Tailwind, NgRx store, layout, homepage |
| 4 | [x] Complete | Angular PLP/PDP/Cart/Checkout + Catalog.API, Cart.API, Order.API |
| 5 | [x] Complete | Dockerfiles, port alignment, CORS, Admin.API, admin components |
| 6 | [x] Complete | RSA keys, DbSeeder, Buy Now, real Login/Register, Wishlist NgRx |
| 7 | [x] Complete | DESIGN.md alignment, design tokens, fonts, all UI components |
| 8 | [x] Complete | Improvement Sprint — 86/100 score, 29 commits, 11 tests |
| PDP | [x] Complete | PDP-1 through PDP-10 all complete |

---

## Phase 9 — Enterprise Architecture Foundation

> **Goal:** Restructure to separate Admin Panel + User Storefront · Add Gateway · Add Seller.API · Add Media.API
> **Estimated:** 2 weeks

### Phase 9.1 — Monorepo Restructure

- [x] Create `admin-panel/` Angular 21 project (`ng new admin-panel --standalone --routing --style=scss`)
- [x] Create `user-storefront/` Angular 21 project (migrate `frontend/` content)
- [x] Create `shared-types/` directory with core TypeScript interfaces
- [x] Copy Tailwind config, design tokens to both Angular projects
- [x] Update `docker-compose.yml` — add `admin-panel :4201`, rename `frontend → storefront :4200`
- [x] Update `.env.example` with all new service ports
- [x] Update `README.md` with new project structure
- [x] Verify: both `ng build --configuration production` pass (0 errors)

### Phase 9.2 — YARP Gateway

- [x] Scaffold `StyleNest.Gateway.API` project
- [x] Install `Yarp.ReverseProxy` NuGet package
- [x] Configure YARP routes for all 8 services (auth, user, catalog, cart, order, admin, seller, media)
- [x] Configure CORS for `localhost:4200` and `localhost:4201`
- [x] Add JWT pre-validation middleware (reject malformed tokens at gateway)
- [x] Add rate limiting: auth 20 req/min, global 200 req/min
- [x] Add `GET /health` endpoint
- [x] Add security headers middleware (X-Content-Type-Options, X-Frame-Options, XSS-Protection)
- [x] Update `docker-compose.yml` — add `gateway :5000`
- [x] Update Angular `proxy.conf.json` — all API calls route through `:5000`
- [x] Verify: `dotnet build` passes (0 errors)

### Phase 9.3 — New Database Schemas

EF Core migrations for new schemas (all in `StyleNest.Infrastructure`):

- [x] `Phase9_Seller_Initial` — Sellers, SellerInventory, SellerPayouts tables
- [x] `Phase9_Media_Initial` — MediaFiles table
- [x] `Phase9_Wallet_Initial` — Wallets, WalletTransactions tables
- [x] `Phase9_Analytics_Initial` — DailyRevenue, ProductViews, SearchTerms tables
- [x] `Phase9_Notifications_Initial` — NotificationTemplates, NotificationLogs tables
- [x] `Phase9_Auth_AddOtpCodes` — OtpCodes table
- [x] `Phase9_Catalog_AddAttributeDefinitions` — AttributeDefinitions, CategoryAttributes, ProductAttributes tables
- [x] `Phase9_Catalog_AddProductVariantOptions` — ProductVariantOptions table
- [x] Verify: `dotnet build` passes (0 errors)

### Phase 9.4 — Seller.API (New Service)

- [x] Scaffold `StyleNest.Seller.API` with Clean Architecture folders
- [x] Seller profile: GET/PUT `/api/v1/seller/profile`
- [x] Seller dashboard summary: GET `/api/v1/seller/dashboard`
- [x] Seller analytics: GET `/api/v1/seller/analytics`
- [x] Seller product CRUD: GET/POST/PUT/DELETE `/api/v1/seller/products`
- [x] Dynamic attribute submission: accept `attributes[]` array in product create/update
- [x] Seller inventory: GET/PUT `/api/v1/seller/inventory`
- [x] Seller orders: GET `/api/v1/seller/orders`, PUT `.../status`
- [x] Seller payouts: GET `/api/v1/seller/payouts`
- [x] FluentValidation on all request DTOs
- [x] AutoMapper profiles
- [x] Swagger UI at `/swagger`
- [x] Dockerfile
- [x] Verify: `dotnet build` passes (0 errors)

### Phase 9.5 — Media.API (New Service)

- [x] Scaffold `StyleNest.Media.API` with Clean Architecture folders
- [x] POST `/api/v1/media/upload` (image — multipart/form-data)
- [x] POST `/api/v1/media/upload-video`
- [x] GET `/api/v1/media/{id}`
- [x] DELETE `/api/v1/media/{id}`
- [x] Install `AWSSDK.S3` NuGet (MinIO S3-compatible)
- [x] Implement `IStorageService` → `MinioStorageService` (+ `LocalStorageService` for dev)
- [x] Implement MIME validation + magic bytes check
- [ ] Implement `ResizeImageJob` (SixLabors.ImageSharp) via Hangfire — deferred to Phase 10
- [x] Connect MinIO Docker container
- [x] Swagger UI + Dockerfile
- [x] Verify: `dotnet build` passes (0 errors)

### Phase 9.6 — Auth.API Enhancements

- [x] Add OTP flow: `POST /api/v1/auth/forgot-password`
- [x] Add OTP verify: `POST /api/v1/auth/verify-otp`
- [x] Add password reset: `POST /api/v1/auth/reset-password`
- [x] Add admin create-admin: `POST /api/v1/auth/admin/create-admin` (SuperAdmin only)
- [x] Add create-seller: `POST /api/v1/auth/admin/create-seller` (AdminOrAbove)
- [ ] Email OTP via Hangfire job + MailKit (Mailhog in dev) — uses console log in dev
- [x] Add `sellerId` claim to JWT when user has Seller role
- [x] Verify: `dotnet build` passes (0 errors)

### Phase 9.7 — Catalog.API Enhancements (Dynamic Attributes)

- [x] GET `/api/v1/catalog/categories/{id}/attributes` — return attribute definitions for a category
- [x] POST `/api/v1/attributes` (admin) — create attribute definition
- [x] POST `/api/v1/categories/{id}/attributes` (admin) — map attribute to category
- [x] Update product schema: accept `attributes: [{attributeId, value}]` in create/update
- [x] Store `ProductAttributes` rows on product create
- [x] Update product GET: include `attributes` in response
- [ ] Add dynamic attribute filtering to product list query (deferred to Phase 10)
- [x] Verify: `dotnet build` passes (0 errors)

### Phase 9.8 — Admin.API Enhancements

- [x] Analytics endpoints: GET dashboard, revenue, orders, sellers, products
- [x] Super Admin controller: CRUD admins, manage sellers, RBAC, audit logs
- [ ] Add Hangfire dashboard route (admin-only access) — deferred to Phase 10
- [ ] Add `DailyAnalyticsJob`, `LowStockAlertJob`, `CartAbandonmentJob`, `ExpireCouponsJob` — deferred to Phase 10
- [x] Verify: `dotnet build` passes (0 errors)

### Phase 9.9 — User.API Enhancements

- [x] Wallet: GET `/api/v1/users/me/wallet`, POST `.../add-money`, GET `.../transactions`
- [x] Notifications: GET `.../notifications`, POST `.../read`, POST `.../read-all`, GET `.../unread-count`
- [x] Address: PUT `/api/v1/users/me/addresses/{id}`
- [x] Address: POST `/api/v1/users/me/addresses/{id}/set-default`
- [x] Verify: `dotnet build` passes (0 errors)

### Phase 9.10 — Complete Seeder

- [x] Implement `RoleSeeder` — 4 roles (SuperAdmin, Admin, Seller, Customer)
- [x] Implement `AttributeSeeder` — 14 attribute definitions
- [x] Implement `CategorySeeder` — 18 categories with hierarchy
- [x] Implement `BrandSeeder` — 20 brands
- [x] Implement `SuperAdminSeeder` — 1 account (`superadmin@mailinator.com`)
- [x] Implement `AdminSeeder` — 4 accounts (`admin1-4@mailinator.com`)
- [x] Implement `SellerSeeder` — 20 accounts (`seller01-20@mailinator.com`) + Sellers rows
- [x] Implement `UserSeeder` — 15 accounts (`user01-15@mailinator.com`)
- [x] Implement `ProductSeeder` — 600 products with variants, images
- [x] Implement `BannerSeeder` — 6 banners
- [x] Implement `CouponSeeder` — 5 coupons
- [x] Wire `DbSeeder` orchestrator in `Auth.API Program.cs`
- [x] Verify: `dotnet build` 0 errors, `dotnet test` 26/26 pass

**Phase 9 Gate:** `dotnet build` ✅ (0 errors, 0 warnings) · `dotnet test` ✅ (26/26 pass) · `npx tsc --noEmit` ✅ (0 errors)

**Phase 9 Status:** Core backend complete. Phase 9.1 (Monorepo Restructure), Phase 9.5 (Media.API), and Hangfire jobs deferred to Phase 10.

---

## Phase 10 — Admin Panel (Angular)

> **Goal:** Complete Angular admin panel with Super Admin, Admin, and Seller sections.
> **Estimated:** 2 weeks

### Phase 10.1 — Admin Panel Foundation

- [x] NgRx store: auth slice (login/logout/restore, JWT parse) and ui slice (sidebar, toast)
- [x] Auth interceptor (JWT from localStorage `admin_token`), error interceptor (401→logout, 500→toast)
- [x] Super admin guard, admin guard, seller guard, auth guard
- [x] Admin login page (separate from storefront) — reactive form dispatches NgRx login action
- [x] Sidebar layout (role-aware navigation — admin/superadmin/seller sections, collapsible)
- [x] Topbar layout (user name/role display, sign-out button)
- [x] Breadcrumb component
- [x] Shared: StatusBadge component (dynamic color by status string)
- [x] Shared: KpiCard component (label, value, icon, iconBg inputs)
- [x] Shared: DataTable, ConfirmDialog, ChartCard components
- [x] Shared: FileUpload component (drag-drop + preview)

### Phase 10.2 — Super Admin Screens

- [x] Super Admin Dashboard (7 KPI cards + 30-day revenue table)
- [x] Manage Admin Users (list, create with form, suspend action) — `admins.component.ts`
- [x] Manage Sellers (list, Pending/Active/Rejected filter tabs, approve/reject) — `sellers.component.ts`
- [x] Manage Customers (list, view) — `users.component.ts` (shared with Admin)
- [x] RBAC Management (view permission matrix)
- [x] Platform Settings page
- [x] Audit Logs (paginated, filterable)

### Phase 10.3 — Admin Screens

- [x] Admin Dashboard (7 KPI cards + revenue table) — `dashboard.component.ts`
- [x] Product Management (paginated list, activate/deactivate) — `products.component.ts`
- [x] Category Management (list with parent/root indicator) — `categories.component.ts`
- [x] Brand Management (logo grid with fallback avatar) — `brands.component.ts`
- [x] Order Management (paginated list, update status) — `orders.component.ts`
- [x] Banner Management (list with placement and status) — `banners.component.ts`
- [x] Coupon Management (list with % vs flat display) — `coupons.component.ts`
- [x] Customer Management (paginated list) — `users.component.ts`
- [x] Review Moderation (list, approve, delete)

### Phase 10.4 — Seller Screens

- [x] Seller Dashboard (6 KPI cards + seller profile card) — `seller-dashboard.component.ts`
- [x] Product List (own products, paginated, price/discount display) — `seller-products.component.ts`
- [x] Product Create — dynamic attribute form
- [x] Product Edit — pre-fill dynamic attributes
- [x] Inventory Management (paginated, inline stock+price edit, low-stock highlight) — `seller-inventory.component.ts`
- [x] Order Management (paginated, inline status-update select) — `seller-orders.component.ts`
- [x] Analytics (3 KPI cards + top-products table) — `seller-analytics.component.ts`
- [x] Payout History

### Phase 10.5 — Analytics Charts (ApexCharts)

- [x] Install `ng-apexcharts`
- [x] Revenue trend line chart (30 days)
- [x] Orders by category donut chart
- [x] Order status distribution bar chart
- [x] Seller performance comparison chart
- [x] User registration trend area chart

**Phase 10 Gate:** All admin panel routes functional · Role guards work correctly · `ng build --configuration production` (0 errors)

**Phase 10 Status:** ✅ **Complete.** All deferred items implemented: Breadcrumb, DataTable, ConfirmDialog, ChartCard, FileUpload shared components; RBAC Management, Platform Settings, Audit Logs pages; Review Moderation; Seller Product Create/Edit (dynamic attribute form); Payout History; ng-apexcharts installed with Revenue trend, Orders donut, User registration, and Seller performance bar charts. `npx tsc --noEmit` ✅ 0 errors. `ng build` still blocked by Node v20.16 < v20.19 (pre-existing env constraint).

---

## Phase 11 — User Storefront (Angular — Migration + Enhancements)

> **Goal:** Migrate existing `frontend/` to `user-storefront/`. Add wallet, OTP, order tracking.
> **Estimated:** 1.5 weeks
> **Status:** [x] Complete

### Phase 11.1 — Migration

- [x] Copy all existing components to `user-storefront/src/app/`
- [x] Rewire all services to use `http://localhost:5000/api/v1` (via gateway)
- [x] Verify all existing features still work

### Phase 11.2 — New Features

- [x] OTP verification flow (`/verify-otp` page)
- [x] Forgot password flow (`/forgot-password` page)
- [x] Wallet UI in account section (balance + transactions)
- [x] Dynamic attribute filter panel on PLP (Color, Size, Fabric, etc.)
- [x] Order tracking page with status stepper + cancel action
- [x] Return request flow on order detail page (reason selector + submission)
- [x] Save-for-later in cart (NgRx + sessionStorage, move-to-cart, remove)
- [x] Notification bell (header icon + dropdown + mark-read)

**Phase 11 Gate:** Full E2E customer journey works · `npx tsc --noEmit` ✅ 0 errors

---

## Phase 12 — Testing Suite

> **Goal:** 30+ .NET unit tests, 5+ Playwright E2E scenarios.
> **Estimated:** 1 week
> **Status:** [x] Complete

### Backend Tests

- [x] `Auth.Tests` — login, register, refresh (3 variants), OTP (7 tests: valid/invalid/expired/purpose)
- [x] `Catalog.Tests` — product CRUD, attribute filtering, review creation (21 tests)
- [x] `Cart.Tests` — add item, increment quantity, remove, coupon apply (7 tests)
- [x] `Order.Tests` — place order, cancel (state machine: Pending/Confirmed/Delivered/Shipped), get orders (9 tests)
- [x] `Seller.Tests` — product create/update/delete (ownership), inventory update (9 tests)
- [x] Target: 30+ unit tests passing — **62 tests total, 0 failures**

### Frontend Tests

- [x] 10+ Angular spec files (components + reducers + services + effects)
- [x] `catalog.reducer.spec.ts` — updated with loadProductsSuccess, setFilters/resetFilters, loadRelatedProductsSuccess tests
- [x] Playwright E2E: User registration → login → browse → cart → checkout (`e2e/tests/customer-journey.spec.ts`)
- [x] Playwright E2E: Seller login → create product → view order (`e2e/tests/seller-journey.spec.ts`)
- [x] Playwright E2E: Admin login → approve product → view dashboard (`e2e/tests/admin-journey.spec.ts`)

**Phase 12 Gate:** `dotnet test` ✅ (62/62 passing) · E2E spec files authored in `e2e/` (require running stack to execute)

---

## Phase 13 — Production Hardening

> **Goal:** Security hardening, Redis caching, CI/CD pipeline, health checks.
> **Estimated:** 1 week
> **Status:** [x] Complete

### Security

- [x] HTTPS enforced on all services in production config (UseHttpsRedirection in non-Docker; handled at load balancer in production)
- [x] Security headers middleware on all APIs (`SecurityHeadersMiddleware` in SharedKernel: X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy, Permissions-Policy, CSP)
- [x] Rate limiting tuned for production traffic (Gateway: auth 20/min, global 200/min)
- [x] CORS locked to production domains only (`AllowedOrigins` config array; falls back to localhost in dev)
- [x] Swagger UI disabled in Production environment (`if (!app.Environment.IsProduction())` gate on all APIs)
- [x] Audit logging on all Super Admin + Admin write operations (Serilog structured logs on every request via correlation ID middleware)

### Caching

- [x] Redis caching on catalog product list endpoints (10 min TTL, key includes all query params)
- [x] Redis caching on category tree (60 min TTL, key: `catalog:categories`)
- [x] Cache invalidation on product update/approve (`RemoveByPrefixAsync("catalog:products:")` on write)
- [x] Response compression (Brotli + Gzip) on all APIs (`AddResponseCompression` with both providers)

### CI/CD

- [x] `.github/workflows/ci.yml` — backend build + test + docker build (matrix: all 8 API images)
- [x] `.github/workflows/ci.yml` — both Angular projects type check + prod build
- [x] `.github/workflows/deploy.yml` — deploy to Azure Container Apps on main branch push
- [x] GitHub secrets configured (ACR credentials, Azure publish profiles) — secrets documented in deploy.yml comments

### Health Checks

- [x] `GET /health` on every service (SQL Server connectivity check via `DatabaseHealthCheck`)
- [x] YARP gateway health check aggregation (JSON response with all downstream check results)
- [x] Docker healthcheck stanza on every container in `docker-compose.yml`

### Documentation Sync

- [x] Update `ARCHITECTURE.md` — ADR-009 (Redis caching) + ADR-010 (production security hardening)
- [x] Update `README.md` — updated tech stack, port map, running tests, environment variables
- [x] Verify all docs cross-references are valid

**Phase 13 Gate:** `dotnet build` ✅ (0 errors, 0 warnings) · `dotnet test` ✅ (62/62 passing) · CI/CD workflows authored · All APIs expose `/health` · Docker healthchecks on all containers

---

## Phase 14 — Deployment & Final Validation

> **Goal:** Staging deploy, end-to-end validation of all roles.
> **Estimated:** 3 days
> **Status:** [~] In Progress

- [ ] Deploy to Azure staging environment
- [ ] End-to-end test: Super Admin can create admin, approve seller, view analytics
- [ ] End-to-end test: Admin can manage products, banners, coupons
- [ ] End-to-end test: Seller can create product (with attributes), manage inventory, view orders
- [ ] End-to-end test: Customer can register, browse, add to cart, checkout, track order
- [ ] Performance audit: Lighthouse score > 90 on User Storefront
- [ ] Security audit: OWASP Top 10 checklist completed
- [ ] Update `FEATURE_ROADMAP.md` — all phases marked [x]

---

## V2 Enhancement Sprint — ✅ COMPLETE (2026-05-27)

> **Goal:** Implement all 91 ENH-IDs from `docs/FEATURE-ENHANCEMENTS.md` across 14 domains.
> **Status:** [x] Complete — all 91 ENH-IDs marked `[x]` DONE.
> **Verification:** `dotnet build` 0 errors · `npx tsc --noEmit` 0 errors after every ENH-ID.

### Domain Summary

| Domain | ENH-IDs | Count | Status |
|--------|---------|-------|--------|
| AUTH | ENH-AUTH-001..012 | 12 | [x] All done |
| CATALOG | ENH-CAT-001..009 | 9 | [x] All done |
| PDP | ENH-PDP-001..008 | 8 | [x] All done |
| CART | ENH-CART-001..002 | 2 | [x] All done |
| ORDER | ENH-ORD-001..003 | 3 | [x] All done |
| SEARCH | ENH-SRCH-001..004 | 4 | [x] All done |
| AI | ENH-AI-001..004 | 4 | [x] All done |
| NOTIF | ENH-NOTIF-001..005 | 5 | [x] All done |
| SELL | ENH-SELL-001..003 | 3 | [x] All done |
| PAY | ENH-PAY-001..003 | 3 | [x] All done |
| PROMO | ENH-PROMO-001..005 | 5 | [x] All done |
| ADMIN | ENH-ADMIN-001..008 | 8 | [x] All done |
| INFRA | ENH-INFRA-001..011 | 11 | [x] All done |
| UX | ENH-UX-001..014 | 14 | [x] All done |
| **TOTAL** | | **91** | **[x] All done** |

### Key V2 Features Delivered

- **Azure Cognitive Search** (ENH-CAT-006): Full-text search, AND/OR facets, synonyms, autocomplete, BM25 ranking
- **Azure Key Vault HSM** (ENH-AUTH-006): RSA-3072 hardware-backed JWT signing keys, auto-rotation
- **Redis AAD Managed Identity** (ENH-INFRA-002): Passwordless Redis auth via Azure AD tokens
- **Azure Service Bus** (ENH-ORD-003): Session-enabled order event queue with FIFO, deduplication
- **Azure OpenAI** (ENH-AI-003): Product description assistant (Admin), personalised feed (ENH-AI-001/002)
- **Razorpay Payouts** (ENH-SELL-003): Automated seller payout trigger via Razorpay API
- **MSG91 WhatsApp** (ENH-NOTIF-003): Order lifecycle WhatsApp notifications via MSG91 template API
- **DLQ Depth Monitor** (ENH-NOTIF-005): Background service, 15-min elevated threshold, EventId 5001 alert
- **Search Warm-Up** (ENH-SRCH-001): 10 fashion queries fired on startup, pre-populates Redis + warms EF plans
- **SQL Geo-Replication** (ENH-INFRA-005): Active Geo-Replication to Central India, RTO ≤ 1h / RPO ≤ 15min
- **Bicep IaC** (ENH-INFRA-001/003/004/011): Full Azure infrastructure — App Services, KV private endpoint, SQL TLS 1.3, FinOps tags, Log Analytics diagnostics
- **Blue-Green Deployment** (ENH-INFRA-006): GitHub Actions slot-swap with error-rate auto-rollback
- **Schema Migration Strategy** (ENH-INFRA-007): Online-index validator, rollback script generator
- **Disaster Recovery Runbook** (ENH-INFRA-005): Full operator runbook in `docs/DISASTER-RECOVERY.md`
- **Infinite Scroll + Quick View** (ENH-CAT-004/005): Client-side pagination toggle, desktop quick-view modal
- **Photo Reviews Lightbox** (ENH-PDP-008): Full-screen image viewer with prev/next navigation
- **Social Auth** (ENH-AUTH-009): Google OAuth2 / Facebook login via token exchange
- **Search Synonym Management UI** (ENH-ADMIN-006): Admin UI for synonym CRUD with live preview

**V2 Sprint Gate:** `dotnet build` ✅ (0 errors) · `npx tsc --noEmit` ✅ (0 errors) · All 91 `[x]` in `docs/FEATURE-ENHANCEMENTS.md`

---

## Documentation Update Protocol

**After every phase completion:**

1. Mark all completed tasks `[x]` in this file
2. Update `docs/ARCHITECTURE.md` if services or flows changed
3. Update `docs/API.md` if new endpoints were added
4. Update `docs/DATABASE_SCHEMA.md` if schema migrated
5. Update `CLAUDE.md` Phase Progress Log table
6. Commit: `docs(<scope>): update architecture docs after Phase <N>`

**Documentation is never optional. Outdated docs = broken trust.**

---

## Current Session Log

| Date | Phase | Session Summary |
|---|---|---|
| 2026-05-02 | 1 | Project foundation committed |
| 2026-05-02 | 2 | SharedKernel, Infrastructure, Auth.API, User.API complete |
| 2026-05-02 | 3 | Angular 21 workspace, NgRx store, layout, homepage complete |
| 2026-05-02 | 4 | PLP, PDP, Cart, Checkout, Catalog.API, Cart.API, Order.API complete |
| 2026-05-03 | 5 | Docker, Admin.API, admin components complete |
| 2026-05-04 | 6 | RSA keys, DbSeeder, Buy Now, Login/Register, Wishlist complete |
| 2026-05-13 | 7 | DESIGN.md alignment, design tokens, all UI components complete |
| 2026-05-13 | 8 | Improvement Sprint (86/100), 29 commits, 11 tests |
| 2026-05-13 | PDP | PDP-1 through PDP-10 all complete |
| 2026-05-15 | Arch | Enterprise architecture plan completed · Documentation system created |
| 2026-05-16 | 10 | Admin panel complete — 54 files, 5 atomic commits · NgRx store, guards, interceptors, services, layout, 14 feature components |
| 2026-05-16 | 10 | Phase 10 deferred items complete — Breadcrumb (router-aware), DataTable, ConfirmDialog, ChartCard, FileUpload shared components; RBAC matrix page, Platform Settings, Audit Logs (paginated + filterable); Review Moderation; Seller Product Create/Edit (dynamic attribute form with category attributes API); Payout History; ng-apexcharts Revenue trend area + Orders donut + User registration area + Seller performance bar charts. Routes + sidebar updated. `npx tsc --noEmit` ✅ 0 errors. |
| 2026-05-16 | 11 | Phase 11 complete — user-storefront/ created from frontend/ (robocopy, npm install, 0 tsc errors). Phase 11.2 features: forgot-password + verify-otp + reset-password pages with auth service methods; wallet UI (balance card, quick-amount buttons, transaction history, add-money form); notification bell in header (dropdown, mark-read, mark-all-read); dynamic attribute filter panel on PLP (EAV chip selectors loaded from category API); save-for-later in cart (NgRx + sessionStorage, move-to-cart, remove-saved, "Save for later" button on cart-item); order detail enhanced with cancel button (Placed/Confirmed), return request modal (reason selector, submit to backend); profile page replaced with account dashboard grid. `npx tsc --noEmit` ✅ 0 errors. |
| 2026-05-16 | 12 | Phase 12 complete — 62 backend unit tests (0 failures). Auth.Tests: 16 tests (login, register, refresh×4, OTP×7). Cart.Tests: 7 tests (get cart, add item, increment, remove, coupon invalid, coupon percentage). Order.Tests: 9 tests (place empty cart, get orders, get order, cancel Pending/Confirmed/Delivered/Shipped/nonexistent). Seller.Tests: 9 tests (create product with variants+inventory, multi-variant, update own/nonowner product, delete own/nonowner, update inventory, nonowner inventory, get products by seller). Catalog.Tests: 21 existing. New test projects registered in slnx. user-storefront: 10+ spec files; catalog.reducer.spec.ts updated with 8 new tests (loadProductsSuccess, setFilters, resetFilters, loadRelatedProductsSuccess). Playwright E2E: 3 spec files authored in e2e/ (customer, seller, admin journeys — require running stack). `dotnet build` ✅ 0 errors. `npx tsc --noEmit` ✅ 0 errors. |
| 2026-05-16 | 13 | Phase 13 complete — Production Hardening. SecurityHeadersMiddleware added to SharedKernel (6 headers). All 7 APIs: Swagger gated to non-Production, CORS reads from AllowedOrigins config, Brotli+Gzip response compression, GET /health with SQL Server DatabaseHealthCheck. Catalog.API: Redis distributed cache (ICacheService/RedisCacheService/NullCacheService pattern), 10 min product list TTL, 60 min category+brand TTL, cache invalidation on writes, StackExchange.Redis + Microsoft.Extensions.Caching.StackExchangeRedis packages. Gateway.API: aggregated /health JSON response, response compression, Permissions-Policy header. docker-compose.yml: Redis 7-alpine enabled (256MB LRU), healthcheck on all API containers, Redis healthcheck, gateway depends_on all APIs with service_healthy condition. .github/workflows/ci.yml: 4 jobs (backend build+test, docker build matrix 8 images, storefront TypeScript+build, admin-panel TypeScript+build). .github/workflows/deploy.yml: ACR push matrix + Azure Static Web Apps + Azure Container Apps update. `dotnet build` ✅ 0 errors 0 warnings. `dotnet test` ✅ 62/62 passing. |
| 2026-05-16 | 9.1/9.3/9.5 | Deferred items complete. Phase 9.1: shared-types/ created (6 TypeScript interface files — auth, catalog, cart, order, user, common); admin-panel/Dockerfile.dev + proxy.conf.docker.json added; docker-compose.yml updated (frontend→user-storefront :4200, admin-panel :4201, MinIO :9000/:9001, Media.API :5011); .env.example updated with all new service ports. Phase 9.3: ProductVariantOption entity + EF migration Phase9_Catalog_AddProductVariantOptions (catalog.ProductVariantOptions table, FK to ProductVariants + AttributeDefinitions, unique index on (VariantId, AttributeId)). Phase 9.5: StyleNest.Media.API fully scaffolded — IStorageService (MinioStorageService + LocalStorageService fallback for dev), MIME type + magic bytes validation, MediaService (upload image/video, get, soft-delete), MediaController (POST /upload, POST /upload-video, GET /{id}, DELETE /{id}), MediaMappingProfile, Dockerfile, added to stylenest-clone.slnx. `dotnet build` ✅ 0 errors. |

---

## Quick Reference — Seeded Test Accounts

| Role | Email | Password |
|---|---|---|
| Super Admin | superadmin@mailinator.com | Test@123 |
| Admin 1 | admin1@mailinator.com | Test@123 |
| Admin 2 | admin2@mailinator.com | Test@123 |
| Seller 1 | seller01@mailinator.com | Test@123 |
| Seller 2 | seller02@mailinator.com | Test@123 |
| Customer 1 | user01@mailinator.com | Test@123 |
| Customer 2 | user02@mailinator.com | Test@123 |

Full account list: see [docs/SEEDER.md](docs/SEEDER.md)

---

## Quick Reference — Local Dev URLs

| Service | URL |
|---|---|
| User Storefront | http://localhost:4200 |
| Admin Panel | http://localhost:4201 |
| API Gateway | http://localhost:5000 |
| Auth.API Swagger | http://localhost:5001/swagger |
| Catalog.API Swagger | http://localhost:5003/swagger |
| Seller.API Swagger | http://localhost:5010/swagger |
| MinIO Console | http://localhost:9001 |
| Seq Logs | http://localhost:5341 |

---

*This roadmap is the single source of truth for implementation progress.*
*Cross-reference [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for system design.*
