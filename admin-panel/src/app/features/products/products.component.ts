import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { AsyncPipe, DecimalPipe } from '@angular/common';
import { Observable, catchError, of } from 'rxjs';
import { AdminApiService, AdminProduct, PagedResult } from '../../core/services/admin-api.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-products',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, DecimalPipe, StatusBadgeComponent],
  template: `
    <div class="space-y-4">
      <h1 class="text-xl font-bold text-dark">Product Management</h1>

      <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
        <div class="px-5 py-4 border-b border-border flex items-center justify-between">
          <h2 class="font-semibold text-dark text-sm">All Products</h2>
        </div>

        @if (products$ | async; as page) {
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-border text-xs text-muted uppercase tracking-wide bg-bg/50">
                  <th class="px-5 py-3 text-left">Product</th>
                  <th class="px-5 py-3 text-left">Brand / Category</th>
                  <th class="px-5 py-3 text-right">Price</th>
                  <th class="px-5 py-3 text-center">Stock</th>
                  <th class="px-5 py-3 text-center">Status</th>
                  <th class="px-5 py-3 text-center">Action</th>
                </tr>
              </thead>
              <tbody>
                @for (p of page.items; track p.id) {
                  <tr class="border-b border-border/50 hover:bg-bg/30">
                    <td class="px-5 py-3 font-medium text-dark">{{ p.name }}</td>
                    <td class="px-5 py-3 text-muted text-xs">{{ p.brandName }} / {{ p.categoryName }}</td>
                    <td class="px-5 py-3 text-right">₹{{ p.price | number:'1.0-0' }}</td>
                    <td class="px-5 py-3 text-center">
                      <app-status-badge [status]="p.inStock ? 'active' : 'rejected'" />
                    </td>
                    <td class="px-5 py-3 text-center">
                      <app-status-badge [status]="p.isActive ? 'active' : 'suspended'" />
                    </td>
                    <td class="px-5 py-3 text-center">
                      <button
                        (click)="toggleStatus(p)"
                        class="text-xs px-3 py-1 rounded-full border transition-colors"
                        [class.border-success]="!p.isActive"
                        [class.text-success]="!p.isActive"
                        [class.border-error]="p.isActive"
                        [class.text-error]="p.isActive">
                        {{ p.isActive ? 'Deactivate' : 'Activate' }}
                      </button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <div class="px-5 py-3 border-t border-border text-xs text-muted">
            Showing {{ page.items.length }} of {{ page.totalCount }} products
          </div>
        } @else {
          <div class="p-8 text-center text-muted text-sm">Loading products…</div>
        }
      </div>
    </div>
  `,
})
export class ProductsComponent implements OnInit {
  products$: Observable<PagedResult<AdminProduct>> | null = null;

  constructor(private api: AdminApiService) {}

  ngOnInit(): void {
    this.products$ = this.api.getProducts().pipe(catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: 20 })));
  }

  toggleStatus(p: AdminProduct): void {
    this.api.updateProductStatus(p.id, !p.isActive).subscribe();
  }
}
