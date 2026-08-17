import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BehaviorSubject, Observable, catchError, of, switchMap } from 'rxjs';
import { SellerApiService, SellerProduct, PagedResult } from '../../../core/services/seller-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-seller-products',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, RouterLink, StatusBadgeComponent],
  template: `
    <div class="space-y-4">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-xl font-bold text-dark">My Products</h1>
          <span class="text-xs text-muted">
            @if (result$ | async; as r) { {{ r.totalCount }} total }
          </span>
        </div>
        <a routerLink="/seller/products/create"
          class="px-4 py-2 bg-navy text-white text-sm rounded-lg hover:bg-navy/90 transition-colors">
          + Add Product
        </a>
      </div>

      <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
        @if (result$ | async; as r) {
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-border text-xs text-muted uppercase tracking-wide bg-bg/50">
                  <th class="px-5 py-3 text-left">Product</th>
                  <th class="px-5 py-3 text-left">Category</th>
                  <th class="px-5 py-3 text-left">Brand</th>
                  <th class="px-5 py-3 text-right">Price</th>
                  <th class="px-5 py-3 text-center">Stock</th>
                  <th class="px-5 py-3 text-center">Status</th>
                  <th class="px-5 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                @for (p of r.items; track p.id) {
                  <tr class="border-b border-border/50 hover:bg-bg/30">
                    <td class="px-5 py-3 font-medium text-dark">{{ p.name }}</td>
                    <td class="px-5 py-3 text-muted text-xs">{{ p.categoryName }}</td>
                    <td class="px-5 py-3 text-muted text-xs">{{ p.brandName }}</td>
                    <td class="px-5 py-3 text-right">
                      @if (p.discountedPrice) {
                        <span class="text-red font-medium">₹{{ p.discountedPrice }}</span>
                        <span class="text-muted line-through text-xs ml-1">₹{{ p.basePrice }}</span>
                      } @else {
                        <span class="font-medium">₹{{ p.basePrice }}</span>
                      }
                    </td>
                    <td class="px-5 py-3 text-center text-muted">{{ p.stockQuantity }}</td>
                    <td class="px-5 py-3 text-center">
                      <app-status-badge [status]="p.isActive ? 'active' : 'suspended'" />
                    </td>
                    <td class="px-5 py-3 text-right">
                      <a [routerLink]="['/seller/products', p.id, 'edit']"
                        class="text-xs font-medium px-2.5 py-1 rounded border border-border text-dark hover:bg-bg transition-colors">
                        Edit
                      </a>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          @if (!r.items.length) {
            <div class="p-8 text-center text-muted text-sm">No products found.</div>
          }
          <div class="px-5 py-3 border-t border-border flex items-center justify-between text-xs text-muted">
            <span>Page {{ page }} of {{ Math.ceil(r.totalCount / pageSize) }}</span>
            <div class="flex gap-2">
              <button (click)="prevPage()" [disabled]="page === 1"
                class="px-3 py-1 border border-border rounded disabled:opacity-40 hover:bg-bg transition-colors">Prev</button>
              <button (click)="nextPage()" [disabled]="page * pageSize >= r.totalCount"
                class="px-3 py-1 border border-border rounded disabled:opacity-40 hover:bg-bg transition-colors">Next</button>
            </div>
          </div>
        } @else {
          <div class="p-8 text-center text-muted text-sm">Loading products…</div>
        }
      </div>
    </div>
  `,
})
export class SellerProductsComponent implements OnInit {
  page = 1;
  pageSize = 20;
  Math = Math;

  page$ = new BehaviorSubject<number>(1);
  result$: Observable<PagedResult<SellerProduct>> | null = null;

  constructor(private api: SellerApiService) {}

  ngOnInit(): void {
    this.result$ = this.page$.pipe(
      switchMap((p) => this.api.getProducts(p, this.pageSize).pipe(
        catchError(() => of({ items: [], totalCount: 0, page: p, pageSize: this.pageSize }))
      ))
    );
  }

  prevPage(): void { if (this.page > 1) { this.page--; this.page$.next(this.page); } }
  nextPage(): void { this.page++; this.page$.next(this.page); }
}
