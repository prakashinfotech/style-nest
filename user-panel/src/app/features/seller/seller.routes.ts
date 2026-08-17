import { Routes } from '@angular/router';

export const sellerRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./seller-layout.component').then((m) => m.SellerLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./seller-dashboard.component').then((m) => m.SellerDashboardComponent),
      },
      {
        path: 'products',
        loadComponent: () =>
          import('./seller-products.component').then((m) => m.SellerProductsComponent),
      },
      {
        path: 'products/new',
        loadComponent: () =>
          import('./seller-product-form.component').then((m) => m.SellerProductFormComponent),
      },
      {
        path: 'orders',
        loadComponent: () =>
          import('./seller-orders.component').then((m) => m.SellerOrdersComponent),
      },
    ],
  },
];
