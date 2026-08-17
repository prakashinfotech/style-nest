# CLAUDE.md — StyleNest E-Commerce Clone (.NET / Angular / SQL)
# Read this file at the start of EVERY Claude Code session. No exceptions.

## Project
Multi-category fashion & lifestyle marketplace — StyleNest.
Angular 21 SPA · .NET Core 10 Web API · SQL Server 2022 · Azure.

## Approved Stack

### V1 — Frontend (Phases 1–3)
- @angular/core@21
- @angular/router@21
- @angular/forms@21
- @angular/material@21
- @angular/cdk@21
- rxjs@7
- @ngrx/store@21
- @ngrx/effects@21
- @ngrx/entity@21
- tailwindcss@3
- lucide-angular

### V1 — Backend (Phases 1–3)
- dotnet 10
- Microsoft.EntityFrameworkCore (EF Core 9)
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.AspNetCore.Identity.EntityFrameworkCore
- AutoMapper
- FluentValidation
- Serilog
- Swashbuckle.AspNetCore (Swagger/OpenAPI)
- Microsoft.AspNetCore.Authentication.JwtBearer

### V2 — Cloud Scale (Phase 5+ — DO NOT install before Phase 5)
- Azure.ServiceBus
- Azure.Storage.Blobs
- Azure.Extensions.AspNetCore.Configuration.Secrets (Key Vault)
- StackExchange.Redis
- Azure.Search.Documents (Cognitive Search)
- Razorpay .NET SDK
- Azure.AI.OpenAI
- Microsoft.ApplicationInsights.AspNetCore

## Design Tokens (updated per DESIGN.md §2.1 — StyleNest Fashion)
| Token            | Hex       | CSS Variable        | Tailwind Class  |
|------------------|-----------|---------------------|-----------------|
| primary-navy     | #1C2B4A   | --sn-navy         | bg-navy         |
| accent-red       | #E31837   | --sn-red          | bg-red / text-red |
| cta-blue         | #0071C2   | --sn-blue         | bg-blue         |
| bg               | #F5F5F5   | --sn-light-gray   | bg-bg           |
| card-white       | #FFFFFF   | --sn-white        | bg-card         |
| text-dark        | #1A1A1A   | --sn-dark         | text-dark       |
| text-muted       | #757575   | --color-muted       | text-muted      |
| mid-gray         | #9E9E9E   | --sn-mid-gray     | text-mid-gray   |
| border           | #E0E0E0   | --sn-border       | border-border   |
| luxury-gold      | #C9A84C   | --sn-gold         | text-gold       |
| success-green    | #2E7D32   | --sn-success      | text-success    |

## Responsive Breakpoints (Tailwind)
- default (mobile-S): 320px–479px — single column, bottom nav
- sm:  480px — 2-col grid option
- md:  768px — top nav, 3-col grid, filter drawer
- lg:  1024px — mega-menu, 4-col grid, sticky filter sidebar
- xl:  1280px — full layout, 4–5 col grid
- 2xl: 1440px — max-width 1440px centred

## Code Rules — Angular
- TypeScript strict mode — ZERO `any` anywhere
- No HttpClient calls inside components — Services ONLY
- Every component: standalone: true + ChangeDetectionStrategy.OnPush
- One component per file, one responsibility per component
- No subscribe() in component classes — use AsyncPipe only
- NgRx: all side effects in Effects, never in components or services directly
- Mobile-first: every component must include responsive Tailwind classes
- Lazy-load every feature module via loadComponent / loadChildren

## Code Rules — .NET Core
- C# nullable reference types enabled — ZERO #nullable disable
- No raw SQL — EF Core LINQ only; all schema changes via Migrations
- One controller per resource, one service per domain concern
- Repository pattern: IRepository<T> → EfRepository<T>
- DTOs for all API inputs/outputs — never expose entities directly
- FluentValidation for all request DTOs
- AutoMapper profiles for entity ↔ DTO mapping
- Serilog structured logging with correlation IDs on every request
- Do NOT modify anything outside the file being generated

## EF Core Migration Naming
Pattern: `dotnet ef migrations add <Phase>_<Context>_<Change>`
Example: `Phase2_Auth_AddUsers`

## Current Phase
**Phase 14 — Deployment & Final Validation (In Progress) | V2 Enhancement Sprint — ✅ COMPLETE (all 91 ENH-IDs done, 2026-05-27)**
See `FEATURE_ROADMAP.md` for the phase-by-phase task tracker (Phases 1–14 + V2 sprint).
See `docs/FEATURE-ENHANCEMENTS.md` for the V2+ enhancement backlog (91 items, all `[x]` DONE).
See `docs/ARCHITECTURE.md` for the complete system architecture.
See `docs/DISASTER-RECOVERY.md` for the DR runbook (RTO ≤ 1h, RPO ≤ 15 min).

## Documentation System (Single Source of Truth)
| File | Purpose |
|---|---|
| `FEATURE_ROADMAP.md` | Phase-by-phase task tracker (replaces TODO.md for Phase 9+) |
| `docs/FEATURE-ENHANCEMENTS.md` | V2+ enhancement backlog — 91 ENH-IDs, all traceable to SOW/TSD |
| `docs/ARCHITECTURE.md` | System architecture, ADRs, sequence diagrams |
| `docs/TECH_STACK.md` | All packages, versions, rationale |
| `docs/DATABASE_SCHEMA.md` | Complete SQL schema for all 11 database schemas |
| `docs/ROLES_RBAC.md` | Permission matrix, policy definitions, guard config |
| `docs/BACKEND_ARCHITECTURE.md` | .NET service internals, all endpoints |
| `docs/FRONTEND_ARCHITECTURE.md` | Angular project structure, patterns, components |
| `docs/MEDIA_UPLOAD.md` | File upload pipeline, MinIO, ImageSharp |
| `docs/SEEDER.md` | All seeded accounts (40), categories, brands, products |
| `docs/DEPLOYMENT.md` | Docker Compose, CI/CD, Azure production architecture |
| `docs/SECURITY.md` | Threat model, auth security, RBAC implementation |
| `docs/PERFORMANCE.md` | Redis caching, query optimization, Angular bundle |
| `docs/API.md` | Full endpoint reference (all controllers) |
| `docs/DESIGN.md` | Design tokens, typography, component specifications |

## Phase Progress Log
| Phase | Status      | Summary |
|-------|-------------|---------|
| 0     | Complete    | TSD provided and reviewed |
| 1     | Complete    | Folder structure, CLAUDE.md, TODO.md, docker-compose, docs committed (f96ed3f) |
| 2     | Complete    | SharedKernel, Infrastructure, EF migrations, Auth.API (JWT RS256), User.API (profile/addresses/wishlist) |
| 3     | Complete    | Angular 21 workspace, Tailwind, NgRx store (auth/cart/catalog/ui), layout, homepage components |
| 4     | Complete    | Angular PLP/PDP/Cart/Checkout + Catalog.API, Cart.API, Order.API (full build 0 errors) |
| 5     | Complete    | Dockerfiles (all 6 APIs + user-panel), port alignment 5001–5009, CORS on all APIs, Admin.API (full Clean Architecture), Angular admin components. ng build 0 errors. |
| 6     | Complete    | RSA dev keys (appsettings.Development.json all 6 APIs), DbSeeder (100 products + admin user), Buy Now endpoint, real Login/Register forms, Wishlist NgRx slice (toggle), Buy Now NgRx flow → order-confirmed page. dotnet build 0 errors, ng build production 0 errors. |
| 7     | Complete    | DESIGN.md alignment: design tokens, fonts (Playfair Display + DM Sans), header/footer redesign, hero carousel, category banners, promo banners, brand-logo-strip, product-card, add-to-cart-panel, size-selector refreshed. ng build production 0 errors. |
| 8     | Complete    | Improvement Sprint — 86/100 final score. 29 Conventional Commits. dotnet build 0 errors, dotnet test 11/11, npx tsc 0 errors. ng test blocked by Node v20.16 < v20.19. See IMPROVEMENT_SPRINT.md. |
| 9     | Complete    | Enterprise Architecture: YARP Gateway :5000, Seller.API :5010, Media.API :5011, 9 new EF migrations, OTP flow, dynamic attributes, wallet, notifications, 600 seeded products. dotnet build 0 errors, dotnet test 62/62. |
| 10    | Complete    | Admin Panel (Angular 21): NgRx store, guards, interceptors, 14 feature components, ApexCharts (revenue/orders/users/seller charts), RBAC matrix, Audit Logs, Review Moderation. npx tsc 0 errors. |
| 11    | Complete    | User Storefront migration: forgot-password, OTP verify, wallet UI, dynamic attribute filters, order tracking stepper, return request, save-for-later, notification bell. npx tsc 0 errors. |
| 12    | Complete    | Testing Suite: 62 backend unit tests (0 failures) across Auth/Cart/Order/Seller/Catalog. 10+ Angular spec files. 3 Playwright E2E specs (customer, seller, admin journeys). |
| 13    | Complete    | Production Hardening: SecurityHeadersMiddleware, Redis caching (10min/60min TTL), Brotli+Gzip compression, CI/CD GitHub Actions (8 Docker images), Azure Container Apps deploy workflow, /health on all services. |
| 14    | In Progress | Deployment & Final Validation — Azure staging deploy, E2E role validation, Lighthouse audit, OWASP checklist. |
| V2    | ✅ Complete  | V2 Enhancement Sprint — all 91 ENH-IDs implemented across 14 domains (Auth, Catalog, PDP, Cart, Order, Search, AI, Notification, Seller, Payment, Promo, Admin, Infra, UX). Completed 2026-05-27. |

## Git Commit Convention (Phase 8 — Mandatory)
Every commit from Phase 8 onward MUST follow Conventional Commits format.
```
<type>(<scope>): <short description>      ← max 72 chars, imperative mood
```
| Type       | When to use |
|------------|-------------|
| `feat`     | New feature or endpoint |
| `fix`      | Bug fix |
| `refactor` | Code restructure, no behaviour change |
| `test`     | Adding or fixing tests |
| `docs`     | README, ARCHITECTURE.md, comments |
| `style`    | Tailwind/CSS/formatting only |
| `chore`    | Config, tooling, migrations |

**Scopes:** `auth` `catalog` `cart` `order` `user` `admin` `seller` `shared` `user-panel` `infra` `docs`

Examples:
```
feat(catalog): add global exception middleware with ProblemDetails
feat(catalog): add pagination to GET /api/v1/products
test(auth): add unit tests for AuthService login/register flows
fix(cart): resolve quantity update overwrite on concurrent requests
docs(root): rewrite README with full local setup guide
```

Rules:
- One logical change per commit — NEVER commit an entire phase as one commit
- Each commit must build and pass `dotnet build` / `npx tsc --noEmit` independently
- Minimum 4 commits per working day during the sprint

## Phase 8 — Improvement Sprint Focus Areas
| Priority | Area              | Key Goal |
|----------|-------------------|----------|
| P1       | Code Quality      | Global exception middleware; FluentValidation on all 14 controllers; unified error responses |
| P1       | Git Discipline    | Conventional Commits on every commit; atomic changes; 25+ total sprint commits |
| P2       | Testing           | 15+ .NET unit tests; 4+ Angular spec files; >40% ng coverage |
| P2       | UI/UX             | HTTP error interceptor → toast; empty-state component; form inline errors |
| P2       | Database & APIs   | Pagination on list endpoints; API versioning /api/v1/; ProblemDetails standard |
| P2       | Functional        | Order status stepper; admin real metrics; address CRUD verified; coupon feedback |
| P3       | Documentation     | README local-setup guide; ARCHITECTURE.md sequence diagrams; API.md payloads |
| P3       | Ownership         | End-to-end feature verification; self-scored re-evaluation on Day 7 |

## Feature Enhancement Tracking
See `docs/FEATURE-ENHANCEMENTS.md` for the full V2+ enhancement backlog (91 items across 14 domains).

**V2 Enhancement Sprint status: ✅ ALL 91 ENH-IDs COMPLETE (2026-05-27)**

All items are now `[x]` DONE. No items remain in `[ ]` TODO or `[~]` IN-PROGRESS state.

Domains covered: AUTH (12) · CATALOG (9) · PDP (8) · CART (2) · ORDER (3) · SEARCH (4) · AI (4) · NOTIF (5) · SELL (3) · PAY (3) · PROMO (5) · ADMIN (8) · INFRA (11) · UX (14)

## Multi-Agent Test Protocol
Rules that apply to every ENH-ID marked `Parallel-testable: YES` in FEATURE-ENHANCEMENTS.md:

1. A TEST agent prompt block (in `docs/TEST-AGENT-PROMPTS.md`) MUST be defined **before** implementation begins.
2. The TEST agent runs in a **separate Claude Code session** — never in the same session as the IMPL agent.
3. Every TEST agent prompt MUST include: ENH-ID · acceptance criteria from SOW v2.1 · exact NFR thresholds from SOW §4 (p95 targets, error rates, CWV values).
4. Test output is written to `docs/test-reports/<ENH-ID>-report.md` and status updated in FEATURE-ENHANCEMENTS.md.
5. An ENH-ID is only marked `[x] DONE` after both IMPL and TEST agents sign off.

## Vibe Coding Guards (READ BEFORE EVERY PROMPT)
1. One component / one controller per prompt — never batch
2. Review every generated file before `dotnet run` or `ng serve`
3. Every commit is atomic — one logical change, Conventional Commits format (see above)
4. V2 libraries are FORBIDDEN before Phase 5
5. Never modify files outside the scope of the current prompt
6. Test at 375px mobile on every Angular component
7. If context window grows large: new session → open with "Read CLAUDE.md"
8. After every change: `dotnet build` (backend) or `npx tsc --noEmit` (user-panel) must pass
