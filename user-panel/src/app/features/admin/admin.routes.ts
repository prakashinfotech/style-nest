import { Routes } from '@angular/router';

export const adminRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./admin-layout.component').then((m) => m.AdminLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./admin-dashboard.component').then((m) => m.AdminDashboardComponent),
      },
      {
        path: 'products',
        loadComponent: () =>
          import('./admin-products.component').then((m) => m.AdminProductsComponent),
      },
      {
        path: 'orders',
        loadComponent: () =>
          import('./admin-orders.component').then((m) => m.AdminOrdersComponent),
      },
      {
        path: 'users',
        loadComponent: () =>
          import('./admin-users.component').then((m) => m.AdminUsersComponent),
      },
      {
        path: 'banners',
        loadComponent: () =>
          import('./banner-list.component').then((m) => m.BannerListComponent),
      },
      {
        path: 'coupons',
        loadComponent: () =>
          import('./coupon-list.component').then((m) => m.CouponListComponent),
      },
      // ENH-ADMIN-002 — Job Management UI
      {
        path: 'jobs',
        loadComponent: () =>
          import('./admin-job-management.component').then((m) => m.AdminJobManagementComponent),
      },
    ],
  },
];
