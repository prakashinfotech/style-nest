 

**TECHNICAL SPECIFICATION DOCUMENT**

**StyleNest E-Commerce Platform — Full-Stack Clone**

.NET Core  ·  Angular  ·  SQL Server  ·  Azure

 

| Document Type | Technical Specification — Vibe Coding Build |
| :---- | :---- |
| **Project Code** | ECM-TSTYLENEST-2026-001 |
| **Version** | v2.0 — Updated post Phase 13 Completion |
| **Date** | May 2026 |
| **Stack** | .NET Core 10 · Angular 21 · TypeScript · SQL Server 2022 · Redis 7 · Docker |
| **Approach** | Vibe Coding via Claude Code |
| **Status** | UPDATED — Phases 1–13 Complete · Phase 14 (Staging Deploy) Pending |
| **Classification** | CONFIDENTIAL — Internal Use Only |

ECM-TSTYLENEST-2026-001  |  E-Commerce Platform Technical Specification  |  CONFIDENTIAL

 

# **1\. Introduction & Purpose**

This Technical Specification Document (TSD) translates the Statement of Work (SOW) for the StyleNest E-Commerce Clone into a concrete, developer-ready blueprint. It defines every architectural decision, data model, API contract, component hierarchy, and phase-by-phase build sequence required to deliver a production-grade multi-category retail marketplace.

The build uses .NET Core 10 for backend microservices, Angular 21 for the single-page application frontend, and SQL Server 2022 as the primary relational database — all hosted on Microsoft Azure. The Vibe Coding approach powered by Claude Code compresses development timelines by 40–60% while maintaining production-quality output.

## **1.1 Document Scope**

This specification covers two interleaved layers of detail:

•       Functional Architecture: What the platform does — every user-facing feature including authentication, catalog, cart, checkout, order management, promotions, admin CMS, and seller portal.

•       Vibe Coding Implementation Blueprint: How the platform is built — the Claude Code phase structure, CLAUDE.md rule system, prompt discipline, and component patterns.

 

## **1.2 Reference Documents**

| Document | Key Contribution |
| :---- | :---- |
| StyleNest\_SOW.docx | All functional scope, milestones, commercial terms, and NFRs |
| bookingclonetechspec.pdf | Vibe Coding methodology template — Claude Code phase structure, CLAUDE.md system, design tokens |

 

 

# **2\. Vibe Coding Methodology with Claude Code**

Vibe Coding is a prompt-driven development paradigm where Claude Code acts as the primary code generator, and human engineers act as architects, reviewers, and orchestrators. The methodology is structured around a CLAUDE.md control file, phase-gated prompts, and a strict review-before-run discipline.

## **2.1 Core Principles**

| Principle | Implementation Rule |
| :---- | :---- |
| One component per prompt | Never ask Claude to generate more than one controller, service, component, or module per prompt. Broad prompts produce half-correct outputs. |
| CLAUDE.md-first | CLAUDE.md is auto-read at every Claude Code session start. Contains approved stack, code rules, design tokens, forbidden libraries, and current phase. Must be authored before any code generation. |
| Review before run | Every generated file must be reviewed before dotnet run or ng serve. AI-generated code is treated as a PR from a junior developer. |
| Commit at every milestone | A git commit is required at every deliverable milestone to prevent a single bad prompt from wiping hours of working code. |
| Phase gating | V2 libraries (Azure Service Bus, Stripe, SignalR, etc.) are listed in CLAUDE.md but gated. Claude must never install them before the phase that introduces them. |
| Mobile-first always | Every Angular component prompt must include responsive breakpoints using Tailwind or Angular CDK breakpoints. Desktop-only layouts are project failures, not polish items. |

 

## **2.2 CLAUDE.md Control File**

The CLAUDE.md file is the single most critical project artefact. It is auto-read at the start of every Claude Code session and prevents context drift across all phases and all developers.

| \# CLAUDE.md — StyleNest E-Commerce Clone (.NET / Angular / SQL) \# Read this file at the start of every Claude Code session.   \#\# Project Multi-category retail marketplace clone of StyleNest. Angular 21 SPA · .NET Core 10 Web API · SQL Server 2022 · Azure.   \#\# Approved Stack \#\#\# V1 — Frontend (Phases 0–2) @angular/core@21, @angular/router, @angular/forms, @angular/material, rxjs@7, @ngrx/store@21, @ngrx/effects, tailwindcss@3, lucide-angular \#\#\# V1 — Backend (Phases 1–2) dotnet@10, Microsoft.EntityFrameworkCore, EF Core SQL Server, AutoMapper, FluentValidation, Serilog, Swagger/OpenAPI, Microsoft.AspNetCore.Identity \#\#\# V2 — Cloud Scale (Phase 3+  — do NOT use before Phase 3\) Azure Service Bus, Azure Blob Storage, Azure Redis Cache, Azure Cognitive Search, Razorpay SDK, Azure Application Insights, Microsoft.Azure.OpenAI \#\# Design Tokens primary-navy: \#1A1A6B | accent-red: \#E4002B | bg: \#F5F5F5 \#\# Code Rules \- TypeScript strict mode — no \`any\` anywhere in Angular code \- C\# nullable reference types enabled — no \#nullable disable \- No HttpClient calls in Angular components — use Services only \- EF Core — no raw SQL queries; use Migrations for all schema changes \- One component per file, one responsibility per controller \#\# Current Phase: Phase 0 — Project Documents |
| :---- |

 

## **2.3 Version Philosophy: V1 vs V2**

| Dimension | V1 — Showcase Build | V2 — Production Extension |
| :---- | :---- | :---- |
| **Goal** | Demo-ready Angular SPA with mock catalog data | Real backend, auth, payments, live inventory |
| **Data** | 100% mock — TypeScript constants (50 products) | Live SQL Server / MongoDB, real SKU inventory |
| **Auth** | Bypassed — guest session assumed | Full JWT \+ OTP \+ Google OAuth (.NET Identity) |
| **Payments** | Simulated — Order Placed confirmation screen | Razorpay integration, real UPI/card/EMI flows |
| **AI Features** | None in V1 | Azure OpenAI — smart search, product summaries, recommendations |
| **Team** | 2–3 developers across 7 phases (\~2 weeks) | Full squad, 36-week multi-sprint delivery |

 

 

# **3\. Design System & Tokens**

All UI components across every phase must apply these design tokens consistently. Tokens are registered in tailwind.config.ts in Phase 1 and documented in docs/DESIGN.md before any Angular component is written.

## **3.1 Color Tokens**

Design tokens aligned with DESIGN.md §2.1 — StyleNest Fashion. Registered in `tailwind.config.ts` and as CSS custom properties in `styles.scss`.

| Token Name | Hex Value | CSS Variable | Tailwind Class | Usage |
| :---- | :---- | :---- | :---- | :---- |
| **Primary Navy** | \#1C2B4A | \--sn-navy | bg-navy | Header, mega-menu, footer, primary CTAs |
| **Accent Red** | \#E31837 | \--sn-red | bg-red / text-red | Sale labels, promo badges, Flash Sale timers |
| **CTA Blue** | \#0071C2 | \--sn-blue | bg-blue | Add to Cart, Buy Now, secondary CTA hover states |
| **Background** | \#F5F5F5 | \--sn-light-gray | bg-bg | Page background, PLP sidebar |
| **Card White** | \#FFFFFF | \--sn-white | bg-card | All card surfaces, modals, drawers |
| **Text Dark** | \#1A1A1A | \--sn-dark | text-dark | All body copy, product names, headings |
| **Text Muted** | \#757575 | \--color-muted | text-muted | Labels, secondary text, breadcrumbs, captions |
| **Mid Gray** | \#9E9E9E | \--sn-mid-gray | text-mid-gray | Placeholder text, disabled states |
| **Border** | \#E0E0E0 | \--sn-border | border-border | Card borders, dividers, input outlines |
| **Luxury Gold** | \#C9A84C | \--sn-gold | text-gold | StyleNest Cash wallet balance, luxury brand accents |
| **Success Green** | \#2E7D32 | \--sn-success | text-success | Order delivered, stock available, payment success |

 

## **3.2 Responsive Breakpoints**

| Breakpoint | Range | Layout Behaviour |
| :---- | :---- | :---- |
| Mobile S (default) | 320px – 479px | Single column, bottom nav bar, stacked product cards |
| Mobile L (sm:) | 480px – 767px | Single column, 2-column product grid option |
| Tablet (md:) | 768px – 1023px | Top nav, 3-column grid, side filter drawer, expanded PDP |
| Laptop (lg:) | 1024px – 1279px | Full mega-menu, 4-column grid, sticky filter sidebar |
| Desktop (xl:) | 1280px – 1439px | Full layout, 4–5 column grid, extended hero carousel |
| Wide (2xl:) | 1440px+ | Max-width 1440px centred; ambient gutter space used |

 

 

# **4\. Technology Stack**

## **4.1 Frontend — Angular 21 SPA (Implemented: Phases 3, 11)**

| Layer | Technology | Version | Rationale |
| :---- | :---- | :---- | :---- |
| Framework | Angular | **21** | Standalone components; OnPush change detection; control flow syntax (@if, @for) |
| Language | TypeScript | 5+ | Strict mode enforced; zero `any`; all API DTOs typed |
| Styling | Tailwind CSS \+ SCSS | **3.4** | Utility-first responsive styling; custom design tokens registered in tailwind.config.ts |
| Component Library | Angular Material | **21** | Accessible UI kit; CDK Dialog, Overlay, FocusTrap for modals/drawers |
| Icons | Lucide Angular | Latest | Typed imports; tree-shakeable |
| State Management | NgRx Store \+ Effects \+ Entity | **21** | Slices: auth, cart, catalog, wishlist, order, ui; devtools integration |
| HTTP Layer | Angular HttpClient \+ Interceptors | 21 | JWT injection, 401→logout, global toast-error interceptors |
| Routing | Angular Router | 21 | `loadComponent` / `loadChildren` lazy loading; route guards; can-activate |
| Forms | Reactive Forms | 21 | Checkout, auth, address, OTP, wallet forms; inline validation error display |
| Charts | ng-apexcharts | Latest | Revenue trend, orders donut, seller performance, user registration charts (admin panel) |
| Node.js Requirement | Node.js | **22.12+** | Angular CLI 21 requires ≥ Node 22 |

 

## **4.2 Backend — .NET Core 10 Web API (Implemented: Phases 2, 4, 5, 9)**

| Layer | Technology | Version | Purpose |
| :---- | :---- | :---- | :---- |
| Runtime | .NET | **10 (LTS)** | Cross-platform; controller-based routing; nullable reference types enforced |
| API Framework | ASP.NET Core Web API | 10 | RESTful APIs; all routes versioned under `/api/v1/`; RFC 7807 ProblemDetails error responses |
| Gateway | YARP Reverse Proxy | Latest | Routes all traffic through `:5000`; JWT pre-validation; rate limiting (auth 20/min, global 200/min) |
| ORM | Entity Framework Core | **9** | Code-first migrations; LINQ-only (no raw SQL); 11 schemas across SQL Server |
| Auth | ASP.NET Core Identity \+ JWT RS256 | — | Password hashing; 15-min access token; 7-day refresh token (httpOnly cookie) |
| Validation | FluentValidation | Latest | All request DTOs validated; on every controller across all 8 services |
| Mapping | AutoMapper | Latest | Profile-based DTO ↔ Entity mapping |
| API Docs | Swashbuckle / OpenAPI 3 | — | Swagger UI at `/swagger` (disabled in Production) |
| Logging | Serilog \+ correlation ID middleware | — | Structured JSON logging; per-request correlation IDs |
| Caching | StackExchange.Redis | Latest | Catalog product list (10 min TTL), categories (60 min TTL); NullCacheService fallback when Redis absent |
| Compression | Brotli \+ Gzip | — | Response compression on all APIs via `AddResponseCompression` |
| Security | SecurityHeadersMiddleware | — | X-Content-Type-Options, X-Frame-Options, XSS-Protection, CSP, Referrer-Policy, Permissions-Policy |
| Testing | xUnit \+ Moq \+ FluentAssertions | — | **62 tests** across 5 projects (Auth 16, Catalog 21, Cart 7, Order 9, Seller 9) |

 

## **4.3 Database — SQL Server 2022 (Implemented) \+ Supporting Stores**

| Store | Technology | Status | Responsibilities |
| :---- | :---- | :---- | :---- |
| Primary DB | SQL Server 2022 | **Implemented** | 11 EF Core schemas: auth, catalog, commerce, orders, payments, admin, seller, media, wallet, analytics, notifications |
| Cache | Redis 7 (Alpine) | **Implemented** | Catalog product list (10 min TTL); category tree (60 min TTL); cache invalidation on writes; 256MB LRU eviction |
| File Storage | MinIO (S3-compatible) | **Implemented** | Product images, seller assets; MIME + magic bytes validation; LocalStorageService fallback for dev |
| Catalog DB | Azure Cosmos DB | Phase 14 | Deferred — EAV attribute system in SQL Server covers V1 product variants |
| Search | Azure Cognitive Search | Phase 14 | Deferred — SQL Server LIKE-based search functional in V1 |
| Message Bus | Azure Service Bus | Phase 14 | Deferred — notification events use console log in dev; Hangfire jobs scaffolded |

 

## **4.4 V2 Cloud & Microservices Stack (Phase 3+ — DO NOT install before Phase 3\)**

| Layer | Technology & Notes |
| :---- | :---- |
| Payments | Razorpay SDK (.NET) \+ PayU failover; webhook reconciliation; COD; BNPL; PCI-DSS tokenised storage |
| Notifications | Azure Communication Services (email/SMS); FCM (push); WhatsApp via partner API; per-user preferences |
| AI / ML | Azure OpenAI (GPT-4o) \+ Azure Cognitive Search semantic ranking — product summaries, smart search, personalised feeds |
| Monitoring | Azure Application Insights APM \+ Sentry \+ Azure Monitor — distributed traces, error tracking, SLA alerting |
| IaC | Bicep / Terraform (Azure) — all Azure resources version-controlled; AKS, Azure SQL, CDN, App Gateway |
| CI/CD | GitHub Actions → Azure Container Registry → Azure Kubernetes Service — blue/green deployments; lint → test → deploy |

 

 

# **5\. Database Architecture & Schema**

The primary database is SQL Server 2022 (Azure SQL Hyperscale) managed entirely through Entity Framework Core Code-First migrations. All schema changes must be committed as EF Core migration files — direct ALTER TABLE statements against the production database are strictly forbidden.

## **5.1 Entity Relationship Overview**

The SQL Server schema is organised into six bounded-context schemas that mirror the microservice boundaries:

•       \[auth\] 	— Users, Roles, RefreshTokens, UserSessions

•       \[catalog\]  — Products, Variants, Categories, Brands (V1 mock; V2 Cosmos DB for catalog)

•       \[commerce\] — Cart, CartItems, Wishlists, WishlistItems

•       \[orders\]   — Orders, OrderItems, OrderStatusHistory, DeliveryTracking

•       \[payments\] — Payments, Refunds, TokenisedCards, StyleNestCashLedger

•       \[admin\]	— Banners, Coupons, SellerProfiles, AuditLogs

 

## **5.2 Core SQL Server Tables (EF Core Code-First)**

**Users Table — \[auth\].\[Users\]**

| Column | SQL Type | Constraints | Notes |
| :---- | :---- | :---- | :---- |
| Id | UNIQUEIDENTIFIER | PK, DEFAULT NEWID() | Maps to Guid in C\# entity |
| Email | NVARCHAR(256) | UNIQUE, NOT NULL | Lowercase-normalised; used as username |
| PasswordHash | NVARCHAR(MAX) | NOT NULL | ASP.NET Core Identity Argon2id hash |
| PhoneNumber | VARCHAR(15) | NULLABLE | E.164 format; used for OTP auth |
| FirstName | NVARCHAR(100) | NOT NULL |   |
| LastName | NVARCHAR(100) | NOT NULL |   |
| IsEmailVerified | BIT | NOT NULL, DEFAULT 0 | Set true after OTP / magic-link flow |
| StyleNestCashBalance | DECIMAL(18,2) | NOT NULL, DEFAULT 0 | Loyalty wallet; updated via StyleNestCashLedger |
| CreatedAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | UTC timestamp |
| UpdatedAt | DATETIME2 | NOT NULL | Updated by EF Core SaveChanges interceptor |
| IsDeleted | BIT | NOT NULL, DEFAULT 0 | Soft delete; GDPR right-to-erasure sets TRUE |

 

**Orders Table — \[orders\].\[Orders\]**

| Column | SQL Type | Constraints | Notes |
| :---- | :---- | :---- | :---- |
| Id | UNIQUEIDENTIFIER | PK |   |
| OrderNumber | VARCHAR(30) | UNIQUE, NOT NULL | Format: CLQ-YYYYMMDD-XXXXXXXX |
| UserId | UNIQUEIDENTIFIER | FK → auth.Users | Indexed; nullable for guest checkout |
| Status | TINYINT | NOT NULL | Enum: 0=Placed 1=Confirmed 2=Packed 3=Shipped 4=OutForDelivery 5=Delivered 6=Completed |
| Subtotal | DECIMAL(18,2) | NOT NULL | Sum of OrderItems.SalePrice |
| TotalDiscount | DECIMAL(18,2) | NOT NULL, DEFAULT 0 | Coupon \+ StyleNest Cash deduction |
| DeliveryCharge | DECIMAL(18,2) | NOT NULL, DEFAULT 0 | 0 if free shipping threshold met |
| GstAmount | DECIMAL(18,2) | NOT NULL | Calculated at line-item level; summed here |
| NetPayable | DECIMAL(18,2) | NOT NULL | Subtotal \- TotalDiscount \+ DeliveryCharge \+ GstAmount |
| DeliveryAddressId | UNIQUEIDENTIFIER | FK → auth.Addresses | Snapshot copied to OrderDeliveryAddress on place |
| PlacedAt | DATETIME2 | NOT NULL | UTC; indexed for reporting queries |
| DeliveredAt | DATETIME2 | NULLABLE | Set by logistics webhook |

 

## **5.3 EF Core Migration Strategy**

| Rule | Detail |
| :---- | :---- |
| Migration naming | dotnet ef migrations add \<Phase\>\_\<Context\>\_\<Change\> — e.g. Phase1\_Auth\_AddUsers |
| Never edit applied migrations | Create a new corrective migration instead; never manually edit migration files in source control |
| Seed data | Use IEntityTypeConfiguration\<T\>.HasData() for static lookup data (Roles, Categories) |
| Indexes | Declare via HasIndex() in Fluent API; never add via raw SQL; include common query patterns |
| Computed columns | Use HasComputedColumnSql() for derived fields (e.g. DiscountPercent) |
| Soft deletes | Implement via global query filter: modelBuilder.Entity\<T\>().HasQueryFilter(e \=\> \!e.IsDeleted) |
| Audit fields | Use SaveChangesInterceptor to auto-set CreatedAt / UpdatedAt on all entities |
| Concurrency | Add RowVersion byte\[\] with IsRowVersion() on all high-contention entities (Cart, Inventory) |

 

 

# **6\. Backend API Architecture — .NET Core 10**

The backend is organised as a Clean Architecture solution with 10 independently deployable .NET Core Web API projects. All services share a common NuGet package (StyleNest.SharedKernel) containing base entities, result types, and middleware. Communication between services is via Azure Service Bus for async events and direct HTTP calls (via Refit) for synchronous queries.

## **6.1 Solution Structure**

```
style-nest-ecommerce-clone-dotnet-angular-sql/
├── backend/
│   ├── stylenest-clone.slnx
│   ├── src/
│   │   ├── Services/
│   │   │   ├── StyleNest.Gateway.API/      ← YARP — routes all traffic; rate limiting; security headers
│   │   │   ├── StyleNest.Auth.API/         ← JWT RS256, OTP, refresh, password reset, seeder entry point
│   │   │   ├── StyleNest.User.API/         ← Profile, addresses, wishlist, wallet, notifications
│   │   │   ├── StyleNest.Catalog.API/      ← Products, categories, brands, EAV attributes, Redis cache
│   │   │   ├── StyleNest.Cart.API/         ← Cart CRUD, coupon validation, save-for-later
│   │   │   ├── StyleNest.Order.API/        ← 7-state order machine, cancel, return, tracking
│   │   │   ├── StyleNest.Admin.API/        ← CMS (banners, coupons), analytics, super admin, audit logs
│   │   │   ├── StyleNest.Seller.API/       ← Products, inventory, orders, payouts, analytics
│   │   │   ├── StyleNest.Media.API/        ← Upload (image/video), MIME+magic-bytes validation, MinIO
│   │   │   ├── StyleNest.Notification.API/ ← (scaffolded — Phase 14)
│   │   │   └── StyleNest.Payment.API/      ← (scaffolded — Phase 14)
│   │   └── Shared/
│   │       ├── StyleNest.SharedKernel/     ← BaseEntity, Result<T>, IRepository<T>, SecurityHeadersMiddleware
│   │       └── StyleNest.Infrastructure/  ← EF Core DbContext (11 schemas), EfRepository<T>, ICacheService, seeders
│   └── tests/
│       ├── StyleNest.Auth.Tests/           ← 16 tests
│       ├── StyleNest.Catalog.Tests/        ← 21 tests
│       ├── StyleNest.Cart.Tests/           ← 7 tests
│       ├── StyleNest.Order.Tests/          ← 9 tests
│       └── StyleNest.Seller.Tests/         ← 9 tests
├── user-panel/                            ← Angular 21 user storefront
├── admin-panel/                           ← Angular 21 admin + seller panel
├── shared-types/                          ← TypeScript interfaces shared across Angular apps
├── e2e/tests/                             ← Playwright E2E (3 journey specs)
├── infra/                                 ← Bicep / Terraform (Phase 14)
├── .github/workflows/                     ← ci.yml (build+test+docker) + deploy.yml (Azure Container Apps)
├── docker-compose.yml                     ← 17 containers: SQL Server, Redis, MinIO, 9 APIs, 2 Angular apps
├── CLAUDE.md                              ← AI coding rules
└── FEATURE_ROADMAP.md                     ← Phase task tracker (Phases 0–13 complete)
```

 

## **6.2 Microservice Breakdown**

| Service | Port | Schema(s) | Status | Responsibilities |
| :---- | :---- | :---- | :---- | :---- |
| StyleNest.Gateway.API | 5000 | — | **Implemented** | YARP routing to all 8 downstream APIs; rate limiting; JWT pre-validation; aggregated `/health` |
| StyleNest.Auth.API | 5001 | \[auth\] | **Implemented** | JWT RS256, ASP.NET Identity, OTP flow, forgot/reset password, admin/seller creation, refresh token, DbSeeder entry point |
| StyleNest.User.API | 5002 | \[auth\] | **Implemented** | Profile CRUD, address book (set-default), wishlist, wallet (add money, transactions), notifications (mark read, unread count) |
| StyleNest.Catalog.API | 5003 | \[catalog\] | **Implemented** | Products (600 seeded), categories (18), brands (20), EAV attribute definitions (14), Redis cache with invalidation |
| StyleNest.Cart.API | 5004 | \[commerce\] | **Implemented** | Cart CRUD, coupon validation (% and flat), price recalculation, save-for-later via sessionStorage |
| StyleNest.Order.API | 5005 | \[orders\] | **Implemented** | 7-state order machine (Placed→Delivered→Completed), cancel, return request, buy-now, order tracking, history |
| StyleNest.Admin.API | 5009 | \[admin\] | **Implemented** | Banners, coupons, products (activate/deactivate), analytics (revenue, orders, sellers), super admin RBAC, audit logs |
| StyleNest.Seller.API | 5010 | \[seller\] | **Implemented** | Seller profile, dashboard KPIs, product CRUD with EAV attributes, inventory, order status updates, payouts, analytics |
| StyleNest.Media.API | 5011 | \[media\] | **Implemented** | Image/video upload (multipart/form-data), MIME + magic bytes validation, MinIO S3 storage, LocalStorageService dev fallback |
| StyleNest.Notification.API | — | \[notifications\] | Scaffolded | Reserved Phase 14 — console log in dev; Hangfire email job deferred |
| StyleNest.Payment.API | — | \[payments\] | Scaffolded | Reserved Phase 14 — Razorpay integration deferred |

 

## **6.3 API Security Architecture**

•       All microservices deployed in private Azure VNet subnets — only Azure API Management (APIM) is publicly exposed.

•       JWT RS256 asymmetric signing — public keys distributed to all services; private key held only by Auth.API.

•       Access token: 15-minute expiry, returned in response body; Angular stores in NgRx memory only — never localStorage (XSS safe).

•       Refresh token: 7-day expiry, httpOnly cookie only — never in response body. Redis-based revocation list for logout.

•       Azure API Management WAF: rate limiting, bot detection, IP reputation block-lists on all public endpoints.

•       Azure Key Vault: all credentials exclusively via Key Vault references — zero plaintext environment variables in containers.

•       RBAC: fine-grained permission scopes for Admin, Seller, Support Agent, and Customer roles via ASP.NET Core policy-based auth.

 

 

# **7\. Frontend Component Specification — Angular 21**

Every component below must be generated by Claude Code as a single file with a single responsibility. Claude must read docs/skills/angular.md before writing any component. No component prompt should request more than one component. All components use OnPush change detection and standalone component pattern (Angular 21+).

## **7.1 Layout Components**

| Component File | Key Behaviour & Rules |
| :---- | :---- |
| layout/header.component.ts | sticky top-0 z-50 bg-primary-navy. Logo left, mega-menu nav centre, search bar \+ cart badge \+ wishlist count \+ profile menu right. Mobile: hamburger collapse (aria-label set). Sticky add-to-cart notification strip when item added. Standalone component. |
| layout/mega-menu.component.ts | Dropdown on hover/focus. Categories: Fashion, Electronics, Luxury, Home. Brand spotlights per category. WCAG keyboard navigable — full tab/arrow key support via Angular CDK FocusTrap. |
| layout/footer.component.ts | 4-column links grid (Help, Policies, Categories, About). App download CTAs. Social icons. Newsletter signup reactive form. bg-primary-navy text-white. |
| layout/bottom-nav.component.ts | Mobile only (md:hidden). 5 items: Home, Categories, Search, Wishlist, Account. Active state via RouterLinkActive. Fixed bottom-0. Thumb-zone placement. |

 

## **7.2 Homepage Components**

| Component File | Key Behaviour & Rules |
| :---- | :---- |
| home/hero-carousel.component.ts | CMS-managed slides from NgRx store. Auto-play 4s via RxJS interval. Swipe support (HammerJS). Pause on hover. Dot indicators. Lazy-load images with LQIP placeholders. Uses AsyncPipe only — no subscribe() in component class. |
| home/category-banners.component.ts | 4 category tiles: Fashion, Electronics, Luxury, Home. Each links to /search?category=X via RouterLink. Hover overlay animation (200ms). Mobile: horizontal scroll with CSS scroll-snap. |
| home/flash-sale.component.ts | Countdown timer (HH:MM:SS) via RxJS timer. Limited quantity progress bar. Horizontal product scroll. Only renders if sale is active (endTime \> now). accent-red theme. |
| home/promo-banners.component.ts | StyleNest Cash loyalty strip. 4 deal cards. Brand spotlights section. Trending Now horizontal scroll. All data from NgRx store selectors. |

 

## **7.3 Product Listing Page (PLP) Components**

| Component File | Key Behaviour & Rules |
| :---- | :---- |
| catalog/product-card.component.ts | 200px image (object-cover). Brand name above product name. Star rating row. Discount badge (accent-red). Original MRP strikethrough \+ sale price. Wishlist heart toggle (aria-label). Add to Cart on hover. hover:shadow-md 200ms transition. Standalone; OnPush. |
| catalog/filter-sidebar.component.ts | sticky top-20. Sections: Price range slider (Angular CDK), Brand checkboxes, Size pills, Colour swatches, Rating, Discount %, Material, Gender. All dispatch NgRx filter actions. 'Clear all' resets store AND visual states. Mobile: Angular CDK overlay drawer. |
| catalog/applied-filters.component.ts | Chips for each active filter. Individual × removal. 'Clear All' button. Dispatches filter removal actions to NgRx. Animates in/out via Angular animations. |
| catalog/sort-dropdown.component.ts | Angular Material Select. Options: Recommended, Price Low–High, Price High–Low, Newest Arrivals, Top Rated, Highest Discount. Dispatches setSortBy action. Persists via Angular Router query params. |
| catalog/results-grid.component.ts | Count badge. Grid/List view toggle. \*ngFor over filteredProducts$ observable. Empty state with category links. Infinite scroll via IntersectionObserver directive. Skeleton via @angular/material skeleton. Quick-view modal via Angular CDK Dialog. |

 

 

# **8\. Project Directory Structure — As Built**

Final structure after Phase 13. Claude Code prompts must reference file paths exactly as shown.

```
style-nest-ecommerce-clone-dotnet-angular-sql/
├── CLAUDE.md                    ← AI coding rules (read every session — overrides defaults)
├── FEATURE_ROADMAP.md           ← Phase-by-phase task tracker (replaces TODO.md for Phase 9+)
├── TODO.md                      ← Legacy phase checklist (Phases 1–8)
├── .env.example                 ← All required environment variables (never commit .env)
├── docker-compose.yml           ← 17 containers: SQL Server + Redis + MinIO + 9 APIs + 2 Angular apps
├── docs/
│   ├── ARCHITECTURE.md          ← System design, sequence diagrams, ADR-001 to ADR-010
│   ├── API.md                   ← Full endpoint reference (all controllers, all HTTP methods)
│   ├── DATABASE_SCHEMA.md       ← All 11 EF Core schemas + table definitions
│   ├── ROLES_RBAC.md            ← Permission matrix, policy definitions, guard config
│   ├── DESIGN.md                ← Design tokens (§2.1), typography, breakpoints, component specs
│   ├── DEPLOYMENT.md            ← Docker Compose, GitHub Actions CI/CD, Azure Container Apps
│   ├── SECURITY.md              ← Threat model, JWT RS256, security headers, RBAC policies
│   ├── PERFORMANCE.md           ← Redis caching strategy, EF Core compiled queries, Angular bundle
│   ├── MEDIA_UPLOAD.md          ← File upload pipeline, MinIO, MIME validation, ImageSharp
│   ├── SEEDER.md                ← All 40 seeded accounts, 18 categories, 20 brands, 600 products
│   ├── BACKEND_ARCHITECTURE.md  ← .NET service internals, all endpoints per service
│   ├── FRONTEND_ARCHITECTURE.md ← Angular project structure, NgRx slices, component patterns
│   ├── TECH_STACK.md            ← All packages, versions, rationale
│   └── skills/
│       ├── angular.md           ← Angular component + service patterns
│       └── dotnet.md            ← .NET Core controller + entity patterns
├── backend/
│   ├── stylenest-clone.slnx
│   ├── src/Services/            ← 11 .NET Web API projects (9 active + 2 scaffolded)
│   ├── src/Shared/              ← SharedKernel, Infrastructure (EF Core, Redis, seeders)
│   └── tests/                   ← 5 xUnit test projects (62 tests total)
├── user-panel/                  ← Angular 21 user storefront (port 4200)
│   └── src/app/
│       ├── core/                ← Guards, interceptors (JWT, error, toast), services, models
│       ├── store/               ← NgRx: auth, cart, catalog, wishlist, order, ui slices
│       ├── features/            ← Lazy pages: home, PLP, PDP, cart, checkout, auth, account,
│       │                           wallet, order-tracking, OTP, forgot-password, order-detail
│       ├── layout/              ← Header (notification bell), footer, bottom-nav, mega-menu
│       └── shared/              ← EmptyState, Snackbar, Skeleton, AttributeFilter, SaveForLater
├── admin-panel/                 ← Angular 21 admin + seller panel (port 4201)
│   └── src/app/
│       ├── core/                ← Guards (super-admin, admin, seller, auth), interceptors, AdminApiService
│       ├── store/               ← NgRx: auth, ui slices
│       ├── features/            ← Dashboard, products, orders, users, coupons, banners,
│       │                           sellers, RBAC, audit-logs, platform-settings,
│       │                           seller-dashboard, seller-products, seller-inventory,
│       │                           seller-orders, seller-analytics, payout-history
│       ├── layout/              ← Sidebar (role-aware, collapsible), Topbar, Breadcrumb
│       └── shared/              ← KpiCard, ChartCard, DataTable, StatusBadge, ConfirmDialog, FileUpload
├── shared-types/                ← TypeScript interface contracts (auth, catalog, cart, order, user, common)
├── e2e/
│   └── tests/                   ← Playwright E2E: customer-journey, seller-journey, admin-journey
├── infra/                       ← Bicep / Terraform Azure IaC (Phase 14)
└── .github/workflows/
    ├── ci.yml                   ← Build + test + Docker build matrix (8 API images, 2 Angular apps)
    └── deploy.yml               ← ACR push + Azure Static Web Apps + Azure Container Apps update
```

 

 

# **9\. Vibe Coding Phase Plan**

Each phase produces a committed, runnable deliverable. The Vibe Coding approach via Claude Code compressed the original 36-week enterprise timeline to a 2-week intensive sprint delivering all 13 phases.

## **9.1 Phase Overview — Actual Implementation (Phases 1–13 Complete)**

| Phase | Status | Goal | Key Deliverable |
| :---- | :---- | :---- | :---- |
| 0 | **Complete** | TSD provided and reviewed; CLAUDE.md authored | CLAUDE.md, TODO.md, docs skeleton |
| 1 | **Complete** | Folder structure, docker-compose, docs, ARCHITECTURE.md | Repo scaffold, docker-compose.yml, docs/ |
| 2 | **Complete** | SharedKernel, Infrastructure, EF migrations, Auth.API, User.API | Backend foundation + JWT RS256 auth |
| 3 | **Complete** | Angular 21 workspace, Tailwind, NgRx store, layout, homepage | User panel SPA — home, header, footer, NgRx |
| 4 | **Complete** | Angular PLP/PDP/Cart/Checkout + Catalog.API, Cart.API, Order.API | Full V1 storefront + 3 backend services |
| 5 | **Complete** | Dockerfiles (all 6 services), port alignment, CORS, Admin.API, admin UI | docker-compose full-stack, admin panel scaffold |
| 6 | **Complete** | RSA dev keys, DbSeeder (100 products), Buy Now, Login/Register, Wishlist NgRx | End-to-end auth flow, seeded database |
| 7 | **Complete** | DESIGN.md alignment — Playfair Display + DM Sans fonts, design tokens on all components | Pixel-perfect UI matching StyleNest Fashion |
| 8 | **Complete** | Improvement Sprint — global exception middleware, FluentValidation on all 14 controllers, API versioning, ProblemDetails | 86/100 evaluation score, 29 Conventional Commits |
| 9 | **Complete** | Enterprise architecture — YARP Gateway, Seller.API, Media.API, 8 new EF schemas, 40 seeded accounts, 600 products | Production-grade microservice architecture |
| 10 | **Complete** | Admin panel (Angular 21) — NgRx, role-aware guards, 14 feature components, ApexCharts | Full admin + seller panel with analytics |
| 11 | **Complete** | User storefront — wallet, OTP/forgot-password, order tracking stepper, return flow, notification bell, save-for-later, dynamic attribute filters | Complete customer journey |
| 12 | **Complete** | Testing suite — 62 .NET unit tests, 10+ Angular specs, 3 Playwright E2E journeys | 62/62 dotnet test passing |
| 13 | **Complete** | Security headers, Redis caching, health checks, GitHub Actions CI/CD, Azure Container Apps deploy workflow | Production-ready hardening |
| 14 | **Pending** | Azure staging deploy, end-to-end UAT (all 4 roles), Lighthouse > 90, OWASP Top 10 checklist | Go-live validation |

 

## **9.2 Developer-Authored Files (Claude Does NOT Generate These)**

| File | Phase | Author | Contents |
| :---- | :---- | :---- | :---- |
| docs/DESIGN.md | 0b | UI/UX Lead | Component specs, colour tokens, spacing, typography, Angular Material theming, responsive breakpoints, accessibility requirements |
| docs/ARCHITECTURE.md | 1 | Architect | Full folder structure, module boundaries, naming conventions, EF Core schema decisions, Azure service communication patterns |
| docs/skills/angular.md | 2 | Lead Dev | Angular component patterns, NgRx conventions, Angular Material usage rules, standalone vs NgModule, OnPush change detection rules |
| docs/skills/dotnet.md | 2 | Lead Dev | Clean Architecture layers, controller/service/repository pattern, EF Core entity conventions, DTO/AutoMapper rules, FluentValidation setup |
| docs/skills/subagents.md | 3 | Lead Dev | Which microservice modules can be built in parallel, how to coordinate outputs, dependency ordering |

 

 

# **10\. Delivery Timeline & Milestones**

**Planned:** 36 weeks (enterprise squad delivery).  
**Actual (Vibe Coding via Claude Code):** ~2 weeks intensive sprint — all 13 phases completed, 62 unit tests passing, full CI/CD pipeline, production-hardened.

## **10.1 Actual Sprint Log**

| Date | Phase | Delivered |
| :---- | :---- | :---- |
| 2026-05-02 | 1 | Repo scaffold, docker-compose, docs |
| 2026-05-02 | 2 | SharedKernel, Infrastructure, Auth.API, User.API |
| 2026-05-02 | 3 | Angular 21 SPA, Tailwind, NgRx, layout, homepage |
| 2026-05-02 | 4 | PLP, PDP, Cart, Checkout, Catalog.API, Cart.API, Order.API |
| 2026-05-03 | 5 | Dockerfiles, Admin.API, admin panel scaffold |
| 2026-05-04 | 6 | RSA keys, DbSeeder (100 products), Buy Now, Login/Register, Wishlist |
| 2026-05-13 | 7 | DESIGN.md alignment — fonts, design tokens, all UI components |
| 2026-05-13 | 8 | Improvement Sprint — 86/100, 29 Conventional Commits, global exception middleware |
| 2026-05-15 | Arch | Enterprise architecture plan, documentation system created |
| 2026-05-16 | 9 | YARP Gateway, Seller.API, Media.API, 8 new EF schemas, 600 products, 40 accounts |
| 2026-05-16 | 10 | Admin panel (Angular 21) — 14 feature components, ApexCharts |
| 2026-05-16 | 11 | User storefront — wallet, OTP, order tracking, return flow, notification bell |
| 2026-05-16 | 12 | 62 .NET unit tests (0 failures), 10+ Angular specs, 3 Playwright E2E specs |
| 2026-05-16 | 13 | Security headers, Redis caching, health checks, GitHub Actions CI/CD |

## **10.2 Remaining Milestone**

| Phase | Schedule | Scope | Key Deliverable |
| :---- | :---- | :---- | :---- |
| Phase 14 | TBD | Azure staging deploy; UAT (all 4 roles); Lighthouse > 90; OWASP Top 10 checklist; DNS cutover | Production Go-Live |

 

 

# **11\. Non-Functional Requirements**

## **11.1 Performance Targets**

| Category | Target | Implementation Strategy |
| :---- | :---- | :---- |
| LCP (Largest Contentful Paint) | \< 2.5s on 4G mobile | Angular Universal SSR \+ LQIP \+ Azure CDN \+ image optimisation (WebP, responsive srcset) |
| INP (Interaction to Next Paint) | \< 200ms | OnPush change detection; minimal zone.js triggers; NgRx memoised selectors; virtual scroll for long lists |
| CLS (Cumulative Layout Shift) | \< 0.1 | Fixed image dimensions; Angular CDK skeleton loaders; no FOUT via preloaded custom fonts |
| API Response (p95) | \< 300ms normal / \< 800ms peak | Azure Redis caching on catalog, cart, search; EF Core compiled queries; Azure SQL read replicas |
| Concurrent Users | 10,000 at launch → 100,000+ auto-scale | Azure AKS Horizontal Pod Autoscaler on CPU/memory thresholds; Azure SQL Hyperscale |
| Uptime SLA | 99.9% (\< 8.76 hrs downtime/year) | Multi-region Azure deployment; AKS health probes; blue/green deployments; Azure Monitor alerts |

 

## **11.2 Security Requirements**

| Requirement | Implementation |
| :---- | :---- |
| PCI-DSS Level 1 | Tokenised card storage via Razorpay — no raw PAN stored in SQL Server. SAQ-D completed. QSA auditor engaged by Week 8\. |
| OWASP Top 10 | Full remediation required before production go-live. Penetration test by certified VAPT consultant in Phase 6\. |
| Data Encryption | SQL Server Always Encrypted for PII columns (Email, Phone). Azure Blob Storage encryption at rest. TLS 1.3 for all traffic. |
| Access Tokens | JWT RS256. 15-minute expiry. Stored in NgRx memory only — never localStorage or sessionStorage. XSS safe. |
| Refresh Tokens | 7-day expiry. httpOnly cookie only. Rotated on every use. Azure Redis revocation list for logout across all devices. |
| PDPB / GDPR | Data minimisation. Right-to-erasure endpoint on User.API. Audit log via SQL Server Temporal Tables. Export user data on request. |

 

 

# **12\. Vibe Coding Pitfalls & Guards**

Every developer must read this section before beginning their phase.

| Pitfall | What Happens | Prevention |
| :---- | :---- | :---- |
| Prompt too broad | Claude generates 8 files, half incorrect structure | One component / one controller per prompt. Always. No exceptions. |
| Skipping CLAUDE.md | Claude installs wrong libraries, uses class components, adds unwanted NuGet packages | Invest 20 minutes in CLAUDE.md before first prompt. It is the most important file. |
| Not reviewing before running | Runtime errors crash the live demo or staging environment | Read every generated file before dotnet run or ng serve. Treat output as a junior dev PR. |
| No commit checkpoints | One bad prompt wipes hours of working code with no recovery | Commit at every deliverable milestone. No exceptions. |
| Access token in localStorage | XSS vulnerability exposes user sessions | Enforce NgRx memory-only in CLAUDE.md and Phase 3 prompt explicitly. |
| V2 libraries added too early | Azure Service Bus errors, broken Docker builds, EF Core migration conflicts | List V2 stack in CLAUDE.md with explicit 'Do not use before Phase 3' gate. |
| Claude refactors working code | Renames Angular modules, breaks adjacent components silently | Append 'do not modify anything else' to every prompt. |
| Mobile not tested | Desktop Angular looks great; mobile broken at demo | Add responsive classes in every layout prompt. Test Angular at 375px. |
| Context window fills up | Quality drops; Claude forgets .NET coding rules and stack constraints | Start new session; open with 'Read CLAUDE.md' \+ current status summary. |
| Hardcoded Azure credentials | Secret exposure, potential security breach | Phase 4 prompt requires Azure Managed Identity — never static connection strings. |

 

 

# **13\. Acceptance Criteria**

## **13.1 Phase 13 Gate — Achieved**

| Criterion | Status | Evidence |
| :---- | :---- | :---- |
| Build health | **Pass** | `dotnet build` 0 errors, 0 warnings; `npx tsc --noEmit` 0 errors |
| Unit tests | **Pass** | `dotnet test` 62/62 passing — Auth (16), Catalog (21), Cart (7), Order (9), Seller (9) |
| E2E spec files | **Pass** | 3 Playwright journey specs authored in `e2e/tests/` (require running stack to execute) |
| API coverage | **Pass** | All 8 active services expose Swagger UI at `/swagger`; all routes versioned under `/api/v1/` |
| Security headers | **Pass** | 6 headers on all APIs via SecurityHeadersMiddleware; Swagger disabled in Production |
| Redis caching | **Pass** | Catalog products (10 min TTL), categories (60 min TTL); NullCacheService fallback |
| Health checks | **Pass** | `GET /health` on every service; Docker healthcheck on every container |
| CI/CD pipeline | **Pass** | GitHub Actions ci.yml (build+test+docker) + deploy.yml (Azure Container Apps) |
| Docker stack | **Pass** | `docker compose up` starts 17 containers with healthcheck dependencies |

## **13.2 Phase 14 Gate — Required for Go-Live**

| Criterion | Pass Threshold |
| :---- | :---- |
| UAT sign-off | All 4 roles (Super Admin, Admin, Seller, Customer) verified end-to-end on Azure staging |
| Lighthouse score | \> 90 on User Storefront (mobile) — LCP \< 2.5s, INP \< 200ms, CLS \< 0.1 |
| OWASP Top 10 | All High / Critical findings resolved before production DNS cutover |
| Load test | 10,000 concurrent users at \< 1% error rate; API p95 \< 300ms (k6) |
| Browser compatibility | Chrome 110+, Firefox 115+, Safari 16+, Edge 110+ — primary journeys verified |
| Mobile responsiveness | All pages tested at 320px, 375px, 480px, 768px, 1024px, 1280px, 1440px |

 

 

# **14\. Risk Register**

| Risk | Prob. | Impact | Mitigation | Owner |
| :---- | :---- | :---- | :---- | :---- |
| Vibe coding prompt drift — Claude generates code outside approved .NET/Angular stack | **H** | **H** | CLAUDE.md investment before first prompt; V2 library gate; one-component-per-prompt rule | All Devs |
| Scope creep — uncontrolled additions post requirements lock | **H** | **H** | Strict Change Request process; Phase 0 lock; weekly scope review | PM / Client |
| EF Core migration conflicts in parallel development branches | **M** | **H** | Feature branch migration isolation; merge migrations in dependency order; no raw SQL on prod | Architect |
| Azure SQL performance under load | **M** | **H** | Early k6 load testing from Phase 2; Azure SQL Hyperscale auto-scale; Redis caching from Phase 1 | DevOps |
| Razorpay / logistics gateway instability | **M** | **H** | PayU fallback; retry with circuit breakers (Polly library); webhook idempotency | Architect |
| PCI-DSS compliance delays blocking go-live | **L** | **H** | QSA auditor engaged early; Razorpay tokenisation — no raw PAN stored at any point | Security |
| Angular bundle size exceeding performance budget | **M** | **M** | Lazy-loaded feature modules from Phase 1; tree-shaking audit in Phase 6; Angular bundle analyser | Dev B |
| Client feedback delays causing timeline drift | **H** | **M** | Contractual 5-day feedback SLA; escalation path defined; parallel workstreams planned | PM |

 

 

# **15\. Approvals & Sign-off**

By signing below, authorised representatives of both parties confirm agreement to all technical specifications, phase plans, build methodology, acceptance criteria, and risk mitigations set forth in this Technical Specification Document.

 

| Party | Name & Title | Signature | Date Signed |
| :---- | :---- | :---- | :---- |
| **Client — Authorised Representative** |   |   |   |
| **Development Partner — Lead Architect** |   |   |   |
| **QA / Compliance Signatory** |   |   |   |
| **UI/UX Design Lead** |   |   |   |

 

**This document is CONFIDENTIAL and PROPRIETARY.**

Unauthorised reproduction or distribution is strictly prohibited.

TSD v1.0  |  May 2026  |  Project Code: ECM-TSTYLENEST-2026-001  
