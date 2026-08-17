import { AsyncPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SellerOrder, SellerService } from '../../core/services/seller.service';

@Component({
  selector: 'app-seller-orders',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe, DatePipe],
  template: `
    <div class="p-4 md:p-8">
      <h1 class="text-xl md:text-2xl font-display font-bold text-dark mb-6">My Orders</h1>

      @if (orders$ | async; as orders) {
        @if (orders.length === 0) {
          <div class="bg-card rounded-xl border border-border p-12 text-center text-muted">
            No orders for your products yet.
          </div>
        } @else {
          <p class="text-sm text-muted mb-4">{{ orders.length }} order(s)</p>
          <div class="space-y-4">
            @for (order of orders; track order.id) {
              <div class="bg-card rounded-xl border border-border p-4 md:p-5">
                <!-- Header row -->
                <div class="flex items-start justify-between gap-3 mb-4">
                  <div>
                    <p class="text-xs text-muted font-mono">ORDER</p>
                    <p class="font-bold text-dark">#{{ order.orderNumber }}</p>
                  </div>
                  <span [class]="statusClass(order.status)">{{ order.status }}</span>
                </div>

                <!-- Meta grid -->
                <div class="grid grid-cols-2 gap-3 text-sm mb-4">
                  <div>
                    <p class="text-xs text-muted">Customer</p>
                    <p class="font-medium text-dark">{{ order.customerName }}</p>
                  </div>
                  <div>
                    <p class="text-xs text-muted">Total</p>
                    <p class="font-semibold text-dark">₹{{ order.totalAmount.toLocaleString('en-IN') }}</p>
                  </div>
                  <div class="col-span-2">
                    <p class="text-xs text-muted">Placed</p>
                    <p class="text-dark">{{ order.createdAt | date:'dd MMM yyyy, h:mm a' }}</p>
                  </div>
                </div>

                <!-- Items -->
                <div class="border-t border-border pt-3 space-y-2">
                  @for (item of order.items; track item.productId) {
                    <div class="flex justify-between text-sm">
                      <span class="text-dark">{{ item.productName }}
                        <span class="text-muted">× {{ item.quantity }}</span>
                      </span>
                      <span class="text-muted font-medium">
                        ₹{{ (item.unitPrice * item.quantity).toLocaleString('en-IN') }}
                      </span>
                    </div>
                  }
                </div>
              </div>
            }
          </div>
        }
      } @else {
        <div class="space-y-3">
          @for (i of [1, 2, 3]; track i) {
            <div class="h-36 bg-border/40 rounded-xl animate-pulse"></div>
          }
        </div>
      }
    </div>
  `,
})
export class SellerOrdersComponent {
  private readonly sellerService = inject(SellerService);
  readonly orders$: Observable<SellerOrder[]> = this.sellerService.getMyOrders();

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
