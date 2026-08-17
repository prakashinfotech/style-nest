# TECH_STACK.md — Fashion eCommerce Platform
> Complete technology stack reference. All packages, versions, and rationale.
> **Do not add packages outside this list without updating this document.**

---

## Frontend — User Storefront & Admin Panel (Both Projects)

### Core Framework

| Package | Version | Purpose |
|---|---|---|
| `@angular/core` | 21.x | Framework runtime |
| `@angular/router` | 21.x | Client-side routing |
| `@angular/forms` | 21.x | Reactive forms |
| `@angular/common/http` | 21.x | HTTP client |
| `@angular/platform-browser` | 21.x | Browser platform |
| `@angular/cdk` | 21.x | Overlays, drag-drop, a11y utilities |
| `@angular/material` | 21.x | Material Design component library |

### State Management

| Package | Version | Purpose |
|---|---|---|
| `@ngrx/store` | 21.x | Predictable state container |
| `@ngrx/effects` | 21.x | Side effects (HTTP, navigation) |
| `@ngrx/entity` | 21.x | Normalized entity collections |
| `@ngrx/store-devtools` | 21.x | Browser DevTools integration |

### Styling

| Package | Version | Purpose |
|---|---|---|
| `tailwindcss` | 3.x | Utility-first CSS |
| `postcss` | latest | CSS processing |
| `autoprefixer` | latest | CSS vendor prefixes |

### Icons & Charts

| Package | Version | Purpose |
|---|---|---|
| `lucide-angular` | latest | Stroke-based icon system |
| `ng-apexcharts` | latest | Analytics charts (Admin Panel) |

### Utilities

| Package | Version | Purpose |
|---|---|---|
| `rxjs` | 7.x | Reactive extensions (bundled with Angular) |
| `date-fns` | latest | Date formatting utilities |

### Dev Tools

| Package | Version | Purpose |
|---|---|---|
| `typescript` | 5.x | Type checking |
| `jasmine` | latest | Unit test framework |
| `karma` | latest | Test runner |
| `playwright` | latest | E2E browser testing |

---

## Backend — All .NET Services

### Runtime & Framework

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.NET.Sdk.Web` | .NET 10 | ASP.NET Core Web API |
| `Microsoft.AspNetCore.OpenApi` | 10.x | Minimal OpenAPI docs |
| `Swashbuckle.AspNetCore` | 7.x | Swagger UI |

### Database & ORM

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.EntityFrameworkCore` | 9.x | ORM core |
| `Microsoft.EntityFrameworkCore.SqlServer` | 9.x | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Tools` | 9.x | Migration CLI tooling |

### Authentication & Identity

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 9.x | Identity + role management |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.x | JWT RS256 validation |
| `System.IdentityModel.Tokens.Jwt` | latest | Token issuance in Auth.API |

### Validation & Mapping

| Package | Version | Purpose |
|---|---|---|
| `FluentValidation.AspNetCore` | latest | Request DTO validation |
| `AutoMapper` | latest | Entity ↔ DTO mapping |
| `AutoMapper.Extensions.Microsoft.DependencyInjection` | latest | DI wiring |

### Logging

| Package | Version | Purpose |
|---|---|---|
| `Serilog.AspNetCore` | latest | Structured logging host |
| `Serilog.Sinks.Console` | latest | Console sink (dev) |
| `Serilog.Sinks.Seq` | latest | Seq log viewer (dev) |
| `Serilog.Sinks.ApplicationInsights` | latest | Azure App Insights (prod) |

### Caching

| Package | Version | Purpose |
|---|---|---|
| `StackExchange.Redis` | latest | Redis client for distributed cache |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | latest | IDistributedCache Redis impl |

### Background Jobs

| Package | Version | Purpose |
|---|---|---|
| `Hangfire.AspNetCore` | latest | Background job framework |
| `Hangfire.SqlServer` | latest | SQL Server job store |

### API Gateway

| Package | Version | Purpose |
|---|---|---|
| `Yarp.ReverseProxy` | latest | YARP gateway in Gateway.API |

### Media Processing

| Package | Version | Purpose |
|---|---|---|
| `SixLabors.ImageSharp` | latest | Server-side image resize |
| `AWSSDK.S3` | latest | MinIO (S3-compatible) client |

### Real-time

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.AspNetCore.SignalR` | 10.x | WebSocket hubs (order tracking) |

### Email

| Package | Version | Purpose |
|---|---|---|
| `MailKit` | latest | SMTP email sending |
| `MimeKit` | latest | MIME message construction |

### Testing

| Package | Version | Purpose |
|---|---|---|
| `xunit` | latest | Test framework |
| `Moq` | latest | Mocking |
| `FluentAssertions` | latest | Assertion library |
| `Microsoft.EntityFrameworkCore.InMemory` | 9.x | In-memory DB for unit tests |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.x | Integration test web factory |

---

## Infrastructure

### Containers

| Tool | Version | Purpose |
|---|---|---|
| Docker Desktop | latest | Container runtime |
| docker-compose | v2 | Local dev orchestration |

### Storage

| Service | Environment | Purpose |
|---|---|---|
| MinIO | Development | S3-compatible object store |
| Azure Blob Storage | Production | Scalable file storage |

### Database

| Service | Version | Purpose |
|---|---|---|
| SQL Server | 2022 | Primary relational database |
| Redis | 7.x | Cache + session + token blacklist |

### CI/CD

| Tool | Purpose |
|---|---|
| GitHub Actions | CI pipeline (build, test, docker push) |
| Azure Container Registry | Docker image registry |
| Azure App Service | API service hosting (prod) |

### Monitoring (Production)

| Service | Purpose |
|---|---|
| Azure Application Insights | APM, distributed tracing |
| Serilog Seq | Structured log viewer (dev/staging) |
| Azure Monitor | Infrastructure metrics |

---

## V2 Packages (Phase 15+ — DO NOT install before Phase 15)

| Package | Purpose |
|---|---|
| `Azure.ServiceBus` | Async inter-service messaging |
| `Azure.Extensions.AspNetCore.Configuration.Secrets` | Azure Key Vault config |
| `Azure.AI.OpenAI` | AI product recommendations |
| `Azure.Search.Documents` | Cognitive Search (replaces EF full-text) |
| `Razorpay .NET SDK` | Payment gateway integration |
| `Microsoft.ApplicationInsights.AspNetCore` | App Insights SDK |

---

## Design Tokens (Shared across both frontends)

| Token | Hex | CSS Variable | Tailwind |
|---|---|---|---|
| primary-navy | `#1C2B4A` | `--sn-navy` | `bg-navy` |
| accent-red | `#E31837` | `--sn-red` | `bg-red` / `text-red` |
| cta-blue | `#0071C2` | `--sn-blue` | `bg-blue` |
| background | `#F5F5F5` | `--sn-light-gray` | `bg-bg` |
| card-white | `#FFFFFF` | `--sn-white` | `bg-card` |
| text-dark | `#1A1A1A` | `--sn-dark` | `text-dark` |
| text-muted | `#757575` | `--color-muted` | `text-muted` |
| mid-gray | `#9E9E9E` | `--sn-mid-gray` | `text-mid-gray` |
| border | `#E0E0E0` | `--sn-border` | `border-border` |
| luxury-gold | `#C9A84C` | `--sn-gold` | `text-gold` |
| success-green | `#2E7D32` | `--sn-success` | `text-success` |

## Typography

| Font | Role | Import |
|---|---|---|
| Playfair Display | Display / Headings | Google Fonts |
| DM Sans | Body / UI | Google Fonts |

## Responsive Breakpoints

| Name | Width | Use Case |
|---|---|---|
| default | 320px–479px | Mobile, single column, bottom nav |
| `sm:` | 480px | 2-col grid option |
| `md:` | 768px | Top nav, 3-col grid, filter drawer |
| `lg:` | 1024px | Mega-menu, 4-col grid, sticky sidebar |
| `xl:` | 1280px | Full layout, 4–5 col grid |
| `2xl:` | 1440px | Max-width 1440px centered |

---

*Updated after each phase. Cross-reference [ARCHITECTURE.md](ARCHITECTURE.md) for system decisions.*
