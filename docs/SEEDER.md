# SEEDER.md — Database Seed Data Specification
> Complete seeder architecture, account credentials, and seed data inventory.
> All seeders live in StyleNest.Infrastructure/Seeders/

---

## 1. Seeder Architecture

```
StyleNest.Infrastructure/Seeders/
├── DbSeeder.cs               ← Orchestrator — calls all seeders in dependency order
├── RoleSeeder.cs             ← ASP.NET Core Identity roles
├── AttributeSeeder.cs        ← AttributeDefinitions (runs before categories)
├── CategorySeeder.cs         ← Product categories with hierarchy
├── BrandSeeder.cs            ← Fashion brands
├── SuperAdminSeeder.cs       ← 1 super admin account
├── AdminSeeder.cs            ← 4 admin accounts
├── SellerSeeder.cs           ← 20 seller accounts + Sellers table rows
├── UserSeeder.cs             ← 15 customer accounts
├── ProductSeeder.cs          ← 100 products across categories
├── BannerSeeder.cs           ← Homepage banners
└── CouponSeeder.cs           ← Demo coupon codes
```

### Seeder Invocation

```csharp
// Auth.API Program.cs — runs on startup in Development environment only
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}
```

### Orchestrator Pattern

```csharp
// DbSeeder.cs
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILogger<DbSeeder>>();

        logger.LogInformation("Running database migrations...");
        await db.Database.MigrateAsync();

        logger.LogInformation("Seeding database...");

        // Run in dependency order — DO NOT reorder
        await new RoleSeeder(roleManager, logger).SeedAsync();
        await new AttributeSeeder(db, logger).SeedAsync();
        await new CategorySeeder(db, logger).SeedAsync();
        await new BrandSeeder(db, logger).SeedAsync();
        await new SuperAdminSeeder(userManager, logger).SeedAsync();
        await new AdminSeeder(userManager, logger).SeedAsync();
        await new SellerSeeder(userManager, db, logger).SeedAsync();
        await new UserSeeder(userManager, logger).SeedAsync();
        await new ProductSeeder(db, logger).SeedAsync();
        await new BannerSeeder(db, logger).SeedAsync();
        await new CouponSeeder(db, logger).SeedAsync();

        logger.LogInformation("Database seeding complete.");
    }
}
```

### Idempotency Rule

Every seeder checks before inserting. If data already exists, it skips:

```csharp
// Pattern used in every seeder
if (await db.Categories.AnyAsync())
{
    logger.LogInformation("CategorySeeder: Already seeded, skipping.");
    return;
}
```

---

## 2. Default Credentials

**Universal password for all seeded accounts:** `Test@123`

---

## 3. Super Admin (1 account)

| Field | Value |
|---|---|
| Email | `superadmin@mailinator.com` |
| Password | `Test@123` |
| FirstName | `Super` |
| LastName | `Admin` |
| Role | `SuperAdmin` |
| EmailConfirmed | `true` |

**Note:** SuperAdmin is NOT created via any API endpoint. Only via seeder or manual DB insert.

---

## 4. Admin Accounts (4 accounts)

| # | Email | FirstName | LastName | Role |
|---|---|---|---|---|
| 1 | `admin1@mailinator.com` | Priya | Mehta | Admin |
| 2 | `admin2@mailinator.com` | Rohan | Sharma | Admin |
| 3 | `admin3@mailinator.com` | Ananya | Gupta | Admin |
| 4 | `admin4@mailinator.com` | Vikram | Patel | Admin |

All: Password = `Test@123`, EmailConfirmed = `true`, IsActive = `true`

---

## 5. Seller Accounts (20 accounts)

| # | Email | Store Name | Status |
|---|---|---|---|
| 1 | `seller01@mailinator.com` | Anokhi Collection | Active |
| 2 | `seller02@mailinator.com` | Fabindia Outlet | Active |
| 3 | `seller03@mailinator.com` | W for Woman | Active |
| 4 | `seller04@mailinator.com` | Biba Fashion House | Active |
| 5 | `seller05@mailinator.com` | Global Desi Store | Active |
| 6 | `seller06@mailinator.com` | Aurelia Boutique | Active |
| 7 | `seller07@mailinator.com` | Marigold Lane | Active |
| 8 | `seller08@mailinator.com` | Manyavar Men | Active |
| 9 | `seller09@mailinator.com` | Raymond Exclusive | Active |
| 10 | `seller10@mailinator.com` | Louis Philippe | Active |
| 11 | `seller11@mailinator.com` | Van Heusen | Active |
| 12 | `seller12@mailinator.com` | Allen Solly | Active |
| 13 | `seller13@mailinator.com` | HRX by Hrithik | Active |
| 14 | `seller14@mailinator.com` | Roadster | Active |
| 15 | `seller15@mailinator.com` | Dressberry | Active |
| 16 | `seller16@mailinator.com` | Nayo Ethnic Wear | Active |
| 17 | `seller17@mailinator.com` | Libas Studio | Active |
| 18 | `seller18@mailinator.com` | Puma India | Active |
| 19 | `seller19@mailinator.com` | Nike Store | Active |
| 20 | `seller20@mailinator.com` | Adidas Originals | Active |

All: Password = `Test@123`, Role = `Seller`, IsVerified = `true`, Status = `Active`

---

## 6. Customer Accounts (15 accounts)

| # | Email | FirstName | LastName |
|---|---|---|---|
| 1 | `user01@mailinator.com` | Riya | Sharma |
| 2 | `user02@mailinator.com` | Aditya | Kumar |
| 3 | `user03@mailinator.com` | Neha | Gupta |
| 4 | `user04@mailinator.com` | Arjun | Patel |
| 5 | `user05@mailinator.com` | Simran | Singh |
| 6 | `user06@mailinator.com` | Rahul | Verma |
| 7 | `user07@mailinator.com` | Pooja | Reddy |
| 8 | `user08@mailinator.com` | Kabir | Nair |
| 9 | `user09@mailinator.com` | Divya | Joshi |
| 10 | `user10@mailinator.com` | Manish | Agarwal |
| 11 | `user11@mailinator.com` | Sneha | Bose |
| 12 | `user12@mailinator.com` | Vikas | Yadav |
| 13 | `user13@mailinator.com` | Priyanka | Malhotra |
| 14 | `user14@mailinator.com` | Rohit | Shah |
| 15 | `user15@mailinator.com` | Kavya | Iyer |

All: Password = `Test@123`, Role = `User`, EmailConfirmed = `true`

---

## 7. Categories Seed Data

```
Clothing (parent)
  ├── Kurtas & Kurtis
  ├── Sarees
  ├── Lehengas
  ├── Salwar Suits
  ├── Western Dresses
  ├── Tops & T-Shirts
  ├── Jeans & Trousers
  └── Ethnic Wear

Footwear (parent)
  ├── Heels
  ├── Flats
  ├── Sneakers
  ├── Boots
  ├── Sandals
  └── Formal Shoes

Bags (parent)
  ├── Handbags
  ├── Backpacks
  ├── Clutches
  └── Totes

Jewelry (parent)
  ├── Necklaces
  ├── Earrings
  ├── Rings
  ├── Bangles
  └── Bracelets

Watches (parent)
  ├── Analog
  ├── Digital
  └── Smart Watches

Beauty & Grooming (parent)
  ├── Skincare
  ├── Makeup
  └── Fragrances
```

---

## 8. Brands Seed Data (20 brands)

| Brand | Category Focus |
|---|---|
| FabIndia | Ethnic, Kurtas |
| Biba | Women Ethnic |
| W for Woman | Women Western |
| Manyavar | Men Ethnic |
| Raymond | Men Formal |
| Louis Philippe | Men Formal |
| Van Heusen | Men Formal |
| Allen Solly | Men/Women Smart Casual |
| HRX | Sportswear |
| Puma | Sportswear/Footwear |
| Nike | Sportswear/Footwear |
| Adidas | Sportswear/Footwear |
| Roadster | Casual Wear |
| Dressberry | Women Casual |
| Global Desi | Women Ethnic |
| Aurelia | Women Ethnic |
| Libas | Women Ethnic |
| Fossil | Watches |
| Tanishq | Jewelry |
| Lakme | Beauty |

---

## 9. Products Seed Data (100 products)

Distribution across categories:
- Clothing: 35 products (kurtas, sarees, jeans, dresses)
- Footwear: 20 products (heels, sneakers, formal)
- Bags: 15 products (handbags, backpacks, clutches)
- Jewelry: 15 products (necklaces, earrings, rings)
- Watches: 10 products (analog, smart)
- Beauty: 5 products (skincare, makeup)

Each product includes:
- Title, description, brand, category
- BasePrice, MRP (with realistic Indian pricing)
- 2–4 ProductVariants (size/color combinations)
- 3–5 ProductImages (from placeholder image service)
- ProductAttributes (category-specific)
- AverageRating (seeded with realistic values 3.5–5.0)
- IsActive = true, IsApproved = true

**Image URLs:** `https://picsum.photos/seed/{productSlug}/800/800` (placeholder images)

---

## 10. Banners Seed Data (6 banners)

| Position | Title | ImageUrl |
|---|---|---|
| Hero | Summer Collection 2026 | picsum.photos/seed/hero1/1440/480 |
| Hero | Ethnic Wear Festival | picsum.photos/seed/hero2/1440/480 |
| Hero | End of Season Sale | picsum.photos/seed/hero3/1440/480 |
| PromoBanner | Women's Picks | picsum.photos/seed/promo1/800/400 |
| PromoBanner | Men's Essentials | picsum.photos/seed/promo2/800/400 |
| CategoryStrip | Luxury Brands | picsum.photos/seed/luxury1/400/200 |

All banners: IsActive = true, no date restrictions in dev environment.

---

## 11. Coupons Seed Data (5 coupons)

| Code | Type | Value | Min Order | Max Discount | Limit | Description |
|---|---|---|---|---|---|---|
| `WELCOME10` | Percentage | 10% | ₹500 | ₹200 | Per user: 1 | Welcome offer for new users |
| `FLAT200` | Fixed | ₹200 | ₹999 | — | Per user: 3 | Flat ₹200 off |
| `SUMMER15` | Percentage | 15% | ₹1500 | ₹500 | Per user: 2 | Summer sale |
| `ETHNIC20` | Percentage | 20% | ₹2000 | ₹800 | Per user: 1 | Ethnic wear special |
| `FREESHIP` | FreeShipping | 0 | ₹300 | — | Per user: 5 | Free delivery |

All coupons: IsActive = true, no expiry in dev environment.

---

## 12. Attribute Definitions Seed Data

See [DATABASE_SCHEMA.md](DATABASE_SCHEMA.md#attributedefinitions) for the complete table.

### Category-Attribute Mappings (seed)

| Category | Required Attributes | Optional Attributes |
|---|---|---|
| Kurtas & Kurtis | Fabric, Fit, Color | Sleeve Type, Neck Type, Occasion |
| Sarees | Fabric, Color | Occasion |
| Western Dresses | Fabric, Fit, Color | Sleeve Type, Neck Type |
| Jeans & Trousers | Fit, Color | Fabric |
| Footwear (all) | Shoe Size, Color | Sole Material, Heel Type |
| Bags (all) | Color | Strap Type, Material |
| Jewelry (all) | Metal Type, Color | Stone Type |
| Watches (all) | Color | — |

---

## 13. Running Seeders

### Automatic (on startup — Development only)

```bash
# Runs automatically when Auth.API starts in Development environment
dotnet run --project backend/src/Services/StyleNest.Auth.API
```

### Manual (via EF migrations)

```bash
cd backend
dotnet ef database update \
  --project src/Shared/StyleNest.Infrastructure \
  --startup-project src/Services/StyleNest.Auth.API
```

### Reset and Re-seed

```bash
# Drop and recreate the entire database
dotnet ef database drop --project src/Shared/StyleNest.Infrastructure \
  --startup-project src/Services/StyleNest.Auth.API --force

dotnet ef database update \
  --project src/Shared/StyleNest.Infrastructure \
  --startup-project src/Services/StyleNest.Auth.API
# Seeder runs automatically on next startup
```

---

*Cross-reference [ROLES_RBAC.md](ROLES_RBAC.md) for role definitions.*
*Cross-reference [DATABASE_SCHEMA.md](DATABASE_SCHEMA.md) for full table schemas.*
