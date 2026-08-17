import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { combineLatest, map } from 'rxjs';
import { SellerService } from '../../core/services/seller.service';
import { selectCurrentUser } from '../../store/auth/auth.selectors';

@Component({
  selector: 'app-seller-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, AsyncPipe],
  template: `
    <div class="p-4 md:p-8">
      @if (user$ | async; as user) {
        <p class="text-sm text-muted mb-1">Welcome back,</p>
        <h1 class="text-xl md:text-2xl font-display font-bold text-dark mb-6">
          {{ user.firstName }} {{ user.lastName }}
        </h1>
      } @else {
        <h1 class="text-xl md:text-2xl font-display font-bold text-dark mb-6">Seller Dashboard</h1>
      }

      <!-- Stats -->
      @if (stats$ | async; as stats) {
        <div class="grid grid-cols-2 gap-3 mb-8">
          <div class="bg-card rounded-xl border border-border p-5 text-center">
            <p class="text-3xl font-bold text-dark">{{ stats.products }}</p>
            <p class="text-sm text-muted mt-1">My Products</p>
          </div>
          <div class="bg-card rounded-xl border border-border p-5 text-center">
            <p class="text-3xl font-bold text-dark">{{ stats.orders }}</p>
            <p class="text-sm text-muted mt-1">My Orders</p>
          </div>
        </div>
      }

      <!-- Quick actions -->
      <h2 class="text-sm font-semibold text-muted uppercase tracking-widest mb-4">Quick Actions</h2>
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <a routerLink="/seller/products/new"
           class="bg-dark text-white rounded-xl p-5 flex items-center gap-4
                  hover:bg-navy transition-colors group">
          <div class="w-10 h-10 rounded-lg bg-white/10 flex items-center justify-center shrink-0">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
            </svg>
          </div>
          <div>
            <p class="font-semibold text-sm">Add New Product</p>
            <p class="text-xs text-white/60 mt-0.5">List a product on StyleNest Fashion</p>
          </div>
        </a>

        <a routerLink="/seller/products"
           class="bg-card rounded-xl border border-border p-5 flex items-center gap-4
                  hover:shadow-md transition-all group">
          <div class="w-10 h-10 rounded-lg bg-bg flex items-center justify-center shrink-0">
            <svg class="w-5 h-5 text-dark" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"/>
            </svg>
          </div>
          <div>
            <p class="font-semibold text-sm text-dark">Manage Products</p>
            <p class="text-xs text-muted mt-0.5">Edit, activate or remove listings</p>
          </div>
        </a>

        <a routerLink="/seller/orders"
           class="bg-card rounded-xl border border-border p-5 flex items-center gap-4
                  hover:shadow-md transition-all group sm:col-span-2 lg:col-span-1">
          <div class="w-10 h-10 rounded-lg bg-bg flex items-center justify-center shrink-0">
            <svg class="w-5 h-5 text-dark" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z"/>
            </svg>
          </div>
          <div>
            <p class="font-semibold text-sm text-dark">View Orders</p>
            <p class="text-xs text-muted mt-0.5">Track orders for your products</p>
          </div>
        </a>
      </div>
    </div>
  `,
})
export class SellerDashboardComponent {
  private readonly sellerService = inject(SellerService);
  private readonly store         = inject(Store);

  readonly user$ = this.store.select(selectCurrentUser);

  readonly stats$ = combineLatest([
    this.sellerService.getMyProducts(),
    this.sellerService.getMyOrders(),
  ]).pipe(
    map(([products, orders]) => ({
      products: products.length,
      orders:   orders.length,
    })),
  );
}
