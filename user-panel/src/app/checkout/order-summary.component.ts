import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Cart } from '../core/models/cart.model';
import { CurrencyInrPipe } from '../shared/pipes/currency-inr.pipe';

@Component({
  selector: 'app-order-summary',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, CurrencyInrPipe],
  template: `
    <aside class="bg-card rounded-lg border border-gray-100 p-5 sticky top-20">
      <h2 class="text-sm font-bold text-dark mb-4">Order Summary</h2>

      <!-- Items -->
      <div class="space-y-3 mb-4 max-h-48 overflow-y-auto">
        @for (item of cart.items; track item.id) {
          <div class="flex gap-3 items-center text-xs">
            @if (item.imageUrl) {
              <img [src]="item.imageUrl" [alt]="item.name" class="w-10 h-12 object-cover rounded bg-gray-50" />
            } @else {
              <div class="w-10 h-12 bg-gray-100 rounded flex items-center justify-center text-lg">🛍️</div>
            }
            <div class="flex-1 min-w-0">
              <p class="font-medium truncate">{{ item.name }}</p>
              <p class="text-muted">Qty: {{ item.quantity }}</p>
            </div>
            <span class="font-semibold flex-shrink-0">
              {{ (item.salePrice ?? item.price) * item.quantity | currencyInr }}
            </span>
          </div>
        }
      </div>

      <!-- Totals -->
      <div class="border-t border-gray-100 pt-3 space-y-2 text-sm">
        <div class="flex justify-between text-dark/80">
          <span>Subtotal</span>
          <span>{{ cart.subtotal | currencyInr }}</span>
        </div>
        @if (cart.discount > 0) {
          <div class="flex justify-between text-success">
            <span>Discount</span>
            <span>− {{ cart.discount | currencyInr }}</span>
          </div>
        }
        <div class="flex justify-between text-dark/80">
          <span>Delivery</span>
          <span class="text-success">FREE</span>
        </div>
        <div class="flex justify-between font-bold text-base border-t border-gray-100 pt-2">
          <span>Total</span>
          <span>{{ cart.total | currencyInr }}</span>
        </div>
      </div>
    </aside>
  `,
})
export class OrderSummaryComponent {
  @Input({ required: true }) cart!: Cart;
}
