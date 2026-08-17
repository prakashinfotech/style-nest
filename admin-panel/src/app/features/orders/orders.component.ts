import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { AsyncPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Observable, catchError, of } from 'rxjs';
import { AdminApiService, AdminOrder, PagedResult } from '../../core/services/admin-api.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-orders',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, DatePipe, DecimalPipe, StatusBadgeComponent],
  template: `
    <div class="space-y-4">
      <h1 class="text-xl font-bold text-dark">Order Management</h1>

      <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
        <div class="px-5 py-4 border-b border-border">
          <h2 class="font-semibold text-dark text-sm">All Orders</h2>
        </div>

        @if (orders$ | async; as page) {
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-border text-xs text-muted uppercase tracking-wide bg-bg/50">
                  <th class="px-5 py-3 text-left">Order #</th>
                  <th class="px-5 py-3 text-left">Customer</th>
                  <th class="px-5 py-3 text-right">Amount</th>
                  <th class="px-5 py-3 text-center">Items</th>
                  <th class="px-5 py-3 text-center">Status</th>
                  <th class="px-5 py-3 text-left">Date</th>
                </tr>
              </thead>
              <tbody>
                @for (o of page.items; track o.id) {
                  <tr class="border-b border-border/50 hover:bg-bg/30">
                    <td class="px-5 py-3 font-mono text-xs font-medium text-dark">{{ o.orderNumber }}</td>
                    <td class="px-5 py-3 text-muted">{{ o.userEmail }}</td>
                    <td class="px-5 py-3 text-right font-medium">₹{{ o.totalAmount | number:'1.0-0' }}</td>
                    <td class="px-5 py-3 text-center text-muted">{{ o.itemCount }}</td>
                    <td class="px-5 py-3 text-center"><app-status-badge [status]="o.status" /></td>
                    <td class="px-5 py-3 text-muted text-xs">{{ o.createdAt | date:'mediumDate' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <div class="px-5 py-3 border-t border-border text-xs text-muted">
            Showing {{ page.items.length }} of {{ page.totalCount }} orders
          </div>
        } @else {
          <div class="p-8 text-center text-muted text-sm">Loading orders…</div>
        }
      </div>
    </div>
  `,
})
export class OrdersComponent implements OnInit {
  orders$: Observable<PagedResult<AdminOrder>> | null = null;

  constructor(private api: AdminApiService) {}

  ngOnInit(): void {
    this.orders$ = this.api.getOrders().pipe(catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: 20 })));
  }
}
