# ROLES_RBAC.md — Roles, Permissions & RBAC Design
> Complete role-based access control specification.
> Four roles: SuperAdmin · Admin · Seller · User (Customer)

---

## 1. Role Definitions

| Role | Description | Created By | Scope |
|---|---|---|---|
| **SuperAdmin** | Full platform control, unrestricted access | Manually inserted into DB | Entire platform |
| **Admin** | Business management with limited permissions | SuperAdmin creates | All data except security settings |
| **Seller** | Manage own store, products, and orders | SuperAdmin or Admin creates | Own seller data only |
| **User** | Customer — browse, purchase, manage orders | Self-registration | Own user data only |

### Role Hierarchy

```
SuperAdmin
    └── Admin
            └── Seller
                    └── User
```

Each role inherits upward — `SuperAdmin` can perform all `Admin`, `Seller`, and `User` actions.
`Seller` can ONLY access their own data — never another seller's products or orders.

---

## 2. Initial SuperAdmin Setup

SuperAdmin is inserted manually via DB seeder (not via registration API):

```csharp
// SuperAdminSeeder.cs
await userManager.CreateAsync(new ApplicationUser {
    UserName = "superadmin@mailinator.com",
    Email    = "superadmin@mailinator.com",
    // ... other fields
}, "Test@123");
await userManager.AddToRoleAsync(user, "SuperAdmin");
```

**There is no public endpoint to create a SuperAdmin.** The only way to create one is:
1. DB seeder (development)
2. Manual SQL script signed off by a platform owner (production)

---

## 3. Permission Matrix

| Resource | Action | SuperAdmin | Admin | Seller | User |
|---|---|:---:|:---:|:---:|:---:|
| **Platform Settings** | Read | ✓ | — | — | — |
| **Platform Settings** | Write | ✓ | — | — | — |
| **Audit Logs** | Read | ✓ | — | — | — |
| **RBAC / Roles** | Read | ✓ | — | — | — |
| **RBAC / Roles** | Write | ✓ | — | — | — |
| **Admin Accounts** | Create | ✓ | — | — | — |
| **Admin Accounts** | Read | ✓ | — | — | — |
| **Admin Accounts** | Suspend | ✓ | — | — | — |
| **All Sellers** | Read | ✓ | ✓ | — | — |
| **All Sellers** | Approve / Reject | ✓ | ✓ | — | — |
| **All Sellers** | Suspend | ✓ | ✓ | — | — |
| **Seller (own profile)** | Read / Update | ✓ | ✓ | ✓ (own) | — |
| **All Users** | Read | ✓ | ✓ | — | — |
| **All Users** | Suspend | ✓ | ✓ | — | — |
| **User (own profile)** | Read / Update | ✓ | ✓ | — | ✓ (own) |
| **All Products** | Read (incl. inactive) | ✓ | ✓ | — | — |
| **All Products** | Approve / Reject | ✓ | ✓ | — | — |
| **All Products** | Activate / Deactivate | ✓ | ✓ | — | — |
| **Seller Products (own)** | Create | ✓ | ✓ | ✓ | — |
| **Seller Products (own)** | Update / Delete | ✓ | ✓ | ✓ (own) | — |
| **Products (public)** | Read | ✓ | ✓ | ✓ | ✓ |
| **Categories** | Read | ✓ | ✓ | ✓ | ✓ |
| **Categories** | Create / Update | ✓ | ✓ | — | — |
| **Brands** | Read | ✓ | ✓ | ✓ | ✓ |
| **Brands** | Create / Update | ✓ | ✓ | — | — |
| **Attribute Definitions** | Read | ✓ | ✓ | ✓ | — |
| **Attribute Definitions** | Create / Update | ✓ | ✓ | — | — |
| **All Orders** | Read | ✓ | ✓ | — | — |
| **All Orders** | Update Status | ✓ | ✓ | — | — |
| **Seller Orders (own)** | Read | ✓ | ✓ | ✓ (own) | — |
| **Seller Orders (own)** | Update Status | ✓ | ✓ | ✓ (own) | — |
| **User Orders (own)** | Place | — | — | — | ✓ |
| **User Orders (own)** | Read / Cancel | ✓ | ✓ | — | ✓ (own) |
| **Inventory** | Read | ✓ | ✓ | ✓ (own) | — |
| **Inventory** | Update | ✓ | ✓ | ✓ (own) | — |
| **Banners** | CRUD | ✓ | ✓ | — | — |
| **Coupons** | CRUD | ✓ | ✓ | — | — |
| **Coupons** | Apply | ✓ | ✓ | — | ✓ |
| **CMS Pages** | CRUD | ✓ | ✓ | — | — |
| **Cart** | Read / Update | — | — | — | ✓ (own) |
| **Wishlist** | Read / Update | — | — | — | ✓ (own) |
| **Addresses** | CRUD | — | — | — | ✓ (own) |
| **Wallet** | Read | ✓ | ✓ | — | ✓ (own) |
| **Wallet** | Credit (admin) | ✓ | ✓ | — | — |
| **Wallet** | Debit (pay) | — | — | — | ✓ (own) |
| **Reviews** | Read | ✓ | ✓ | ✓ | ✓ |
| **Reviews** | Create | — | — | — | ✓ (verified purchase) |
| **Reviews** | Approve / Delete | ✓ | ✓ | — | — |
| **Analytics (platform)** | Read | ✓ | Partial | — | — |
| **Analytics (seller own)** | Read | ✓ | ✓ | ✓ (own) | — |
| **Payouts** | Read | ✓ | ✓ | ✓ (own) | — |
| **Payouts** | Process | ✓ | ✓ | — | — |
| **Media Upload** | Images / Videos | ✓ | ✓ | ✓ | — |
| **Notifications (own)** | Read / Mark Read | ✓ | ✓ | ✓ | ✓ |

---

## 4. .NET Authorization Implementation

### Policy Registration

```csharp
// Program.cs (each API service)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly",
        p => p.RequireRole("SuperAdmin"));

    options.AddPolicy("AdminOrAbove",
        p => p.RequireRole("SuperAdmin", "Admin"));

    options.AddPolicy("SellerOrAbove",
        p => p.RequireRole("SuperAdmin", "Admin", "Seller"));

    options.AddPolicy("AuthenticatedUser",
        p => p.RequireAuthenticatedUser());

    options.AddPolicy("OwnSellerData",
        p => p.AddRequirements(new SellerOwnershipRequirement()));
});
```

### Seller Ownership Requirement

```csharp
// OwnSellerData policy — Sellers can only access their own data
public class SellerOwnershipRequirement : IAuthorizationRequirement { }

public class SellerOwnershipHandler : AuthorizationHandler<SellerOwnershipRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SellerOwnershipRequirement requirement)
    {
        // SuperAdmin and Admin bypass ownership check
        if (context.User.IsInRole("SuperAdmin") || context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var sellerId = context.User.FindFirst("sellerId")?.Value;
        var resource = context.Resource as string;

        if (sellerId != null && resource == sellerId)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
```

### Controller Decoration Examples

```csharp
// Seller.API — SellerProductsController.cs
[ApiController]
[Route("api/v1/seller/products")]
[Authorize(Policy = "SellerOrAbove")]
public class SellerProductsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyProducts()
    {
        // Extract sellerId from JWT claim — never from request body
        var sellerId = User.FindFirst("sellerId")!.Value;
        return Ok(await _service.GetProductsBySellerAsync(sellerId));
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequestDto dto)
    {
        var sellerId = User.FindFirst("sellerId")!.Value;
        // sellerId is always from JWT, never trusted from client
        return Created(...);
    }
}

// Admin.API — SuperAdminController.cs
[ApiController]
[Route("api/v1/super-admin")]
[Authorize(Policy = "SuperAdminOnly")]
public class SuperAdminController : ControllerBase { ... }

// Admin.API — AdminController.cs
[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "AdminOrAbove")]
public class AdminController : ControllerBase { ... }
```

---

## 5. Angular Route Guards

### Guard Hierarchy

```typescript
// super-admin.guard.ts
export const superAdminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  return authService.hasRole('SuperAdmin')
    ? true
    : router.createUrlTree(['/unauthorized']);
};

// admin.guard.ts
export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  return authService.hasAnyRole(['SuperAdmin', 'Admin'])
    ? true
    : router.createUrlTree(['/unauthorized']);
};

// seller.guard.ts
export const sellerGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  return authService.hasAnyRole(['SuperAdmin', 'Admin', 'Seller'])
    ? true
    : router.createUrlTree(['/unauthorized']);
};
```

### Route Configuration

```typescript
// admin-panel/app.routes.ts
export const routes: Routes = [
  {
    path: 'super-admin',
    canActivate: [superAdminGuard],
    loadChildren: () => import('./features/super-admin/routes')
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadChildren: () => import('./features/admin/routes')
  },
  {
    path: 'seller',
    canActivate: [sellerGuard],
    loadChildren: () => import('./features/seller/routes')
  }
];
```

---

## 6. Admin Panel Sidebar — Role-Aware Navigation

```typescript
// Navigation items shown based on role from JWT
const navConfig = {
  superAdmin: [
    { label: 'Dashboard',         route: '/super-admin',          icon: 'BarChart2' },
    { label: 'Admin Users',        route: '/super-admin/admins',   icon: 'Users' },
    { label: 'Sellers',            route: '/super-admin/sellers',  icon: 'Store' },
    { label: 'Customers',          route: '/super-admin/users',    icon: 'User' },
    { label: 'RBAC / Permissions', route: '/super-admin/rbac',     icon: 'Shield' },
    { label: 'Platform Settings',  route: '/super-admin/settings', icon: 'Settings' },
    { label: 'Audit Logs',         route: '/super-admin/audit-logs', icon: 'FileText' },
  ],
  admin: [
    { label: 'Dashboard',   route: '/admin',            icon: 'BarChart2' },
    { label: 'Products',    route: '/admin/products',   icon: 'Package' },
    { label: 'Categories',  route: '/admin/categories', icon: 'Grid' },
    { label: 'Brands',      route: '/admin/brands',     icon: 'Tag' },
    { label: 'Orders',      route: '/admin/orders',     icon: 'ShoppingBag' },
    { label: 'Banners',     route: '/admin/banners',    icon: 'Image' },
    { label: 'Coupons',     route: '/admin/coupons',    icon: 'Percent' },
    { label: 'Users',       route: '/admin/users',      icon: 'Users' },
    { label: 'Reviews',     route: '/admin/reviews',    icon: 'Star' },
  ],
  seller: [
    { label: 'Dashboard',   route: '/seller',             icon: 'BarChart2' },
    { label: 'My Products', route: '/seller/products',    icon: 'Package' },
    { label: 'Inventory',   route: '/seller/inventory',   icon: 'Archive' },
    { label: 'Orders',      route: '/seller/orders',      icon: 'ShoppingBag' },
    { label: 'Analytics',   route: '/seller/analytics',   icon: 'TrendingUp' },
  ]
};
```

---

## 7. Admin Restrictions (Hard Rules)

Admin **cannot**:
- Manage, view, or delete SuperAdmin accounts
- Access platform-level security settings (JWT keys, CORS, rate limits)
- Modify core RBAC rules or role permissions
- View audit logs from SuperAdmin actions
- Access any seller's bank details directly (only payouts processed by system)

Seller **cannot**:
- Access other sellers' products, orders, or inventory
- Access customer personal information beyond shipping name
- Approve their own products (must wait for Admin/SuperAdmin approval)
- Create or apply coupons
- Access any admin or platform data

---

## 8. JWT Claims Structure

```json
{
  "sub": "a1b2c3d4-1234-5678-abcd-000000000001",
  "email": "seller01@mailinator.com",
  "role": ["Seller"],
  "sellerId": "b2c3d4e5-1234-5678-abcd-000000000002",
  "jti": "unique-token-id-for-blacklisting",
  "iss": "https://stylenest-auth.local",
  "aud": "stylenest-spa",
  "iat": 1716000000,
  "exp": 1716000900
}
```

**Notes:**
- `role` is always an array (even for single role)
- `sellerId` claim is only present when role includes `Seller`
- `jti` is stored in Redis blacklist on logout (TTL = remaining token expiry)
- No PII beyond email in JWT claims

---

*Cross-reference [ARCHITECTURE.md](ARCHITECTURE.md) for auth flow diagrams.*
*Cross-reference [SEEDER.md](SEEDER.md) for default account credentials.*
