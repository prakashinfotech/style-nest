import { AsyncPipe, CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../core/services/admin.service';

interface NavTile {
  label: string;
  description: string;
  link: string;
  iconPath: string;
  accent: string;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, AsyncPipe, CurrencyPipe],
  template: `
    <div class="p-4 md:p-8">
      <h1 class="text-xl md:text-2xl font-display font-bold text-dark mb-2">Admin Dashboard</h1>
      <p class="text-sm text-muted mb-8">Manage your StyleNest Fashion store.</p>

      <!-- Real metrics row -->
      @if (metrics$ | async; as m) {
        <div class="grid grid-cols-2 lg:grid-cols-4 gap-3 mb-8">
          <div class="bg-card rounded-xl border border-border p-4 text-center">
            <p class="text-3xl font-bold text-navy">{{ m.totalOrders }}</p>
            <p class="text-xs text-muted mt-1">Total Orders</p>
          </div>
          <div class="bg-card rounded-xl border border-border p-4 text-center">
            <p class="text-2xl font-bold text-navy">{{ m.totalRevenue | currency:'INR':'symbol-narrow':'1.0-0' }}</p>
            <p class="text-xs text-muted mt-1">Total Revenue</p>
          </div>
          <div class="bg-card rounded-xl border border-border p-4 text-center">
            <p class="text-3xl font-bold text-navy">{{ m.totalUsers }}</p>
            <p class="text-xs text-muted mt-1">Total Users</p>
          </div>
          <div class="bg-card rounded-xl border border-border p-4 text-center">
            <p class="text-3xl font-bold text-navy">{{ m.totalProducts }}</p>
            <p class="text-xs text-muted mt-1">Total Products</p>
          </div>
        </div>
      }

      <!-- Nav tiles grid -->
      <h2 class="text-sm font-semibold text-muted uppercase tracking-widest mb-4">Management</h2>
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        @for (tile of navTiles; track tile.link) {
          <a [routerLink]="tile.link"
             class="bg-card rounded-xl border border-border p-5 flex items-center gap-4
                    hover:shadow-md hover:-translate-y-0.5 transition-all duration-200 group">
            <div class="w-11 h-11 rounded-xl flex items-center justify-center shrink-0"
                 [class]="tile.accent">
              <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      [attr.d]="tile.iconPath"/>
              </svg>
            </div>
            <div class="min-w-0">
              <p class="font-semibold text-dark text-sm group-hover:text-navy transition-colors">
                {{ tile.label }}
              </p>
              <p class="text-xs text-muted mt-0.5 line-clamp-1">{{ tile.description }}</p>
            </div>
            <svg class="w-4 h-4 text-mid-gray ml-auto shrink-0 group-hover:translate-x-0.5 transition-transform"
                 fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
            </svg>
          </a>
        }
      </div>
    </div>
  `,
})
export class AdminDashboardComponent {
  private readonly adminService = inject(AdminService);

  readonly metrics$ = this.adminService.getDashboardMetrics();

  readonly navTiles: NavTile[] = [
    {
      label:       'Products',
      description: 'Browse and manage the full catalog',
      link:        '/admin/products',
      iconPath:    'M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4',
      accent:      'bg-navy',
    },
    {
      label:       'Orders',
      description: 'View and process customer orders',
      link:        '/admin/orders',
      iconPath:    'M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z',
      accent:      'bg-blue',
    },
    {
      label:       'Users',
      description: 'Manage customers, sellers and admins',
      link:        '/admin/users',
      iconPath:    'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z',
      accent:      'bg-gold',
    },
    {
      label:       'Banners',
      description: 'Hero, promo and flash-sale banners',
      link:        '/admin/banners',
      iconPath:    'M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z',
      accent:      'bg-red',
    },
    {
      label:       'Coupons',
      description: 'Create and toggle discount codes',
      link:        '/admin/coupons',
      iconPath:    'M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z',
      accent:      'bg-success',
    },
  ];
}
