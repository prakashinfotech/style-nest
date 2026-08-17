import { AsyncPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminOrder, AdminService } from '../../core/services/admin.service';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, DatePipe],
  template: `
    <div class="p-4 md:p-8">
      <h1 class="text-xl md:text-2xl font-display font-bold text-dark mb-6">Orders</h1>

      @if (orders$ | async; as orders) {
        @if (orders.length === 0) {
          <div class="bg-card rounded-xl border border-border p-12 text-center text-muted">
            No orders yet.
          </div>
        } @else {
          <p class="text-sm text-muted mb-4">{{ orders.length }} orders total</p>
          <div class="bg-card rounded-xl border border-border overflow-hidden">
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead class="bg-bg border-b border-border">
                  <tr>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider">Order #</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider hidden md:table-cell">Customer</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider">Amount</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider">Status</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider hidden lg:table-cell">Items</th>
                    <th class="px-4 py-3 text-left text-xs font-semibold text-muted uppercase tracking-wider hidden lg:table-cell">Date</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-border">
                  @for (order of orders; track order.id) {
                    <tr class="hover:bg-bg/50 transition-colors">
                      <td class="px-4 py-3 font-mono text-xs font-semibold text-navy">
                        #{{ order.orderNumber }}
                      </td>
                      <td class="px-4 py-3 text-dark hidden md:table-cell">{{ order.userEmail }}</td>
                      <td class="px-4 py-3 font-semibold text-dark">
                        ₹{{ order.totalAmount.toLocaleString('en-IN') }}
                      </td>
                      <td class="px-4 py-3">
                        <span [class]="statusClass(order.status)">{{ order.status }}</span>
                      </td>
                      <td class="px-4 py-3 text-muted hidden lg:table-cell">{{ order.itemCount }}</td>
                      <td class="px-4 py-3 text-muted text-xs hidden lg:table-cell">
                        {{ order.createdAt | date:'dd MMM yyyy' }}
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
          @for (i of [1, 2, 3, 4, 5]; track i) {
            <div class="h-12 bg-border/40 rounded-lg animate-pulse"></div>
          }
        </div>
      }
    </div>
  `,
})
export class AdminOrdersComponent {
  private readonly adminService = inject(AdminService);
  readonly orders$: Observable<AdminOrder[]> = this.adminService.getAdminOrders();

  statusClass(status: string): string {
    const base = 'text-xs font-medium px-2 py-0.5 rounded-full';
    switch (status.toLowerCase()) {
      case 'confirmed':
      case 'delivered': return `${base} bg-success/10 text-success`;
      case 'cancelled': return `${base} bg-red/10 text-red`;
      case 'pending':   return `${base} bg-gold/10 text-gold`;
      default:          return `${base} bg-muted/10 text-muted`;
    }
  }
}
