import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-seller-layout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="min-h-screen bg-bg flex flex-col md:flex-row">

      <!-- Desktop sidebar -->
      <aside class="hidden md:flex flex-col w-60 bg-dark text-white shrink-0 min-h-screen">
        <div class="px-6 py-5 border-b border-white/10">
          <span class="font-display text-lg font-bold text-gold tracking-wide">Seller Hub</span>
          <p class="text-xs text-white/40 mt-0.5">StyleNest Fashion</p>
        </div>

        <nav class="flex-1 py-4 space-y-0.5 px-3">
          <a routerLink="/seller" [routerLinkActiveOptions]="{exact:true}" routerLinkActive="bg-white/20 font-medium"
             class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm text-white/80
                    hover:bg-white/10 hover:text-white transition-colors">
            <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"/>
            </svg>
            Dashboard
          </a>

          <a routerLink="/seller/products" routerLinkActive="bg-white/20 font-medium"
             class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm text-white/80
                    hover:bg-white/10 hover:text-white transition-colors">
            <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"/>
            </svg>
            My Products
          </a>

          <a routerLink="/seller/orders" routerLinkActive="bg-white/20 font-medium"
             class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm text-white/80
                    hover:bg-white/10 hover:text-white transition-colors">
            <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z"/>
            </svg>
            My Orders
          </a>
        </nav>

        <div class="px-6 py-4 border-t border-white/10">
          <a routerLink="/" class="text-xs text-white/50 hover:text-white/80 transition-colors">
            ← Back to store
          </a>
        </div>
      </aside>

      <!-- Mobile top bar -->
      <div class="md:hidden sticky top-0 z-30 bg-dark text-white shadow-md">
        <div class="flex items-center justify-between px-4 py-3">
          <span class="font-display font-bold text-gold text-sm">Seller Hub</span>
          <a routerLink="/" class="text-white/60 text-xs">← Store</a>
        </div>
        <nav class="flex gap-1 px-3 pb-2">
          <a routerLink="/seller" [routerLinkActiveOptions]="{exact:true}" routerLinkActive="bg-white/20"
             class="px-3 py-1.5 rounded text-xs text-white/80 hover:bg-white/10">Dashboard</a>
          <a routerLink="/seller/products" routerLinkActive="bg-white/20"
             class="px-3 py-1.5 rounded text-xs text-white/80 hover:bg-white/10">Products</a>
          <a routerLink="/seller/orders" routerLinkActive="bg-white/20"
             class="px-3 py-1.5 rounded text-xs text-white/80 hover:bg-white/10">Orders</a>
        </nav>
      </div>

      <!-- Content area -->
      <main class="flex-1">
        <router-outlet />
      </main>
    </div>
  `,
})
export class SellerLayoutComponent {}
