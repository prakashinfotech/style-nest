import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { AsyncPipe, DatePipe } from '@angular/common';
import { BehaviorSubject, Observable, catchError, of, switchMap } from 'rxjs';
import { SellerApiService, SellerOrder, PagedResult } from '../../../core/services/seller-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

const STATUS_OPTIONS = ['Processing', 'Shipped', 'Delivered', 'Cancelled'];

@Component({
  selector: 'app-seller-orders',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, DatePipe, StatusBadgeComponent],
  template: `
    <div class="space-y-4">
      <div class="flex items-center justify-between">
        <h1 class="text-xl font-bold text-dark">My Orders</h1>
        <span class="text-xs text-muted">
          @if (result$ | async; as r) { {{ r.totalCount }} total }
        </span>
      </div>

      <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
        @if (result$ | async; as r) {
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-border text-xs text-muted uppercase tracking-wide bg-bg/50">
                  <th class="px-5 py-3 text-left">Order #</th>
                  <th class="px-5 py-3 text-left">Customer</th>
                  <th class="px-5 py-3 text-center">Items</th>
                  <th class="px-5 py-3 text-right">Amount</th>
                  <th class="px-5 py-3 text-center">Status</th>
                  <th class="px-5 py-3 text-left">Date</th>
                  <th class="px-5 py-3 text-center">Action</th>
                </tr>
              </thead>
              <tbody>
                @for (o of r.items; track o.id) {
                  <tr class="border-b border-border/50 hover:bg-bg/30">
                    <td class="px-5 py-3 font-mono text-xs text-navy font-bold">{{ o.orderNumber }}</td>
                    <td class="px-5 py-3 font-medium text-dark">{{ o.customerName }}</td>
                    <td class="px-5 py-3 text-center text-muted">{{ o.itemCount }}</td>
                    <td class="px-5 py-3 text-right font-medium">₹{{ o.totalAmount }}</td>
                    <td class="px-5 py-3 text-center"><app-status-badge [status]="o.status" /></td>
                    <td class="px-5 py-3 text-muted text-xs">{{ o.createdAt | date:'mediumDate' }}</td>
                    <td class="px-5 py-3 text-center">
                      @if (o.status !== 'Delivered' && o.status !== 'Cancelled') {
                        <select
                          (change)="updateStatus(o.id, $any($event.target).value)"
                          class="text-xs border border-border rounded px-2 py-1 focus:outline-none focus:border-navy bg-white">
                          @for (s of statusOptions; track s) {
                            <option [value]="s" [selected]="o.status === s">{{ s }}</option>
                          }
                        </select>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          @if (!r.items.length) {
            <div class="p-8 text-center text-muted text-sm">No orders found.</div>
          }
          <div class="px-5 py-3 border-t border-border flex items-center justify-between text-xs text-muted">
            <span>Page {{ page }} of {{ Math.ceil(r.totalCount / pageSize) || 1 }}</span>
            <div class="flex gap-2">
              <button (click)="prevPage()" [disabled]="page === 1"
                class="px-3 py-1 border border-border rounded disabled:opacity-40 hover:bg-bg transition-colors">Prev</button>
              <button (click)="nextPage()" [disabled]="page * pageSize >= r.totalCount"
                class="px-3 py-1 border border-border rounded disabled:opacity-40 hover:bg-bg transition-colors">Next</button>
            </div>
          </div>
        } @else {
          <div class="p-8 text-center text-muted text-sm">Loading orders…</div>
        }
      </div>
    </div>
  `,
})
export class SellerOrdersComponent implements OnInit {
  page = 1;
  pageSize = 20;
  Math = Math;
  statusOptions = STATUS_OPTIONS;

  page$ = new BehaviorSubject<number>(1);
  result$: Observable<PagedResult<SellerOrder>> | null = null;

  constructor(private api: SellerApiService) {}

  ngOnInit(): void {
    this.result$ = this.page$.pipe(
      switchMap((p) => this.api.getOrders(p, this.pageSize).pipe(
        catchError(() => of({ items: [], totalCount: 0, page: p, pageSize: this.pageSize }))
      ))
    );
  }

  updateStatus(orderId: string, status: string): void {
    this.api.updateOrderStatus(orderId, status).subscribe(() => this.page$.next(this.page));
  }

  prevPage(): void { if (this.page > 1) { this.page--; this.page$.next(this.page); } }
  nextPage(): void { this.page++; this.page$.next(this.page); }
}
