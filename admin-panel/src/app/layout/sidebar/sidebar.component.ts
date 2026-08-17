import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AsyncPipe } from '@angular/common';
import { Store } from '@ngrx/store';
import { selectIsSuperAdmin, selectIsAdmin, selectIsSeller, selectSidebarCollapsed } from '../../store';
import { toggleSidebar } from '../../store/ui/ui.actions';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  roles: string[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, AsyncPipe],
  template: `
    <aside
      class="h-full bg-navy text-white flex flex-col transition-all duration-300"
      [class.w-64]="!(collapsed$ | async)"
      [class.w-16]="collapsed$ | async">

      <!-- Logo -->
      <div class="flex items-center gap-3 px-4 py-5 border-b border-white/10">
        <div class="w-8 h-8 bg-red rounded-lg flex items-center justify-center flex-shrink-0 font-display font-bold text-sm">T</div>
        @if (!(collapsed$ | async)) {
          <span class="font-semibold text-sm leading-tight">StyleNest<br><span class="text-white/60 text-xs font-normal">Admin</span></span>
        }
      </div>

      <!-- Nav -->
      <nav class="flex-1 py-4 overflow-y-auto">
        @if (isAdmin$ | async) {
          <div class="px-3 mb-2">
            @if (!(collapsed$ | async)) {
              <p class="text-white/40 text-xs font-semibold uppercase tracking-wider px-1 mb-1">Admin</p>
            }
            @for (item of adminNav; track item.route) {
              <a [routerLink]="item.route"
                 routerLinkActive="bg-white/15 text-white"
                 class="flex items-center gap-3 px-3 py-2 rounded-lg text-white/70 hover:text-white hover:bg-white/10 transition-colors mb-1 text-sm"
                 [title]="(collapsed$ | async) ? item.label : ''">
                <span class="text-base flex-shrink-0">{{ item.icon }}</span>
                @if (!(collapsed$ | async)) { <span>{{ item.label }}</span> }
              </a>
            }
          </div>
        }

        @if (isSuperAdmin$ | async) {
          <div class="px-3 mb-2 mt-4">
            @if (!(collapsed$ | async)) {
              <p class="text-white/40 text-xs font-semibold uppercase tracking-wider px-1 mb-1">Super Admin</p>
            }
            @for (item of superAdminNav; track item.route) {
              <a [routerLink]="item.route"
                 routerLinkActive="bg-white/15 text-white"
                 class="flex items-center gap-3 px-3 py-2 rounded-lg text-white/70 hover:text-white hover:bg-white/10 transition-colors mb-1 text-sm"
                 [title]="(collapsed$ | async) ? item.label : ''">
                <span class="text-base flex-shrink-0">{{ item.icon }}</span>
                @if (!(collapsed$ | async)) { <span>{{ item.label }}</span> }
              </a>
            }
          </div>
        }

        @if (isSeller$ | async) {
          <div class="px-3 mb-2">
            @if (!(collapsed$ | async)) {
              <p class="text-white/40 text-xs font-semibold uppercase tracking-wider px-1 mb-1">Seller</p>
            }
            @for (item of sellerNav; track item.route) {
              <a [routerLink]="item.route"
                 routerLinkActive="bg-white/15 text-white"
                 class="flex items-center gap-3 px-3 py-2 rounded-lg text-white/70 hover:text-white hover:bg-white/10 transition-colors mb-1 text-sm"
                 [title]="(collapsed$ | async) ? item.label : ''">
                <span class="text-base flex-shrink-0">{{ item.icon }}</span>
                @if (!(collapsed$ | async)) { <span>{{ item.label }}</span> }
              </a>
            }
          </div>
        }
      </nav>

      <!-- Toggle -->
      <button
        (click)="toggle()"
        class="p-4 border-t border-white/10 text-white/50 hover:text-white text-sm text-left transition-colors">
        {{ (collapsed$ | async) ? '→' : '← Collapse' }}
      </button>
    </aside>
  `,
})
export class SidebarComponent {
  private readonly store = inject(Store);

  readonly collapsed$    = this.store.select(selectSidebarCollapsed);
  readonly isAdmin$      = this.store.select(selectIsAdmin);
  readonly isSuperAdmin$ = this.store.select(selectIsSuperAdmin);
  readonly isSeller$     = this.store.select(selectIsSeller);

  adminNav = [
    { label: 'Dashboard',         icon: '📊', route: '/dashboard' },
    { label: 'Products',          icon: '👗', route: '/products' },
    { label: 'Orders',            icon: '📦', route: '/orders' },
    { label: 'Users',             icon: '👥', route: '/users' },
    { label: 'Categories',        icon: '🗂️',  route: '/categories' },
    { label: 'Brands',            icon: '🏷️',  route: '/brands' },
    { label: 'Banners',           icon: '🖼️',  route: '/banners' },
    { label: 'Coupons',           icon: '🎟️',  route: '/coupons' },
    { label: 'Review Moderation', icon: '⭐', route: '/review-moderation' },
    { label: 'Search Synonyms',  icon: '🔤', route: '/search-synonyms' },
  ];

  superAdminNav = [
    { label: 'Sellers',           icon: '🏪', route: '/super/sellers' },
    { label: 'Admins',            icon: '🛡️',  route: '/super/admins' },
    { label: 'RBAC',              icon: '🔐', route: '/super/rbac' },
    { label: 'Audit Logs',        icon: '📋', route: '/super/audit-logs' },
    { label: 'Platform Settings', icon: '⚙️',  route: '/super/platform-settings' },
  ];

  sellerNav = [
    { label: 'My Dashboard', icon: '📊', route: '/seller/dashboard' },
    { label: 'Products',     icon: '👗', route: '/seller/products' },
    { label: 'Inventory',    icon: '📦', route: '/seller/inventory' },
    { label: 'Orders',       icon: '🛍️',  route: '/seller/orders' },
    { label: 'Analytics',    icon: '📈', route: '/seller/analytics' },
    { label: 'Payouts',      icon: '💳', route: '/seller/payouts' },
  ];

  toggle(): void {
    this.store.dispatch(toggleSidebar());
  }
}
