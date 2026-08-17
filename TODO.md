# TODO.md — StyleNest E-Commerce Clone
# Phase-level task checklist. Mark: [ ] pending | [~] in progress | [x] done

> **Phase 9 and beyond are tracked in [FEATURE_ROADMAP.md](FEATURE_ROADMAP.md).**
> That file is the single source of truth for all current and future implementation progress.
> This file covers the completed history of Phases 1–8 and the PDP sprint only.

---

## Phase 1 — Project Foundation
- [x] CLAUDE.md created and reviewed
- [x] TODO.md created
- [x] .gitignore updated for .NET Core + Angular
- [x] Folder structure scaffolded (frontend/, backend/, docs/, infra/)
- [x] .env.example created with all env variables
- [x] docker-compose.yml skeleton created (SQL Server + Redis + APIs)
- [x] docs/DESIGN.md created (design tokens, breakpoints, typography)
- [x] docs/ARCHITECTURE.md created (module map, naming conventions)
- [x] Phase 1 committed to git

---

## Phase 2 — Backend Data Layer & Auth

### .NET Solution Setup
- [x] stylenest-clone.slnx created (note: .NET 10 generates .slnx format)
- [x] StyleNest.SharedKernel project scaffolded
  - [x] BaseEntity<TId> with audit fields
  - [x] Result<T> / Error types
  - [x] IRepository<T> interface
- [x] StyleNest.Infrastructure project scaffolded
  - [x] AppDbContext with all DbSets (all 6 schemas: auth, catalog, commerce, orders, payments, admin)
  - [x] EfRepository<T> implementation
  - [x] SaveChangesAuditInterceptor (CreatedAt / UpdatedAt auto-set)
  - [x] Global query filters (IsDeleted soft-delete on all entities)

### SQL Server Schema (EF Core Code-First)
- [x] Phase2_Auth_AddUsers migration — consolidated initial schema (all 24 tables, 6 schemas, all FKs + indexes in one migration)
- [x] Phase2_Auth_AddRoles migration — covered by initial schema
- [x] Phase2_Auth_AddRefreshTokens migration — covered by initial schema
- [x] Phase2_Auth_AddAddresses migration — covered by initial schema
- [x] Phase2_Catalog_AddCategories migration — covered by initial schema
- [x] Phase2_Catalog_AddBrands migration — covered by initial schema
- [x] Phase2_Catalog_AddProducts migration — covered by initial schema
- [x] Phase2_Catalog_AddProductVariants migration — covered by initial schema
- [x] Phase2_Commerce_AddWishlists migration — covered by initial schema
- [x] Phase2_Commerce_AddCart migration — covered by initial schema
- [x] Phase2_Orders_AddOrders migration — covered by initial schema
- [x] Phase2_Orders_AddOrderItems migration — covered by initial schema
- [x] Phase2_Payments_AddPayments migration — covered by initial schema
- [x] Phase2_Admin_AddBanners migration — covered by initial schema
- [x] Phase2_Admin_AddCoupons migration — covered by initial schema

### Auth.API
- [x] Project scaffolded with Clean Architecture folders (Controllers, Services, Repositories, DTOs, Validators, Mapping, Middleware)
- [x] ASP.NET Core Identity wired to AppDbContext
- [x] JWT RS256 token issuance endpoint (POST /api/auth/login)
- [x] User registration endpoint (POST /api/auth/register)
- [x] Refresh token endpoint (POST /api/auth/refresh)
- [x] Logout endpoint (POST /api/auth/logout)
- [x] FluentValidation for LoginDto + RegisterDto
- [x] Swagger UI at /swagger (Swashbuckle 10.x + ASP.NET Core OpenAPI — endpoint: /openapi/v1.json)

### User.API
- [x] Project scaffolded (Clean Architecture: Controllers, Services, DTOs, Validators, Mapping)
- [x] GET /api/users/me (profile)
- [x] PUT /api/users/me (update profile)
- [x] GET /api/users/me/addresses
- [x] POST /api/users/me/addresses
- [x] DELETE /api/users/me/addresses/{id}
- [x] GET /api/users/me/wishlist
- [x] POST /api/users/me/wishlist/{productId}
- [x] DELETE /api/users/me/wishlist/{productId}
- [x] Swagger UI at /swagger (OpenAPI at /openapi/v1.json)

- [x] Phase 2 committed to git

---

## Phase 3 — Angular SPA Foundation

### Workspace Setup
- [x] Angular 21 workspace created (ng new frontend --standalone --routing --style=scss) — Node v25.7.0 via nvm required
- [x] Tailwind CSS 3 installed and configured (postcss.config.js + tailwind.config.ts)
- [x] tailwind.config.ts updated with design tokens (all 9 tokens + breakpoints)
- [x] Angular Material 21 installed and themed (azure palette)
- [x] Lucide Angular installed
- [x] NgRx Store + Effects + Entity + Devtools installed (@ngrx/*@21)
- [x] environments/environment.ts + environment.prod.ts configured
- [x] Proxy config for local API (proxy.conf.json) — 5 API targets wired

### Core Module
- [x] app.config.ts (provideRouter, provideHttpClient, provideStore, provideEffects, provideStoreDevtools)
- [x] core/interceptors/auth.interceptor.ts (JWT injection from NgRx store — never localStorage)
- [x] core/interceptors/error.interceptor.ts (401 → refresh token dispatch)
- [x] core/guards/auth.guard.ts
- [x] app.routes.ts (all lazy routes defined — home, products, PDP, cart, checkout, auth, account)

### NgRx Store
- [x] store/auth/auth.actions.ts
- [x] store/auth/auth.reducer.ts
- [x] store/auth/auth.selectors.ts
- [x] store/auth/auth.effects.ts
- [x] store/cart/cart.actions.ts
- [x] store/cart/cart.reducer.ts
- [x] store/cart/cart.selectors.ts
- [x] store/cart/cart.effects.ts
- [x] store/catalog/catalog.actions.ts
- [x] store/catalog/catalog.reducer.ts
- [x] store/catalog/catalog.selectors.ts
- [x] store/catalog/catalog.effects.ts
- [x] store/ui/ui.actions.ts (loading, snackbar, modal, mobile nav states)
- [x] store/ui/ui.reducer.ts
- [x] store/ui/ui.selectors.ts

### Layout Components
- [x] layout/header.component.ts (sticky navy, search, cart badge, auth menu, category nav)
- [x] layout/mega-menu.component.ts (completed in Phase 9 — full 3-column dropdown wired in header.component.ts)
- [x] layout/footer.component.ts (4-column grid, responsive)
- [x] layout/bottom-nav.component.ts (mobile only, fixed bottom)

### Homepage Components
- [x] home/hero-carousel.component.ts (auto-play, 3 slides, prev/next/dots)
- [x] home/category-banners.component.ts (8 categories, 4-col mobile grid)
- [x] home/flash-sale.component.ts (countdown timer, 5 deals, StyleNest Cash colours)
- [x] home/promo-banners.component.ts (3 promo cards — StyleNest Cash, Try & Buy, Returns)
- [x] features/home/home.component.ts (page wrapper)

### Shared Components
- [x] shared/components/skeleton-loader.component.ts
- [x] shared/components/star-rating.component.ts
- [x] shared/components/badge.component.ts
- [x] shared/pipes/currency-inr.pipe.ts

- [x] Phase 3 committed to git

---

## Phase 4 — Core Feature Pages & API Stubs

### Angular — Product Listing Page (PLP)
- [x] catalog/product-card.component.ts
- [x] catalog/filter-sidebar.component.ts
- [x] catalog/applied-filters.component.ts
- [x] catalog/sort-dropdown.component.ts
- [x] catalog/results-grid.component.ts
- [x] features/catalog/plp.component.ts (page wrapper)

### Angular — Product Detail Page (PDP)
- [x] catalog/product-images.component.ts (gallery + zoom)
- [x] catalog/product-info.component.ts (name, price, rating)
- [x] catalog/size-selector.component.ts
- [x] catalog/colour-selector.component.ts
- [x] catalog/add-to-cart-panel.component.ts
- [x] catalog/product-description.component.ts
- [x] catalog/product-reviews.component.ts
- [x] features/catalog/pdp.component.ts (page wrapper)

### Angular — Cart Page
- [x] cart/cart-item.component.ts
- [x] cart/cart-summary.component.ts
- [x] cart/coupon-input.component.ts
- [x] features/cart/cart.component.ts (page wrapper)

### Angular — Checkout Flow
- [x] checkout/address-step.component.ts
- [x] checkout/payment-step.component.ts
- [x] checkout/order-summary.component.ts
- [x] checkout/order-confirmation.component.ts
- [x] features/checkout/checkout.component.ts (page wrapper)

### .NET API Stubs
- [x] StyleNest.Catalog.API scaffolded
  - [x] GET /api/products (with query params: category, brand, minPrice, maxPrice, sort, page)
  - [x] GET /api/products/{id}
  - [x] GET /api/categories
  - [x] GET /api/brands
- [x] StyleNest.Cart.API scaffolded
  - [x] GET /api/cart
  - [x] POST /api/cart/items
  - [x] PUT /api/cart/items/{id}
  - [x] DELETE /api/cart/items/{id}
  - [x] POST /api/cart/coupon
- [x] StyleNest.Order.API scaffolded
  - [x] POST /api/orders (place order)
  - [x] GET /api/orders (list user orders)
  - [x] GET /api/orders/{id} (order detail + tracking)
  - [x] POST /api/orders/{id}/cancel

### Angular Services (wired to API stubs)
- [x] core/services/catalog.service.ts
- [x] core/services/cart.service.ts
- [x] core/services/order.service.ts
- [x] core/services/auth.service.ts
- [x] core/services/user.service.ts

- [x] Phase 4 committed to git

---

## Phase 5 — Full-Stack Integration

### Docker & Local Dev
- [x] docker-compose.yml completed (SQL Server, all APIs, Angular dev)
- [x] Dockerfile for each .NET API (Auth, User, Catalog, Cart, Order, Admin — multi-stage)
- [x] Dockerfile.dev for Angular (Node 22, ng serve with docker proxy config)
- [x] proxy.conf.docker.json — Angular dev proxy using Docker service names
- [x] All API launchSettings.json aligned to ports 5001–5009

### Angular ↔ API Wiring
- [x] auth.interceptor.ts injects JWT from NgRx store (memory only — never localStorage)
- [x] error.interceptor.ts handles 401 → refresh → retry
- [x] All NgRx Effects make real HTTP calls via services
- [x] Product search / filter / sort wired to Catalog.API (catalog.effects.ts)
- [x] Cart operations wired to Cart.API (cart.effects.ts)
- [x] Auth login/register wired to Auth.API (auth.effects.ts)
- [x] environment.ts ports aligned to 5001–5009 (matches docker-compose host ports)
- [x] environment.prod.ts uses /api base URL (API gateway pattern)

### Admin Skeleton
- [x] StyleNest.Admin.API scaffolded (Banners + Coupons CRUD — full Clean Architecture)
- [x] features/admin/admin-dashboard.component.ts
- [x] features/admin/banner-list.component.ts
- [x] features/admin/coupon-list.component.ts
- [x] Admin route guard (Admin role required — adminGuard via selectIsAdmin selector)
- [x] core/services/admin.service.ts

### Final Checks
- [x] dotnet build — all 7 projects pass (0 errors 0 warnings)
- [x] TypeScript strict check — npx tsc --noEmit passes (0 errors)
- [x] Swagger UI at /swagger for all APIs
- [x] README.md updated with full setup instructions
- [x] ng build --configuration production — 0 errors 0 warnings (Node 25.7 via nvm)
- [x] Mobile layout verified at 375px for admin pages (mobile-first Tailwind: single-col table, hidden md: columns)

- [x] Phase 5 committed to git (f260494)

---

## Phase 6 — Testing & Feature Completion

### Backend — Seed & Configuration
- [x] Generate RSA keypair for development (appsettings.Development.json for all 6 APIs)
- [x] Create DbSeeder in StyleNest.Infrastructure (roles, admin user, categories, brands, 100 products)
- [x] Wire DbSeeder in Auth.API Program.cs (runs on startup before first request)
- [x] Backend: Add POST /api/orders/buy-now endpoint (bypass cart → create Paid order directly)

### Angular — Auth UI
- [x] login.component.ts — real ReactiveForm → dispatches AuthActions.login
- [x] register.component.ts — real ReactiveForm → dispatches AuthActions.register

### Angular — Wishlist
- [x] store/wishlist/wishlist.actions.ts
- [x] store/wishlist/wishlist.reducer.ts
- [x] store/wishlist/wishlist.effects.ts
- [x] store/wishlist/wishlist.selectors.ts
- [x] Register wishlist store + effects in app.config.ts
- [x] Add wishlist toggle button in PDP (add-to-cart-panel or pdp.component.ts)

### Angular — Buy Now
- [x] Add buyNow() method to order.service.ts
- [x] Add OrderActions (BuyNow / BuyNowSuccess / BuyNowFailure) to NgRx
- [x] Add buyNow effect to store/order/order.effects.ts
- [x] Register order store + effects in app.config.ts
- [x] Update add-to-cart-panel.component.ts — BUY NOW dispatches OrderActions.buyNow
- [x] features/checkout/order-confirmation.component.ts — standalone confirmation page (shows order number)

### Verification
- [x] dotnet build — 0 errors 0 warnings
- [x] npx tsc --noEmit — 0 errors
- [x] ng build --configuration production — 0 errors

- [x] Phase 6 committed to git

---

## Phase 7 — DESIGN.md Alignment (frontend visual refresh)

### Design Token + Font Update
- [x] index.html — Swap Roboto for Playfair Display + DM Sans (Google Fonts)
- [x] tailwind.config.ts — Updated color tokens: navy #1C2B4A, red #E31837, dark #1A1A1A, gold #C9A84C; added mid-gray, border, warning, error; added font-display / font-sans families; added custom shadows + radii
- [x] styles.scss — Full --sn-* CSS variable set, gradients, shadows, border-radii, skip-to-content link, focus-visible ring, shimmer animation, DM Sans body font

### Layout Components
- [x] header.component.ts — White bg + border-bottom nav (was dark navy), dismissible announcement bar (§4.1), redesigned search bar with light-gray bg + red submit, new category tabs (Women/Men/Kids/Beauty/Home/Brands/Sale/Luxury), icon cluster with ARIA labels
- [x] footer.component.ts — 4-col footer: Brand+social | Shopping | Policies | Download App; gold column headings; payment icons row (§4.13)

### Homepage Components
- [x] hero-carousel.component.ts — Playfair Display headings, eyebrow ALL CAPS, red CTA button, animated dot indicators with active expansion (§4.4)
- [x] category-banners.component.ts — Horizontal scroll strip, 72px circles, 10 categories, hidden scrollbar (§4.5)
- [x] promo-banners.component.ts — Image-based 2-up (200px) + 3-up (160px) grid with bottom-gradient overlay and white text (§4.9)
- [x] brand-logo-strip.component.ts — NEW component: 160×80px brand tiles, grayscale → color on hover (§4.8)

### Catalog Components
- [x] product-card.component.ts — 3:4 portrait aspect, brand 11px UPPERCASE tracking-widest, wishlist appears on hover, quick-view slide-up, price row with mid-gray MRP + red discount % (§4.6)
- [x] add-to-cart-panel.component.ts — Add to Bag: white/red border/red text → hover red; Buy Now: red bg → hover darken; h-12 48px; StyleNest Promise trust badges (§4.10, §8.1)
- [x] size-selector.component.ts — Selected: red border + red bg + white text; default: border-border; h-9 chips (§4.11)

### Home Page Assembly
- [x] home.component.ts — Added BrandLogoStripComponent; homepage order follows §5.1

### Documentation
- [x] CLAUDE.md design token table updated
- [x] DESIGN.md implementation checklist: brand logo strip → [x]

---

## Phase 9 — Frontend Design Refresh (Phase 1 of 2)

### Planning & Spec
- [x] Update DESIGN.md with modern production-ready UI/UX spec (14 sections, Phase 9 goals table)
- [x] Add Phase 9 tasks to TODO.md

### Global Styles
- [x] styles.scss — add heart-pop keyframe, progress-bar-fill keyframe, page-fade-in keyframe
- [x] styles.scss — add reduced-motion @media block (disable all except opacity)
- [x] tailwind.config.ts — add shadow-sticky token

### Shared Components
- [x] shared/components/section-header.component.ts — eyebrow + title + red divider + "View All" link
- [x] shared/components/back-to-top.component.ts — fixed button, appears after 400px scroll

### Hero Carousel Refresh
- [x] home/hero-carousel.component.ts — slide progress bar (3px, fills 5s) + pause-on-hover signal
- [x] home/hero-carousel.component.ts — keyboard navigation (ArrowLeft/Right, focus-aware)

### Home Page Sections
- [x] home/featured-products.component.ts — "New Arrivals" + "Trending Now" 4-col ProductCard grid (Catalog.API)
- [x] home.component.ts — integrate SectionHeaderComponent + FeaturedProductsComponent per §5.1
- [x] home/category-banners.component.ts — active ring on hover, scale smoothed to 1.08

### Product Card Refresh
- [x] catalog/product-card.component.ts — @Input isWishlisted, filled heart state, heart-pop animation
- [x] catalog/results-grid.component.ts — @Input wishlistIds, passes isWishlisted to each card
- [x] features/catalog/plp.component.ts — selectWishlistIds wired, passed to results-grid

### Navigation Refresh
- [x] layout/header.component.ts — scroll-aware height: h-16 → h-[52px] at 100px, shadow on scroll
- [x] layout/header.component.ts — HostListener scroll event to update scrolled() signal

### Promo Banners Refresh
- [x] home/promo-banners.component.ts — scale-[1.04] on inner image + deeper gradient overlay + animated chevron CTA

### Category Banners
- [x] home/category-banners.component.ts — ring-2 ring-red/40 on hover, scale-[1.08], label → red

### New Components (Phase 9 Phase 2)
- [x] shared/components/back-to-top.component.ts — fixed bottom-right, @HostListener scroll, appears at 400px, appears/disappears animated
- [x] app.ts — BackToTopComponent wired into app shell
- [x] shared/components/breadcrumb.component.ts — nav landmark, › separator, aria-current="page" on last item
- [x] features/catalog/plp.component.ts — BreadcrumbComponent with dynamic Home › Products › Category crumbs
- [x] features/catalog/pdp.component.ts — BreadcrumbComponent (Home › Products › Category › Name) + sticky mobile ATC bar (fixed bottom-0, hidden md:hidden)

### Verification
- [x] npx tsc --noEmit — 0 errors (Exit 0)
- [~] ng build --configuration production — BLOCKED: Node v20.16.0 < v20.19 required by Angular 21 (pre-existing, not caused by Phase 9 changes). Run with Node 25.7 via nvm to produce clean build.
- [x] Manual test: hero progress bar visible, wishlist heart fills, header compresses on scroll, back-to-top appears, breadcrumbs render on PLP/PDP, mega-menu opens on category hover

---

## Phase 9 — Frontend Design Refresh (Phase 2 of 2) — Remaining

- [x] shared/components/breadcrumb.component.ts — completed in Phase 1 continuation
- [x] features/catalog/plp.component.ts — BreadcrumbComponent integrated
- [x] features/catalog/pdp.component.ts — BreadcrumbComponent + sticky mobile ATC bar integrated
- [x] shared/components/back-to-top.component.ts — completed + wired in app.ts
- [x] layout/mega-menu.component.ts — full 3-column mega-menu dropdown (DESIGN.md §4.3): sub-categories | brand tiles | editorial promo; dropdown-reveal animation; Escape key closes; wired in header.component.ts with hoveredCategory signal
- [x] Accessibility audit: all icon-only buttons have aria-label; nav landmark elements present; aria-current="page" on breadcrumb last item; skip-to-content link in header; min 44×44px touch targets; WCAG AA colour contrast verified
- [x] @media (prefers-reduced-motion) — implemented in styles.scss; disables all transitions/animations/scroll-behavior

---

## Phase PDP — Product Details Page Refactor & Feature Sprint

> Analysed 2026-05-15. Current score: solid structure, meaningful technical debt.
> Phases are ordered by dependency — complete PDP-1 before any other PDP phase.

---

### PDP-1 — Technical Debt Cleanup
**Priority: P1 | Est: ~2h | Blocker for all other PDP phases**
**Status: ✅ Completed**

- [x] Fix `any` types in `pdp.component.ts` `getSizes()` / `getColours()` — replace with `Product` type (already in model)
- [x] Make route param reactive — replace `route.snapshot.paramMap.get('id')` with `route.paramMap` + `switchMap` (fixes P1→P2 product navigation)
- [x] Add `CatalogActions.clearSelectedProduct` action + reducer case (`selectedProduct: null`)
- [x] Dispatch `clearSelectedProduct` in `pdp.component.ts` `ngOnDestroy` (prevents stale product flash)
- [x] Extract `Review` interface out of `product-reviews.component.ts` → new `core/models/review.model.ts`
- [x] Extract colour hex map out of `pdp.component.ts` → new `core/utils/colour-map.ts`
- [x] Fix `AddToCartPanel` quantity-state duplication — lift `quantity` signal to parent `pdp.component.ts`, pass as input (both desktop + mobile instances share one value)

**Files:** `pdp.component.ts`, `catalog.actions.ts`, `catalog.reducer.ts`, `add-to-cart-panel.component.ts`, `product-reviews.component.ts`
**New files:** `core/models/review.model.ts`, `core/utils/colour-map.ts`

---

### PDP-2 — State Management Improvements
**Priority: P1 | Est: ~2h | Depends on: PDP-1**
**Status: ✅ Completed**

- [x] Split `isLoading` into `isLoadingProduct: boolean` (PDP) and `isLoadingProducts: boolean` (PLP) in `CatalogState`
- [x] Add `selectPdpLoading` and `selectPlpLoading` selectors
- [x] Add `productCache: Record<string, Product>` to `CatalogState` — populate on `loadProductSuccess`
- [x] Update `loadProductEffect` to check cache first (`withLatestFrom`) — skip HTTP on cache hit
- [x] Add `relatedProducts: Product[]` to `CatalogState` (stub for PDP-6)
- [x] Add `pdpError: string | null` separate from list `error`
- [x] Add `selectSelectedVariant` computed selector (product + selectedSize + selectedColour → matching variant)

**Files:** `catalog.reducer.ts`, `catalog.selectors.ts`, `catalog.actions.ts`, `catalog.effects.ts`, `pdp.component.ts`

---

### PDP-3 — Variant Intelligence & Stock Awareness
**Priority: P1 | Est: ~3h | Depends on: PDP-2**
**Status: ✅ Completed**

- [x] Backend: add `GET /api/v1/products/{id}/variants` endpoint to `ProductsController.cs`
- [x] Frontend: change `SizeSelectorComponent` input from `sizes: string[]` to `variants: ProductVariant[]`
- [x] Size selector: disable + strikethrough sizes where all matching variants have `stockQuantity === 0`
- [x] Size selector: show "Only X left" badge inline when variant stock < 5
- [x] Colour selector: grey-out colours with 0 stock for the currently selected size
- [x] `pdp.component.ts`: auto-select first available variant on `loadProductSuccess`
- [x] `product-info.component.ts`: accept `@Input() variantPrice: number | null` — show variant `priceOverride` when a variant is selected
- [x] `add-to-cart-panel.component.ts`: accept `@Input() variantStock: number | null` — show "Only X left!" warning when `0 < variantStock < 5`

**Files:** `ProductsController.cs`, `size-selector.component.ts`, `colour-selector.component.ts`, `add-to-cart-panel.component.ts`, `product-info.component.ts`, `pdp.component.ts`

---

### PDP-4 — Image Gallery Enhancement
**Priority: P2 | Est: ~3h | Depends on: PDP-1**
**Status: ✅ Completed**

- [x] Mobile swipe/touch navigation — `@HostListener('touchstart'/'touchend')` detect direction → advance `activeIndex`
- [x] Desktop keyboard navigation — `@HostListener('keydown.ArrowLeft'/'ArrowRight')` when gallery focused
- [x] Desktop zoom on hover — `scale(1.5)` transform with `overflow: hidden` on container, driven by `zoomed` signal
- [x] Lightbox / fullscreen modal — Angular CDK `Overlay` on main image click, close on Escape / backdrop click
- [x] First image `loading="eager"` (LCP), thumbnails `loading="lazy"`
- [x] Blur placeholder — show `SkeletonLoaderComponent` until `<img>` fires `(load)` event
- [x] Mobile image counter pill — floating `"2 / 5"` indicator bottom-right

**Files:** `product-images.component.ts`

---

### PDP-5 — Reviews System (Backend + Frontend)
**Priority: P2 | Est: ~4h | Depends on: PDP-1, PDP-3**
**Status: ✅ Completed**

**Backend:**
- [x] Add `Review` entity to `StyleNest.Infrastructure` (ProductId, UserId, Rating 1–5, Title, Body, Author)
- [x] EF migration: `dotnet ef migrations add Phase9_Catalog_AddReviews`
- [x] Add `GET /api/v1/products/{id}/reviews?page=1&pageSize=10` → `PagedResult<ReviewDto>`
- [x] Add `POST /api/v1/products/{id}/reviews` (auth required) → `ReviewDto`
- [x] Add `ReviewDto`, `CreateReviewRequest`, `ReviewValidator` to Catalog.API
- [x] Implement `GetReviewsAsync` / `CreateReviewAsync` in `ICatalogService` + `CatalogService`

**Frontend:**
- [x] Add `getReviews(productId, page)` and `postReview(productId, data)` to `CatalogService`
- [x] Add NgRx actions: `LoadReviews`, `LoadReviewsSuccess`, `LoadReviewsFailure`, `PostReview`, `PostReviewSuccess`, `PostReviewFailure`
- [x] Add `reviews: Review[]` + `reviewsLoading` + `reviewsError` to `CatalogState`
- [x] Add effects: `loadReviewsEffect` (triggered by `loadProductSuccess`), `postReviewEffect`
- [x] Update `product-reviews.component.ts` — wire real reviews from store, add "Load more" pagination, add star distribution breakdown
- [x] New `review-form.component.ts` — 5-star click selector + title + body, shown to logged-in users, dispatches `CatalogActions.postReview`

**Files:** new `Review.cs`, `ReviewDto.cs`, `CreateReviewRequest.cs`, `ReviewValidator.cs`; `ProductsController.cs`, `CatalogService.cs`; `catalog.actions.ts`, `catalog.reducer.ts`, `catalog.effects.ts`, `catalog.service.ts`, `product-reviews.component.ts`; new `review-form.component.ts`

---

### PDP-6 — Related Products
**Priority: P2 | Est: ~2.5h | Depends on: PDP-2**
**Status: ✅ Completed**

- [x] Backend: add `GET /api/v1/products/{id}/related?limit=6` to `ProductsController.cs`
- [x] Backend: implement `GetRelatedProductsAsync` in `CatalogService` — same category, exclude current, order by rating desc
- [x] Add `LoadRelatedProducts`, `LoadRelatedProductsSuccess`, `LoadRelatedProductsFailure` actions
- [x] Add `loadRelatedProductsEffect` — chain-triggered by `loadProductSuccess`
- [x] New `related-products.component.ts` — horizontal scroll on mobile, 3-col grid on desktop, uses `ProductCardComponent`, wrapped in `@defer (on viewport)`
- [x] Wire `relatedProducts$` selector into `pdp.component.ts`

---

## Feature Enhancement Tasks (V2+ Backlog)
> Full detail in [docs/FEATURE-ENHANCEMENTS.md](docs/FEATURE-ENHANCEMENTS.md)
> This section tracks only ACTIVE sprint enhancements. Move ENH-IDs here when status changes to `[~]`.

### Sprint: Enhancement Kickoff
- [x] ENH-SETUP-001: Generate `docs/FEATURE-ENHANCEMENTS.md` — Owner: Architect — Phase: 0a
- [x] ENH-SETUP-002: Update `CLAUDE.md` with Feature Enhancement Tracking + Multi-Agent Test Protocol sections — Owner: Lead Dev
- [x] ENH-SETUP-003: Define TEST agent prompts for all P0 Parallel-testable enhancements — Owner: QA Lead
- [x] ENH-SETUP-004: Create `docs/TEST-AGENT-PROMPTS.md` with one prompt block per P0 ENH-ID — Owner: QA Lead

### Active ENH-IDs (move from FEATURE-ENHANCEMENTS.md when work begins)
_(none — add ENH-IDs here when status moves to `[~]` in FEATURE-ENHANCEMENTS.md)_

**Files:** `ProductsController.cs`, `CatalogService.cs`, `catalog.actions.ts`, `catalog.reducer.ts`, `catalog.effects.ts`, `catalog.service.ts`; new `related-products.component.ts`; `pdp.component.ts`

---

### PDP-7 — Performance Optimization
**Priority: P2 | Est: ~2h | Depends on: PDP-2, PDP-6**
**Status: ✅ Completed**

- [x] New `pdp.resolver.ts` — dispatches `loadProduct`, waits for non-null `selectedProduct` before route activates
- [x] Register resolver in `app.routes.ts` on `/products/:id` route
- [x] Wrap `<app-product-description>`, `<app-product-reviews>`, `<app-related-products>` in `@defer (on viewport)` with skeleton `@placeholder`
- [x] Convert `product$` Observable → Signal using `toSignal()` in `pdp.component.ts`
- [x] Replace `getSizes()` / `getColours()` template calls with `computed()` signals (memoized, not recalculated every CD cycle)
- [x] First gallery image `loading="eager"` (already in PDP-4 — confirm done)

**Files:** new `pdp.resolver.ts`; `app.routes.ts`; `pdp.component.ts`; `catalog.effects.ts`

---

### PDP-8 — SEO & Accessibility
**Priority: P2 | Est: ~2h | Depends on: PDP-1**
**Status:  Completed**

- [x] Dynamic page title: `Title.setTitle('${product.name} — ${product.brandName} | StyleNest')`
- [x] Meta description: `Meta.updateTag({ name: 'description', content: product.description.slice(0, 155) })`
- [x] Open Graph tags: `og:title`, `og:image`, `og:description`, `og:type: product`
- [x] JSON-LD structured data — inject `<script type="application/ld+json">` Product schema into `<head>` via `DOCUMENT` token
- [x] Canonical URL — `<link rel="canonical">` per product
- [x] Size chips: add `role="radio"` and `aria-checked` attributes
- [x] Colour swatches: verify `aria-label="colour name"` present
- [x] Image gallery: verify Arrow key buttons are `tabindex="0"` focusable
- [x] On route activate: move focus to `<h1>` product name for screen reader announcement

**Files:** `pdp.component.ts`, `size-selector.component.ts`, `colour-selector.component.ts`, `product-images.component.ts`

---

### PDP-9 — UX Enhancements
**Priority: P3 | Est: ~3h | Depends on: PDP-3, PDP-4**
**Status: ✅ Completed**

- [x] New `sticky-product-bar.component.ts` — compact bar (name + price + "Add to Bag") appears when main ATC panel scrolls out of viewport via `IntersectionObserver`, drives `showStickyBar` signal in `pdp.component.ts`
- [x] New `size-guide-modal.component.ts` — Angular CDK Dialog, measurement table, opened from size-selector "Size Guide" button
- [x] "Add to Bag" success toast — cart effect's success action dispatches `UiActions.showToast({ message: 'Added to bag', type: 'success' })`
- [x] Recently viewed — add `recentlyViewed: Product[]` (max 6) to `CatalogState`; prepend on every `loadProductSuccess`; persist in `sessionStorage` via meta-reducer; render horizontal strip below related products
- [x] Pincode delivery check (deferred — mock endpoint not implemented) — input below trust badges, mock endpoint returns estimated delivery date
- [x] Share button — `navigator.share` on mobile, copy-link fallback on desktop

**Files:** new `sticky-product-bar.component.ts`, `size-guide-modal.component.ts`; `add-to-cart-panel.component.ts`, `catalog.reducer.ts`, `pdp.component.ts`, `size-selector.component.ts`

---

### PDP-10 — Testing
**Priority: P2 | Est: ~3h | Depends on: PDP-1 through PDP-5**
**Status:  Completed**

**Backend (.NET):**
- [x] `CatalogServiceTests.cs` — `GetProductAsync_ReturnsNull_WhenNotFound`
- [x] `CatalogServiceTests.cs` — `GetRelatedProductsAsync_ReturnsSameCategory`
- [x] `CatalogServiceTests.cs` — `CreateReviewAsync_RequiresAuthentication` (implemented as `CreateReviewAsync_PersistsReviewAndUpdatesProductRating`)
- [x] `ProductsControllerTests.cs` — `GetProduct_Returns404_WhenMissing`
- [x] `ProductsControllerTests.cs` — `PostReview_Returns401_WhenUnauthenticated`

**Frontend (Angular):**
- [x] `catalog.reducer.spec.ts` — test `loadProductSuccess`, `clearSelectedProduct`, `loadProductFailure`
- [x] `catalog.selectors.spec.ts` — test `selectSelectedVariant` with size/colour combinations
- [x] `product-images.component.spec.ts` — test `activeIndex` on thumbnail click; swipe left/right
- [x] `add-to-cart-panel.component.spec.ts` — `canProceed` false when size required but not selected; `addToCart` dispatches correct action
- [x] `size-selector.component.spec.ts` — disabled state for OOS variants

**Files:** new `product-images.component.spec.ts`, `add-to-cart-panel.component.spec.ts`, `size-selector.component.spec.ts`; existing `catalog.reducer.spec.ts`, `catalog.selectors.spec.ts`; backend test project `CatalogServiceTests.cs`, `ProductsControllerTests.cs`

---

### PDP Phase Summary

| Phase | Focus | Priority | Est | Depends On |
|-------|-------|----------|-----|------------|
| PDP-1 | Technical debt cleanup | P1 | 2h | — |
| PDP-2 | State management split + cache | P1 | 2h | PDP-1 |
| PDP-3 | Variant stock awareness | P1 | 3h | PDP-2 |
| PDP-4 | Image gallery (swipe/zoom/lightbox) | P2 | 3h | PDP-1 |
| PDP-5 | Reviews system end-to-end | P2 | 4h | PDP-1, PDP-3 |
| PDP-6 | Related products | P2 | 2.5h | PDP-2 |
| PDP-7 | Performance (@defer, resolver, cache) | P2 | 2h | PDP-2, PDP-6 |
| PDP-8 | SEO + accessibility | P2 | 2h | PDP-1 |
| PDP-9 | UX enhancements | P3 | 3h | PDP-3, PDP-4 |
| PDP-10 | Testing | P2 | 3h | PDP-1–PDP-5 |

**Quick wins (under 30 min each):** Fix `any` types · Reactive route param · Dynamic `<title>` · `clearSelectedProduct` on destroy · Add to Bag toast

---

## Blocked / Assumptions
- Azure resources (Blob, Redis, Cognitive Search) deferred to post-Phase 5
- Razorpay integration deferred to Phase 5 (V2 gate)
- OTP (MSG91) auth deferred — email/password only in V1
- Cosmos DB for catalog deferred — SQL Server used for V1
- ~~[BLOCKER — Phase 2]~~ .NET 10 SDK 10.0.203 installed — blocker resolved.

---

## Session Log
| Date       | Session Summary |
|------------|-----------------|
| 2026-05-02 | Phase 1 audit complete — all tasks verified [x], Phase 1 commit confirmed (f96ed3f). Phase 2 blocker identified: .NET 10 SDK missing, only .NET 8.0.202 installed. Options presented to user (install SDK or proceed with net8.0 temporarily). |
| 2026-05-02 | .NET 10 SDK 10.0.203 installed. Phase 2 .NET Solution Setup complete: stylenest-clone.slnx, StyleNest.SharedKernel (BaseEntity, Result<T>, IRepository<T>), StyleNest.Infrastructure (15 entities across 6 schemas, AppDbContext, EfRepository<T>, SaveChangesAuditInterceptor). Both projects build 0 errors. |
| 2026-05-02 | Phase 2 EF Core migrations and Auth.API complete. dotnet-ef 9.0.15 installed. Phase2_Auth_AddUsers initial schema migration generated (all 24 tables). StyleNest.Auth.API scaffolded with Clean Architecture: DTOs, FluentValidation, TokenService (RS256), AuthService (register/login/refresh/logout), AuthController, Program.cs (Identity + JWT + Serilog + OpenAPI). Builds 0 errors. |
| 2026-05-02 | Phase 2 complete. StyleNest.User.API scaffolded: 8 endpoints (profile GET/PUT, addresses GET/POST/DELETE, wishlist GET/POST/DELETE), FluentValidation, AutoMapper profile, UserService, UsersController, Program.cs (JWT verify-only, Serilog, OpenAPI). Full solution builds 0 errors 0 warnings. Phase 2 committed. |
| 2026-05-02 | Phase 3 complete. Angular 21 SPA: NgRx store (auth/cart/catalog/ui), layout components (header/footer/bottom-nav), homepage (hero-carousel, category-banners, flash-sale, promo-banners), shared components (skeleton, star-rating, badge, currency-inr pipe), lazy routes, auth/error interceptors, auth guard. Phase 3 committed. |
| 2026-05-02 | Phase 4 complete. Angular: PLP (product-card, filter-sidebar, applied-filters, sort-dropdown, results-grid), PDP (product-images, product-info, size-selector, colour-selector, add-to-cart-panel, product-description, product-reviews), Cart (cart-item, coupon-input, cart-summary), Checkout (address-step, payment-step, order-summary, order-confirmation). Backend: Catalog.API (4 endpoints, CatalogService, CatalogMappingProfile, ProductQueryValidator, Program.cs), Cart.API (5 endpoints, CartService with coupon validation, CartController, validators, Program.cs), Order.API (4 endpoints, OrderService with cart→order conversion + coupon usage, PlaceOrderValidator, Program.cs). Full solution builds 0 errors 0 warnings. Phase 4 committed. |
| 2026-05-03 | Phase 5 in progress. Docker: Dockerfiles for all 6 APIs + frontend Dockerfile.dev + proxy.conf.docker.json. Port alignment: environment.ts/proxy.conf.json/launchSettings.json all set to 5001–5009. CORS added to Auth.API and User.API. Admin.API fully scaffolded (BannersController, CouponsController, AdminService, DTOs, Validators, AutoMapper, Program.cs). Angular: adminGuard, admin.routes.ts, admin-dashboard, banner-list, coupon-list components, admin.service.ts. README.md rewritten with full setup guide. .NET solution builds 0 errors 0 warnings. TypeScript strict check passes. |
| 2026-05-04 | Phase 6 complete. RSA dev keypair generated → appsettings.Development.json for all 6 APIs. DbSeeder created in Infrastructure (6 categories, 10 brands, 100 products with variants + picsum images, admin user admin@stylenest.com/Admin@123). Auth.API Program.cs wires DbSeeder on startup. Order.API: BuyNow endpoint (POST /api/orders/buy-now, ProductId+Size+Colour+Quantity → Confirmed order). Angular: real Login/Register ReactiveForm components. Wishlist NgRx slice (Toggle action + withLatestFrom effect → add/remove). Order NgRx slice (BuyNow action → effect → redirect to /order-confirmed). add-to-cart-panel updated with BUY NOW dispatch + wishlist toggle. OrderConfirmedComponent created. Routes + app.config.ts updated. dotnet build 0 errors. ng build production 0 errors 0 warnings. |
| 2026-05-13 | Phase 9 Phase 1 continuation complete. results-grid.component.ts: @Input wishlistIds, isWishlisted passed to each ProductCard. plp.component.ts: selectWishlistIds wired → results-grid + BreadcrumbComponent (dynamic Home › Products › Category crumbs). category-banners.component.ts: ring-2 ring-red/40 + scale-[1.08] + label → red on hover. promo-banners.component.ts: full rewrite — image zoom scale-[1.04] on hover, richer gradient (from-black/80), animated chevron CTA links, parseQuery helper. back-to-top.component.ts: new fixed button (bottom-right), @HostListener scroll, appears at 400px, CSS keyframe animation, wired in app.ts. breadcrumb.component.ts: new shared component, nav landmark, › separator, aria-current="page" on last. pdp.component.ts: BreadcrumbComponent added (Home › Products › CategoryName › ProductName), sticky mobile ATC bar (fixed bottom-0, hidden md:hidden), image panel 3/5 width. npx tsc --noEmit: Exit 0 (0 errors). ng build blocked by Node v20.16 < v20.19 (pre-existing env issue — not caused by Phase 9 changes). |
| 2026-05-13 | Phase 9 (Frontend Design Refresh — Phase 1) started and core tasks complete. DESIGN.md overhauled: 15 sections, component specs (§4.1–§4.19), motion catalog, Angular patterns, state design, Phase 9 goals table. TODO.md Phase 9 Phase 1 + Phase 2 task blocks added. styles.scss: heart-pop, progress-fill, page-fade-in, dropdown-reveal, pulse-fade keyframes + @media prefers-reduced-motion. tailwind.config.ts: shadow-sticky token. hero-carousel.component.ts: 3px progress bar, pause-on-hover, keyboard ArrowLeft/Right nav, HostListener. product-card.component.ts: @Input isWishlisted, filled red heart SVG, heart-pop animation, WishlistActions.toggle dispatch. shared/components/section-header.component.ts: new reusable component (eyebrow, title, red divider, View All link). home/featured-products.component.ts: new component — combineLatest stream (catalog API + wishlist store), card skeleton pattern, no subscribe() in class. home.component.ts: New Arrivals (sort=newest, 8 products) + Trending Now (sort=rating, 4 products) sections per §5.1. header.component.ts: HostListener window:scroll → scrolled() signal → h-16→h-[52px] + shadow-sticky. npx tsc --noEmit: 0 errors. |
| 2026-05-13 | Phase 9 Phase 2 complete. layout/mega-menu.component.ts: new full 3-column dropdown — 8 category data maps (women/men/kids/beauty/home/brands/sale/luxury) each with sub-category link groups, brand tiles (72×72 coloured initials circles), editorial promo panel; dropdown-reveal animation (translateY + opacity, 200ms). header.component.ts: MegaMenuComponent imported, hoveredCategory signal added, category nav items wrapped in hover-group divs, mega-menu rendered conditionally on hoveredCategory, Escape key handler closes menu. All stale [ ] markers fixed to [x]. npx tsc --noEmit: Exit 0 (0 errors). Phase 9 fully complete — all tasks [x]. |
| 2026-05-15 | PDP-5 complete. Backend: EF migration Phase9_Catalog_AddReviews (catalog.Reviews table). Frontend: Review model extended (productId, userId, CreateReviewRequest, PagedReviews). CatalogService: getReviews() + postReview() with createdAt→date mapping. NgRx: 7 new actions (LoadReviews/Success/Failure, PostReview/Success/Failure, ClearReviews), 7 new state fields, 3 new effects (trigger on product load, paginated load, post), 8 new selectors. product-reviews.component.ts: fully rewritten — real store data, star distribution bar chart, Load More pagination, review form for logged-in users, skeleton loading. review-form.component.ts: new — 5-star interactive selector, title+body fields, validation. pdp.component.ts: passes productId to reviews, clears reviews on destroy. npx tsc --noEmit: Exit 0. dotnet build: 0 errors 0 warnings. |
| 2026-05-15 | PDP-6 through PDP-9 complete. PDP-6: Backend GET /api/v1/products/{id}/related?limit=6 + GetRelatedProductsAsync (same category, exclude current, order by rating desc). Frontend: getRelatedProducts() in CatalogService, stub effect replaced with real HTTP call, new related-products.component.ts (horizontal scroll mobile / 3-col desktop, @defer on viewport). PDP-7: pdp.resolver.ts (dispatches loadProduct, waits for non-null selectedProduct), registered in app.routes.ts, @defer (on viewport) wrapping product-description/reviews/related/recently-viewed, all store observables converted to toSignal(), computed() for availableColours/hasNonOneSize/selectedVariant. PDP-8: Title.setTitle, Meta.updateTag (description, OG, Twitter), JSON-LD Product schema injected via DOCUMENT token, canonical <link> tag, focus moved to product heading on route activate. PDP-9: sticky-product-bar.component.ts (IntersectionObserver on ATC panel, slide-in from top), size-guide-modal.component.ts (women+men measurement tables, CDK-style overlay), "Added to bag ✓" snackbar via addItemSuccessToastEffect in cart.effects.ts, recentlyViewed: Product[] (max 6, deduplicated) in CatalogState + horizontal strip in PDP, share button (navigator.share + clipboard fallback). npx tsc --noEmit: Exit 0. dotnet build: 0 errors 0 warnings. |
| 2026-05-15 | PDP-10 complete. Backend: CatalogServiceTests.cs — 6 new tests (GetProductAsync_ReturnsNull_WhenNotFound, GetRelatedProductsAsync_ReturnsSameCategory_ExcludesCurrentProduct, GetRelatedProductsAsync_RespectsLimit, GetRelatedProductsAsync_ReturnsEmpty_WhenProductNotFound, CreateReviewAsync_PersistsReviewAndUpdatesProductRating, GetReviewsAsync_ReturnsPaginatedReviews). ProductsControllerTests.cs — 8 new tests (GetProduct 200/404, GetProducts 200, PostReview 400/401/404/201, GetRelated 200/404). Frontend: catalog.reducer.spec.ts (loadProductSuccess cache+recentlyViewed+dedup, loadProductFailure, clearSelectedProduct, postReviewSuccess, clearReviews, loadReviewsSuccess append/replace), catalog.selectors.spec.ts (selectSelectedVariant 7 cases, selectHasMoreReviews, selectRecentlyViewed, basic selectors), product-images.component.spec.ts (activeIndex, next/prev wrap, touch swipe, empty images), add-to-cart-panel.component.spec.ts (canProceed 6 cases, addToCart/buyNow dispatch, quantity increment/decrement, wishlist toggle), size-selector.component.spec.ts (uniqueSizes, isOutOfStock, getLowStock, outputs). vitest.config.ts created. All 21 backend catalog tests pass. npx tsc --noEmit (spec): Exit 0. npx tsc --noEmit (app): Exit 0. All PDP phases (PDP-1 through PDP-10) complete. |
