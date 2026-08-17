import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="min-h-screen bg-bg flex flex-col md:flex-row">

      <!-- Desktop sidebar -->
      <aside class="hidden md:flex flex-col w-60 bg-navy text-white shrink-0 min-h-screen">
        <div class="px-6 py-5 border-b border-white/10">
          <span class="font-display text-lg font-bold text-gold tracking-wide">StyleNest Admin</span>
        </div>

        <nav class="flex-1 py-4 space-y-0.5 px-3">
          <a routerLink="/admin" [routerLinkActiveOptions]="{exact:true}" routerLinkActive="bg-white/20 font-medium"
             class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm text-white/80
                    hover:bg-white/10 hover:text-white transition-colors">
            <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"/>
            </svg>
            Dashboard
          </a>

          <a routerLink="/admin/products" routerLinkActive="bg-white/20 font-medium"
             class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm text-white/80
                    hover:bg-white/10 hover:text-white transition-colors">
            <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"/>
            </svg>
            Products
          </a>

          <a routerLink="/admin/orders" routerLinkActive="bg-white/20 font-medium"
             class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm text-white/80
                    hover:bg-white/10 hover:text-white transition-colors">
            <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z"/>
            </svg>
            Orders
          </a>

          <a routerLink="/admin/users" routerLinkActive="bg-white/20 font-medium"
             class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm text-white/80
                    hover:bg-white/10 hover:text-white transition-colors">
            <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"/>
            </svg>
            Users
          </a>

          <div class="mx-3 my-3 border-t border-white/10"></div>

          <a routerLink="/admin/banners" routerLinkActive="bg-white/20 font-medium"
             class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm text-white/80
                    hover:bg-white/10 hover:text-white transition-colors">
            <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"/>
            </svg>
            Banners
          </a>

          <a routerLink="/admin/coupons" routerLinkActive="bg-white/20 font-medium"
             class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm text-white/80
                    hover:bg-white/10 hover:text-white transition-colors">
            <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z"/>
            </svg>
            Coupons
          </a>

          <!-- ENH-ADMIN-002 — Job Management -->
          <a routerLink="/admin/jobs" routerLinkActive="bg-white/20 font-medium"
             class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm text-white/80
                    hover:bg-white/10 hover:text-white transition-colors">
            <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
            </svg>
            Jobs
          </a>
        </nav>

        <div class="px-6 py-4 border-t border-white/10">
          <a routerLink="/" class="text-xs text-white/50 hover:text-white/80 transition-colors">
            ← Back to store
          </a>
        </div>
      </aside>

      <!-- Mobile top bar -->
      <div class="md:hidden sticky top-0 z-30 bg-navy text-white shadow-md">
        <div class="flex items-center justify-between px-4 py-3">
          <span class="font-display font-bold text-gold text-sm">StyleNest Admin</span>
          <a routerLink="/" class="text-white/60 text-xs">← Store</a>
        </div>
        <nav class="flex overflow-x-auto gap-1 px-3 pb-2">
          <a routerLink="/admin" [routerLinkActiveOptions]="{exact:true}" routerLinkActive="bg-white/20"
             class="shrink-0 px-3 py-1.5 rounded text-xs text-white/80 hover:bg-white/10 whitespace-nowrap">Home</a>
          <a routerLink="/admin/products" routerLinkActive="bg-white/20"
             class="shrink-0 px-3 py-1.5 rounded text-xs text-white/80 hover:bg-white/10 whitespace-nowrap">Products</a>
          <a routerLink="/admin/orders" routerLinkActive="bg-white/20"
             class="shrink-0 px-3 py-1.5 rounded text-xs text-white/80 hover:bg-white/10 whitespace-nowrap">Orders</a>
          <a routerLink="/admin/users" routerLinkActive="bg-white/20"
             class="shrink-0 px-3 py-1.5 rounded text-xs text-white/80 hover:bg-white/10 whitespace-nowrap">Users</a>
          <a routerLink="/admin/banners" routerLinkActive="bg-white/20"
             class="shrink-0 px-3 py-1.5 rounded text-xs text-white/80 hover:bg-white/10 whitespace-nowrap">Banners</a>
          <a routerLink="/admin/coupons" routerLinkActive="bg-white/20"
             class="shrink-0 px-3 py-1.5 rounded text-xs text-white/80 hover:bg-white/10 whitespace-nowrap">Coupons</a>
          <a routerLink="/admin/jobs" routerLinkActive="bg-white/20"
             class="shrink-0 px-3 py-1.5 rounded text-xs text-white/80 hover:bg-white/10 whitespace-nowrap">Jobs</a>
        </nav>
      </div>

      <!-- Content area -->
      <main class="flex-1">
        <router-outlet />
      </main>
    </div>
  `,
})
export class AdminLayoutComponent {}
