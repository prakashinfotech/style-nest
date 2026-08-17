# StyleNest E-Commerce Platform

A production-grade multi-category fashion & lifestyle marketplace — **StyleNest** — built with Angular 21, .NET 10 microservices, SQL Server 2022, Redis 7, and the full Azure cloud stack. Features an Angular admin panel, a user storefront, YARP API gateway, JWT RS256 auth, Azure Cognitive Search, Azure Key Vault HSM keys, Azure Service Bus order events, AI-powered product descriptions, Redis AAD Managed Identity auth, blue-green deployments, SQL geo-replication (DR), Docker orchestration, GitHub Actions CI/CD, and a comprehensive test suite.

> **V2 Enhancement Sprint complete** — all 91 ENH-IDs implemented across 14 domains. See [docs/FEATURE-ENHANCEMENTS.md](docs/FEATURE-ENHANCEMENTS.md).

---

## Tech Stack

| Layer      | Technology                                                                                            |
|------------|-------------------------------------------------------------------------------------------------------|
| Frontend   | Angular 21, NgRx 21, Tailwind CSS 3, Angular Material 21, ApexCharts                                 |
| Backend    | .NET 10, ASP.NET Core Web API (9 microservices + YARP gateway), EF Core 9                            |
| Database   | SQL Server 2022 (11 schemas, 30+ tables) + Active Geo-Replication (Central India DR)                 |
| Cache      | Redis 7 — catalog cache (10–60 min TTL) · Azure Cache for Redis AAD Managed Identity auth            |
| Search     | Azure Cognitive Search — full-text, BM25 ranking, facets, synonyms, autocomplete                     |
| Auth       | JWT RS256 · ASP.NET Core Identity · OTP flow · Google / Facebook OAuth2 · Azure KV HSM RSA-3072 keys |
| Messaging  | Azure Service Bus — session-enabled order event queue (FIFO, deduplication)                          |
| AI         | Azure OpenAI — product description assistant, personalised product feed                               |
| Notifications | MSG91 WhatsApp API · Email OTP (MailKit) · In-app notification bell                               |
| Payments   | Razorpay Payout API — automated seller payouts                                                        |
| Storage    | MinIO / Azure Blob Storage via Media.API                                                              |
| IaC        | Bicep — App Services, Key Vault (private endpoint), SQL TLS 1.3, Log Analytics, FinOps tags          |
| Container  | Docker / docker-compose (17 containers)                                                               |
| Testing    | xUnit + Moq + FluentAssertions (62 tests) · Playwright E2E (3 journeys)                              |
| CI/CD      | GitHub Actions — build, test, Docker push, Azure Container Apps deploy (blue-green + auto-rollback)  |

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0+ | `dotnet --version` |
| [Node.js](https://nodejs.org/) | 22.12+ | Angular CLI 21 requires ≥ 22. Use `nvm install 22` if needed. |
| [Docker Desktop](https://www.docker.com/products/docker-desktop) | Latest | Required for the Docker path |
| SQL Server 2022 | 2022 | Local install or use the Docker SQL container |
| Angular CLI | 21+ | `npm install -g @angular/cli@21` |

---

## Service & Port Map

| Service              | Port     | Swagger UI (Dev only)                  |
|----------------------|----------|----------------------------------------|
| Gateway.API (YARP)   | **5000** | —                                      |
| Auth.API             | **5001** | http://localhost:5001/swagger          |
| User.API             | **5002** | http://localhost:5002/swagger          |
| Catalog.API          | **5003** | http://localhost:5003/swagger          |
| Cart.API             | **5004** | http://localhost:5004/swagger          |
| Order.API            | **5005** | http://localhost:5005/swagger          |
| Admin.API            | **5009** | http://localhost:5009/swagger          |
| Seller.API           | **5010** | http://localhost:5010/swagger          |
| Media.API            | **5011** | http://localhost:5011/swagger          |
| User Panel           | **4200** | http://localhost:4200                  |
| Admin Panel          | **4201** | http://localhost:4201                  |
| SQL Server           | **1433** | —                                      |
| Redis                | **6379** | —                                      |
| MinIO Console        | **9001** | http://localhost:9001                  |

All API routes are versioned under `/api/v1/`. Swagger UI is disabled in Production (`ASPNETCORE_ENVIRONMENT=Production`).

> **Note:** `Notification.API` and `Payment.API` are scaffolded and reserved for a future phase.

### Health Checks

Every service exposes `GET /health` returning JSON:

```json
{ "status": "Healthy", "checks": [{ "name": "sqlserver", "status": "Healthy" }] }
```

---

## Local Setup — with Docker (recommended)

```bash
# 1. Clone the repo
git clone <repo-url>
cd style-nest-ecommerce-clone-dotnet-angular-sql

# 2. Copy the env template and fill in values
cp .env.example .env
# Edit .env: set SQLSERVER_SA_PASSWORD and JWT key paths

# 3. Start the full stack (SQL Server + Redis + MinIO + all APIs + Angular)
docker compose up --build
```

Open http://localhost:4200 (User Panel) or http://localhost:4201 (Admin Panel).

> **First run:** EF Core migrations run automatically on startup.  
> **JWT RS256 keys** must be generated and referenced in `.env` — see the Environment Variables section below.  
> **Redis** starts automatically — Catalog.API auto-detects and enables caching when `ConnectionStrings:Redis` is set.  
> **MinIO** starts automatically on ports `9000` (API) and `9001` (console).

---

## Local Setup — without Docker

### Step 1 — Start SQL Server

Option A — use only the SQL Server container:
```bash
docker compose up sqlserver -d
```

Option B — use a local SQL Server 2022 instance (Windows Auth or SA login).

### Step 2 — Generate RSA key pair (JWT RS256)

```bash
openssl genrsa -out keys/private.pem 2048
openssl rsa -in keys/private.pem -pubout -out keys/public.pem
```

Set paths in each API's `appsettings.Development.json`:
```json
{
  "Jwt": {
    "PrivateKeyPath": "./keys/private.pem",
    "PublicKeyPath":  "./keys/public.pem",
    "Issuer":   "https://stylenest-auth.local",
    "Audience": "stylenest-spa"
  }
}
```

> Only **Auth.API** needs `PrivateKeyPath`. All other APIs need only `PublicKeyPath`.

### Step 3 — Apply EF Core migrations

```bash
cd backend
dotnet ef database update \
  --project src/Shared/StyleNest.Infrastructure \
  --startup-project src/Services/StyleNest.Auth.API
```

This creates all schemas (`auth`, `catalog`, `commerce`, `orders`, `admin`) and seeds:
- 100 sample products across categories
- Default admin user: `admin@stylenest.com` / `Admin@123`

### Step 4 — Run the APIs

Open terminals for each service (or use your IDE's multi-run config):

```bash
cd backend && dotnet run --project src/Services/StyleNest.Gateway.API   # :5000
cd backend && dotnet run --project src/Services/StyleNest.Auth.API      # :5001
cd backend && dotnet run --project src/Services/StyleNest.User.API      # :5002
cd backend && dotnet run --project src/Services/StyleNest.Catalog.API   # :5003
cd backend && dotnet run --project src/Services/StyleNest.Cart.API      # :5004
cd backend && dotnet run --project src/Services/StyleNest.Order.API     # :5005
cd backend && dotnet run --project src/Services/StyleNest.Admin.API     # :5009
cd backend && dotnet run --project src/Services/StyleNest.Seller.API    # :5010
cd backend && dotnet run --project src/Services/StyleNest.Media.API     # :5011
```

### Step 5 — Run the Angular applications

```bash
# User Storefront (port 4200)
cd user-panel
npm install
npx ng serve --proxy-config proxy.conf.json

# Admin Panel (port 4201) — separate terminal
cd admin-panel
npm install
npx ng serve
```

Open http://localhost:4200 (user storefront) or http://localhost:4201 (admin panel).

---

## Default Credentials

All accounts below are seeded automatically by `DbSeeder` on first startup. Password for all seeded accounts is `Test@123`.

| Role        | Email                      | Password  | Notes |
|-------------|----------------------------|-----------|-------|
| Super Admin | superadmin@mailinator.com  | Test@123  | Full platform access; manages admins & sellers |
| Admin 1     | admin1@mailinator.com      | Test@123  | CMS, products, orders, coupons, banners |
| Admin 2     | admin2@mailinator.com      | Test@123  | Same as Admin 1 |
| Seller 1    | seller01@mailinator.com    | Test@123  | First of 20 seeded seller accounts |
| Seller 2    | seller02@mailinator.com    | Test@123  | Second seeded seller (seller02–20 follow same pattern) |
| Customer 1  | user01@mailinator.com      | Test@123  | First of 15 seeded customer accounts |
| Customer 2  | user02@mailinator.com      | Test@123  | Second seeded customer (user01–15 follow same pattern) |

> **New user registration** is enabled via `/register` on the storefront.  
> Full account list (40 accounts): see [docs/SEEDER.md](docs/SEEDER.md).

---

## Running Tests

### .NET unit tests

```bash
cd backend
dotnet test
```

**62 tests pass** across 5 test projects:

| Project | Tests |
|---------|-------|
| StyleNest.Auth.Tests | 16 |
| StyleNest.Catalog.Tests | 21 |
| StyleNest.Cart.Tests | 7 |
| StyleNest.Order.Tests | 9 |
| StyleNest.Seller.Tests | 9 |

### Angular type-check (TypeScript compilation)

```bash
# User Panel
cd user-panel && npx tsc --noEmit

# Admin Panel
cd admin-panel && npx tsc --noEmit
```

### Angular unit tests

```bash
cd user-panel
npx ng test --watch=false --code-coverage
```

> **Note:** Angular CLI 21 requires Node.js ≥ 22. If on an older version, use `npx tsc --noEmit` as a compilation-check proxy.

### Playwright E2E tests

Playwright specs live in `e2e/tests/`. The full stack must be running first:

```bash
cd e2e
npm install
npx playwright test
```

Three journeys covered: user registration flow, product browse & add-to-cart, and checkout.

---

## Environment Variables

Copy `.env.example` to `.env` and fill in the values. **Never commit `.env` to git.**

| Variable | Required | Description |
|----------|----------|-------------|
| `SQLSERVER_SA_PASSWORD` | Yes | SQL Server SA password |
| `SQLSERVER_HOST` | Yes | SQL Server hostname (default: `localhost`) |
| `SQLSERVER_PORT` | Yes | SQL Server port (default: `1433`) |
| `SQLSERVER_DB` | Yes | Database name (default: `StyleNestDb`) |
| `ConnectionStrings__DefaultConnection` | Yes | Full EF Core connection string |
| `ConnectionStrings__Redis` | No | Redis connection string (e.g. `localhost:6379`). Omit to disable catalog caching. |
| `AllowedOrigins__0` | Prod | First allowed CORS origin (e.g. `https://stylenest.com`) |
| `AllowedOrigins__1` | Prod | Second allowed CORS origin (e.g. `https://admin.stylenest.com`) |
| `Jwt__PrivateKeyPath` | Auth.API only | Path to RSA private key `.pem` |
| `Jwt__PublicKeyPath` | All APIs | Path to RSA public key `.pem` |
| `Jwt__Issuer` | Yes | JWT issuer claim (e.g. `https://stylenest-auth.local`) |
| `Jwt__Audience` | Yes | JWT audience claim (e.g. `stylenest-spa`) |
| `Jwt__AccessTokenExpiryMinutes` | No | Default: `15` |
| `Jwt__RefreshTokenExpiryDays` | No | Default: `7` |
| `MinIO__Endpoint` | No | MinIO endpoint (default: `localhost:9000`) |
| `MinIO__AccessKey` | No | MinIO access key |
| `MinIO__SecretKey` | No | MinIO secret key |

**V2 Azure services (optional — graceful no-op when absent):**

| Variable | Required | Description |
|----------|----------|-------------|
| `AzureCognitiveSearch__Endpoint` | V2 | Azure Cognitive Search endpoint URL |
| `AzureCognitiveSearch__ApiKey` | V2 | ACS admin key (or use Managed Identity) |
| `Jwt__KeyVaultUri` | V2 | Azure Key Vault URI for RSA-HSM JWT key |
| `Jwt__KeyVaultKeyName` | V2 | KV key name (default: `stylenest-jwt-rsa3072`) |
| `ServiceBus__ConnectionString` | V2 | Azure Service Bus connection string |
| `Redis__UseManagedIdentity` | V2 | `true` to use AAD token auth for Redis |
| `AzureOpenAI__Endpoint` | V2 | Azure OpenAI endpoint URL |
| `AzureOpenAI__ApiKey` | V2 | Azure OpenAI API key |
| `Msg91WhatsApp__AuthKey` | V2 | MSG91 auth key for WhatsApp notifications |
| `RazorpayPayout__KeyId` | V2 | Razorpay payout key ID |
| `RazorpayPayout__KeySecret` | V2 | Razorpay payout key secret |

> **Local development:** Leave all V2 variables at their `REPLACE_` defaults. Services auto-detect and fall back to local/in-memory implementations.  
> **Production note:** Set `ASPNETCORE_ENVIRONMENT=Production` to disable Swagger UI and lock CORS to the `AllowedOrigins` list.

---

## Project Structure

```
.
├── backend/
│   ├── src/
│   │   ├── Services/
│   │   │   ├── StyleNest.Gateway.API/        # YARP reverse proxy — routes all client traffic
│   │   │   ├── StyleNest.Auth.API/           # Register, login, refresh, logout, OTP, password reset
│   │   │   ├── StyleNest.User.API/           # Profile, addresses, wishlist, wallet, notifications
│   │   │   ├── StyleNest.Catalog.API/        # Products, categories, brands, attributes, Redis cache
│   │   │   ├── StyleNest.Cart.API/           # Cart CRUD, coupon apply, save-for-later
│   │   │   ├── StyleNest.Order.API/          # Place order, buy-now, order history, cancel, tracking, returns
│   │   │   ├── StyleNest.Admin.API/          # Banners, coupons, admin orders/products/users, analytics, RBAC
│   │   │   ├── StyleNest.Seller.API/         # Seller products, inventory, analytics, payouts, onboarding
│   │   │   ├── StyleNest.Media.API/          # File upload pipeline (MinIO / Azure Blob), MIME validation
│   │   │   ├── StyleNest.Notification.API/   # (scaffolded — reserved for Phase 14)
│   │   │   └── StyleNest.Payment.API/        # (scaffolded — reserved for Phase 14)
│   │   └── Shared/
│   │       ├── StyleNest.Infrastructure/     # EF Core DbContext, EfRepository<T>, migrations, Redis, seeders
│   │       └── StyleNest.SharedKernel/       # BaseEntity, Result<T>, IRepository<T>, middleware, security headers
│   ├── tests/
│   │   ├── StyleNest.Auth.Tests/             # xUnit — AuthService (16 tests)
│   │   ├── StyleNest.Catalog.Tests/          # xUnit — CatalogService + Validators (21 tests)
│   │   ├── StyleNest.Cart.Tests/             # xUnit — CartService (7 tests)
│   │   ├── StyleNest.Order.Tests/            # xUnit — OrderService (9 tests)
│   │   └── StyleNest.Seller.Tests/           # xUnit — SellerService (9 tests)
│   └── stylenest-clone.slnx
├── user-panel/                              # User-facing Angular 21 storefront
│   └── src/app/
│       ├── core/          # Guards, interceptors, services, models
│       ├── store/         # NgRx slices: auth, cart, catalog, wishlist, order, ui
│       ├── features/      # Lazy pages: home, PLP, PDP, cart, checkout, auth, account, wallet, OTP
│       ├── layout/        # Header (notification bell), footer, bottom-nav, mega-menu
│       └── shared/        # Reusable components (empty-state, snackbar, skeleton, attribute-filter)
├── admin-panel/                             # Admin / Seller Angular 21 panel
│   └── src/app/
│       ├── core/          # Guards (super-admin, admin, seller, auth), interceptors, admin API service
│       ├── store/         # NgRx slices: auth, ui
│       ├── features/      # Dashboard, products, orders, users, coupons, banners, seller, RBAC, audit-logs
│       ├── layout/        # Sidebar (role-aware), topbar, breadcrumb
│       └── shared/        # KPI cards, ApexCharts, status badge, confirm dialog, file upload, data table
├── shared-types/                            # TypeScript interface contracts shared across both Angular apps
│   ├── auth.types.ts      # AuthUser, LoginRequest, RegisterRequest, OtpRequest
│   ├── catalog.types.ts   # Product, Category, Brand, AttributeDefinition
│   ├── cart.types.ts      # Cart, CartItem, Coupon
│   ├── order.types.ts     # Order, OrderItem, OrderStatus
│   ├── user.types.ts      # UserProfile, Address, WalletTransaction, Notification
│   └── common.types.ts    # PagedResult<T>, ApiResponse<T>, SortOption
├── e2e/
│   └── tests/             # Playwright E2E — 3 journey specs (customer, seller, admin)
├── infra/
│   └── bicep/
│       ├── main.bicep                        # Subscription-scoped orchestrator (ENH-INFRA-001/003/004/011)
│       ├── modules/
│       │   ├── app-service.bicep             # TLS 1.3, diagnostic settings, FinOps tags
│       │   ├── app-service-plan.bicep        # Zone-redundant plan
│       │   ├── keyvault.bicep                # Premium SKU, private endpoint, RBAC
│       │   ├── keyvault-hsm-key.bicep        # RSA-3072 HSM key, 2-year rotation policy
│       │   ├── sql-server.bicep              # BusinessCritical, TDE, ATP, ZRS backup
│       │   └── sql-geo-replication.bicep     # Active Geo-Replication → Central India (ENH-INFRA-005)
│       └── parameters/
│           └── production.bicepparam         # Parameter template (REPLACE_ placeholders)
├── scripts/
│   └── migration/
│       ├── validate-migration.ps1            # Online-index validator (ENH-INFRA-007)
│       └── generate-rollback.ps1             # EF rollback script generator
├── docs/
│   ├── ARCHITECTURE.md           # System design, sequence diagrams, ADRs (ADR-001 to ADR-010)
│   ├── API.md                    # Full endpoint reference (all controllers)
│   ├── DATABASE_SCHEMA.md        # Complete SQL schema — all 11 schemas
│   ├── ROLES_RBAC.md             # Permission matrix, policy definitions, guard config
│   ├── DESIGN.md                 # Design tokens, typography, breakpoints
│   ├── DEPLOYMENT.md             # Docker Compose, CI/CD, Azure Container Apps architecture
│   ├── SECURITY.md               # Threat model, auth security, RBAC, security headers
│   ├── PERFORMANCE.md            # Redis caching, query optimization, Angular bundle analysis
│   ├── MEDIA_UPLOAD.md           # File upload pipeline, MinIO, ImageSharp, MIME validation
│   ├── SEEDER.md                 # All seeded accounts (40+), categories, brands, 600 products
│   ├── DISASTER-RECOVERY.md      # DR runbook — RTO ≤ 1h, RPO ≤ 15min, quarterly drill checklist
│   ├── FEATURE-ENHANCEMENTS.md   # V2 backlog — 91 ENH-IDs, all [x] DONE
│   ├── TEST-AGENT-PROMPTS.md     # Multi-agent test prompts for all parallel-testable ENH-IDs
│   ├── BACKEND_ARCHITECTURE.md   # .NET service internals, all endpoints per service
│   ├── FRONTEND_ARCHITECTURE.md  # Angular project structure, patterns, components
│   └── TECH_STACK.md             # All packages, versions, rationale
├── docker-compose.yml
├── .env.example
├── FEATURE_ROADMAP.md     # Phase-by-phase task tracker (Phases 0–13 ✅, 14 🔄, V2 ✅)
└── CLAUDE.md              # AI coding rules (read every session)
```

---

---

## V2 Cloud Enhancements (All Complete ✅)

All 91 items from `docs/FEATURE-ENHANCEMENTS.md` are implemented. Each is gracefully degraded — local dev works without Azure credentials (REPLACE_ placeholder values in appsettings.json trigger fallbacks).

### Search & Discovery
| ENH-ID | Feature | Endpoint / File |
|--------|---------|-----------------|
| ENH-CAT-006 | **Azure Cognitive Search** — full-text, BM25, AND/OR facets, synonyms, autocomplete | `GET /api/v1/search` |
| ENH-SRCH-001 | Search warm-up — 10 fashion queries fired on startup to pre-populate Redis cache | `SearchWarmUpBackgroundService` |
| ENH-SRCH-002 | DB-backed autocomplete typeahead (prefix match across 4 sources) | `GET /api/v1/search/suggest` |
| ENH-SRCH-003/004 | Synonym management · Search analytics | `SearchSynonymsController` · `SearchAnalyticsService` |
| ENH-ADMIN-006 | Synonym management UI (Admin) — CRUD with live preview | Admin panel |

### Auth & Security
| ENH-ID | Feature | File |
|--------|---------|------|
| ENH-AUTH-006 | **Azure Key Vault HSM RSA-3072** — hardware-backed JWT signing keys, auto-rotate P30D | `KeyVaultRsaKeyProvider.cs` |
| ENH-AUTH-009 | **Social Auth** — Google OAuth2 / Facebook token exchange → JWT | `SocialAuthController.cs` |
| ENH-AUTH-007 | Polly-retry JWKS key provider (15-min cache, circuit breaker) | `JwksKeyProvider.cs` |
| ENH-AUTH-008 | Refresh token rotation with reuse detection | `TokenRotationService.cs` |
| ENH-AUTH-012 | MFA TOTP for Admin / SuperAdmin (RFC 6238 TOTP) | `MfaController.cs` |

### Cloud Infrastructure
| ENH-ID | Feature | File |
|--------|---------|------|
| ENH-INFRA-001/003/004/011 | **Bicep IaC** — App Services, KV private endpoint, SQL TLS 1.3, Log Analytics, FinOps tags | `infra/bicep/` |
| ENH-INFRA-002 | **Redis AAD Managed Identity** — passwordless Azure Cache auth | `RedisServiceCollectionExtensions.cs` |
| ENH-INFRA-005 | **SQL Active Geo-Replication** — Central India DR, RTO ≤ 1h / RPO ≤ 15min | `infra/bicep/modules/sql-geo-replication.bicep` · `docs/DISASTER-RECOVERY.md` |
| ENH-INFRA-006 | **Blue-Green Deployment** — ACA slot swap, error-rate auto-rollback | `.github/workflows/blue-green-deploy.yml` |
| ENH-INFRA-007 | Schema migration validator + rollback script generator | `scripts/migration/` |
| ENH-INFRA-008/009 | Application Insights · Azure Front Door CDN | `TelemetryService.cs` |

### Messaging & Events
| ENH-ID | Feature | File |
|--------|---------|------|
| ENH-ORD-003 | **Azure Service Bus** — session-enabled order events (FIFO + deduplication) | `OrderSessionBusService.cs` |
| ENH-NOTIF-003 | **MSG91 WhatsApp** — order lifecycle notifications via template API | `WhatsAppNotificationService.cs` |
| ENH-NOTIF-005 | **DLQ Depth Monitor** — background alert (EventId 5001) at 100-message / 15-min threshold | `DlqDepthMonitorService.cs` |

### AI & Personalisation
| ENH-ID | Feature | File |
|--------|---------|------|
| ENH-AI-003 | **Azure OpenAI product description** — luxury fashion copywriter in Admin | `ProductDescriptionAssistant.cs` |
| ENH-AI-001/002 | **Personalised feed** — collaborative-filter scoring + trending fallback | `PersonalisedFeedService.cs` |
| ENH-AI-004 | Related product rails — Similar, Complete the Look, FBT | `RelatedProductsService.cs` |

### Payments
| ENH-ID | Feature | File |
|--------|---------|------|
| ENH-SELL-003 | **Razorpay Payout** — automated seller payout trigger, INR→paise, fund account creation | `SellerPayoutService.cs` |
| ENH-PAY-001/002/003 | Payment gateway integration, webhook processing, refund flow | `PaymentController.cs` |

### UX Enhancements (Angular)
| ENH-ID | Feature |
|--------|---------|
| ENH-CAT-004 | Quick View Modal — desktop hover / mobile tap, ATC without PDP nav |
| ENH-CAT-005 | Infinite Scroll + Pagination Toggle (client-side, persisted preference) |
| ENH-PDP-008 | Photo Reviews Lightbox — full-screen, prev/next, swipe |
| ENH-UX-001..014 | Progressive Web App, skeleton screens, empty states, toast system, accessibility (WCAG 2.1 AA) |

---

## Disaster Recovery

See [docs/DISASTER-RECOVERY.md](docs/DISASTER-RECOVERY.md) for the full operator runbook.

| Metric | Target | Implementation |
|--------|--------|----------------|
| RTO | ≤ 1 hour | Forced SQL failover < 30 min; ACA images already warm in DR |
| RPO | ≤ 15 min | Active Geo-Replication typical lag < 5 s |
| Quarterly drill | ✓ | Checklist in `docs/DISASTER-RECOVERY.md` |

---

## Swagger / OpenAPI

Each API exposes Swagger UI at `/swagger` in Development mode:

| API          | URL                             |
|--------------|---------------------------------|
| Auth.API     | http://localhost:5001/swagger   |
| User.API     | http://localhost:5002/swagger   |
| Catalog.API  | http://localhost:5003/swagger   |
| Cart.API     | http://localhost:5004/swagger   |
| Order.API    | http://localhost:5005/swagger   |
| Admin.API    | http://localhost:5009/swagger   |
| Seller.API   | http://localhost:5010/swagger   |
| Media.API    | http://localhost:5011/swagger   |

All error responses conform to **RFC 7807 ProblemDetails** (`application/problem+json`).

---

## Architecture Overview

```
Browser → Gateway.API (:5000, YARP)
              │
              ├── /api/v1/auth/**     → Auth.API    (:5001)
              ├── /api/v1/users/**    → User.API    (:5002)
              ├── /api/v1/catalog/**  → Catalog.API (:5003) → Redis cache
              ├── /api/v1/cart/**     → Cart.API    (:5004)
              ├── /api/v1/orders/**   → Order.API   (:5005)
              ├── /api/v1/admin/**    → Admin.API   (:5009)
              ├── /api/v1/seller/**   → Seller.API  (:5010)
              └── /api/v1/media/**    → Media.API   (:5011) → MinIO
```

All services share a single SQL Server 2022 instance via separate EF Core schemas. JWT RS256 tokens are issued by Auth.API and validated by every downstream service using the shared public key.

---

## Phase Progress

| Phase | Status      | Summary |
|-------|-------------|---------|
| 1     | ✅ Complete | Project foundation — folder structure, docker-compose, docs skeleton |
| 2     | ✅ Complete | Backend — SharedKernel, Infrastructure, EF Core migrations, Auth.API, User.API |
| 3     | ✅ Complete | Angular SPA — Tailwind, NgRx store, layout, homepage components |
| 4     | ✅ Complete | Feature pages — PLP, PDP, Cart, Checkout + Catalog.API, Cart.API, Order.API |
| 5     | ✅ Complete | Full-stack integration — Dockerfiles, port alignment, CORS, Admin.API, admin UI |
| 6     | ✅ Complete | RSA keys, DbSeeder (100 products), Buy Now, real Login/Register, Wishlist NgRx |
| 7     | ✅ Complete | DESIGN.md alignment — design tokens (Playfair + DM Sans fonts), all UI components refreshed |
| 8     | ✅ Complete | Improvement Sprint — 86/100 score, 29 Conventional Commits, global exception middleware, FluentValidation |
| 9     | ✅ Complete | Enterprise architecture — YARP Gateway, Seller.API, Media.API, 8 new EF schemas, seeder (600 products, 40 accounts) |
| 10    | ✅ Complete | Admin panel (Angular 21) — NgRx, role guards, 14 feature components, ApexCharts revenue/order/seller charts |
| 11    | ✅ Complete | User storefront migration — wallet, OTP/forgot-password, order tracking stepper, return flow, notification bell, save-for-later |
| 12    | ✅ Complete | Testing suite — 62 .NET unit tests (0 failures), 10+ Angular specs, 3 Playwright E2E journeys |
| 13    | ✅ Complete | Production hardening — security headers, Redis caching (10–60 min TTL), health checks, GitHub Actions CI/CD |
| 14    | 🔄 Pending  | Staging deploy, end-to-end UAT, performance audit (Lighthouse > 90), OWASP checklist |
| V2    | ✅ Complete | **V2 Enhancement Sprint** — all 91 ENH-IDs across 14 domains. Azure Search, KV HSM, Service Bus, OpenAI, Razorpay, WhatsApp, DR runbook, Bicep IaC, blue-green deploy. See below. |
