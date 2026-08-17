import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';
import { superAdminGuard } from './core/guards/super-admin.guard';
import { sellerGuard } from './core/guards/seller.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    loadComponent: () => import('./layout/admin-layout/admin-layout.component').then((m) => m.AdminLayoutComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

      // Admin + SuperAdmin routes
      {
        path: 'dashboard',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'products',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/products/products.component').then((m) => m.ProductsComponent),
      },
      {
        path: 'orders',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/orders/orders.component').then((m) => m.OrdersComponent),
      },
      {
        path: 'users',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/users/users.component').then((m) => m.UsersComponent),
      },
      {
        path: 'categories',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/categories/categories.component').then((m) => m.CategoriesComponent),
      },
      {
        path: 'brands',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/brands/brands.component').then((m) => m.BrandsComponent),
      },
      {
        path: 'banners',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/banners/banners.component').then((m) => m.BannersComponent),
      },
      {
        path: 'coupons',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/coupons/coupons.component').then((m) => m.CouponsComponent),
      },

      // SuperAdmin-only routes
      {
        path: 'super/sellers',
        canActivate: [superAdminGuard],
        loadComponent: () => import('./features/sellers/sellers.component').then((m) => m.SellersComponent),
      },
      {
        path: 'super/admins',
        canActivate: [superAdminGuard],
        loadComponent: () => import('./features/admins/admins.component').then((m) => m.AdminsComponent),
      },
      {
        path: 'super/rbac',
        canActivate: [superAdminGuard],
        loadComponent: () => import('./features/rbac/rbac.component').then((m) => m.RbacComponent),
      },
      {
        path: 'super/platform-settings',
        canActivate: [superAdminGuard],
        loadComponent: () => import('./features/platform-settings/platform-settings.component').then((m) => m.PlatformSettingsComponent),
      },
      {
        path: 'super/audit-logs',
        canActivate: [superAdminGuard],
        loadComponent: () => import('./features/audit-logs/audit-logs.component').then((m) => m.AuditLogsComponent),
      },

      // Admin moderation routes
      {
        path: 'review-moderation',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/review-moderation/review-moderation.component').then((m) => m.ReviewModerationComponent),
      },
      // ENH-ADMIN-006 — Search Synonym Management
      {
        path: 'search-synonyms',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/search-synonyms/search-synonyms.component').then((m) => m.SearchSynonymsComponent),
      },

      // Seller routes
      {
        path: 'seller',
        canActivate: [sellerGuard],
        children: [
          { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
          {
            path: 'dashboard',
            loadComponent: () => import('./features/seller/seller-dashboard/seller-dashboard.component').then((m) => m.SellerDashboardComponent),
          },
          {
            path: 'products',
            loadComponent: () => import('./features/seller/seller-products/seller-products.component').then((m) => m.SellerProductsComponent),
          },
          {
            path: 'products/create',
            loadComponent: () => import('./features/seller/seller-product-form/seller-product-form.component').then((m) => m.SellerProductFormComponent),
          },
          {
            path: 'products/:id/edit',
            loadComponent: () => import('./features/seller/seller-product-form/seller-product-form.component').then((m) => m.SellerProductFormComponent),
          },
          {
            path: 'inventory',
            loadComponent: () => import('./features/seller/seller-inventory/seller-inventory.component').then((m) => m.SellerInventoryComponent),
          },
          {
            path: 'orders',
            loadComponent: () => import('./features/seller/seller-orders/seller-orders.component').then((m) => m.SellerOrdersComponent),
          },
          {
            path: 'analytics',
            loadComponent: () => import('./features/seller/seller-analytics/seller-analytics.component').then((m) => m.SellerAnalyticsComponent),
          },
          {
            path: 'payouts',
            loadComponent: () => import('./features/seller/seller-payouts/seller-payouts.component').then((m) => m.SellerPayoutsComponent),
          },
        ],
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
