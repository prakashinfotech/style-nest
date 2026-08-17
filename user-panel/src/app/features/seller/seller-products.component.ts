import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnDestroy, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { BehaviorSubject, Subject, switchMap, takeUntil } from 'rxjs';
import { SellerService } from '../../core/services/seller.service';

@Component({
  selector: 'app-seller-products',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, RouterLink],
  template: `
    <div class="p-4 md:p-8">
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-xl md:text-2xl font-display font-bold text-dark">My Products</h1>
        <a routerLink="/seller/products/new"
           class="flex items-center gap-2 bg-dark text-white text-sm font-medium
                  px-4 py-2 rounded-lg hover:bg-navy transition-colors">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
          </svg>
          Add Product
        </a>
      </div>

      @if (deleteError()) {
        <div class="mb-4 p-3 bg-red/10 border border-red/20 rounded-lg text-sm text-red">
          {{ deleteError() }}
        </div>
      }

      @if (products$ | async; as products) {
        @if (products.length === 0) {
          <div class="bg-card rounded-xl border border-border p-12 text-center">
            <p class="text-muted mb-4">You haven't listed any products yet.</p>
            <a routerLink="/seller/products/new"
               class="inline-flex items-center gap-2 bg-dark text-white text-sm
                      px-4 py-2 rounded-lg hover:bg-navy transition-colors">
              List your first product
            </a>
          </div>
        } @else {
          <p class="text-sm text-muted mb-4">{{ products.length }} product(s) listed</p>
          <div class="bg-card rounded-xl border border-border overflow-hidden">
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead class="bg-bg border-b border-border">
                  <tr>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider">Product</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider hidden md:table-cell">Category</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider">Price</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider hidden md:table-cell">Stock</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-border">
                  @for (product of products; track product.id) {
                    <tr class="hover:bg-bg/50 transition-colors">
                      <td class="px-4 py-3">
                        <p class="font-medium text-dark line-clamp-1">{{ product.name }}</p>
                        <p class="text-xs text-muted">{{ product.brandName }}</p>
                      </td>
                      <td class="px-4 py-3 text-muted hidden md:table-cell">{{ product.categoryName }}</td>
                      <td class="px-4 py-3">
                        <p class="font-semibold text-dark">₹{{ product.price.toLocaleString('en-IN') }}</p>
                        @if (product.mrp) {
                          <p class="text-xs text-muted line-through">₹{{ product.mrp.toLocaleString('en-IN') }}</p>
                        }
                      </td>
                      <td class="px-4 py-3 hidden md:table-cell">
                        <span [class]="product.inStock ? 'text-xs text-success' : 'text-xs text-red'">
                          {{ product.inStock ? 'In Stock' : 'Out of Stock' }}
                        </span>
                      </td>
                      <td class="px-4 py-3">
                        <button (click)="removeProduct(product.id)"
                                class="text-xs text-red hover:underline font-medium">
                          Remove
                        </button>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>
        }
      } @else {
        <div class="space-y-2">
          @for (i of [1, 2, 3]; track i) {
            <div class="h-14 bg-border/40 rounded-lg animate-pulse"></div>
          }
        </div>
      }
    </div>
  `,
})
export class SellerProductsComponent implements OnDestroy {
  private readonly sellerService = inject(SellerService);
  private readonly destroy$      = new Subject<void>();

  private readonly refresh$ = new BehaviorSubject<void>(undefined);
  readonly products$ = this.refresh$.pipe(
    switchMap(() => this.sellerService.getMyProducts()),
  );

  readonly deleteError = signal<string | null>(null);

  removeProduct(id: string): void {
    this.sellerService.deleteProduct(id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.deleteError.set(null);
        this.refresh$.next();
      },
      error: () => this.deleteError.set('Failed to remove product. Please try again.'),
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
