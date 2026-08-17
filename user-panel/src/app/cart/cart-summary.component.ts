import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Cart } from '../core/models/cart.model';
import { CurrencyInrPipe } from '../shared/pipes/currency-inr.pipe';

@Component({
  selector: 'app-cart-summary',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, CurrencyInrPipe],
  template: `
    <aside class="bg-card rounded-lg border border-gray-100 p-5 sticky top-20" aria-label="Order summary">
      <h2 class="text-base font-bold text-dark mb-4">Price Details</h2>

      <div class="space-y-3 text-sm mb-4">
        <div class="flex justify-between">
          <span class="text-dark/80">Subtotal ({{ itemCount }} items)</span>
          <span class="font-medium">{{ cart.subtotal | currencyInr }}</span>
        </div>

        @if (cart.discount > 0) {
          <div class="flex justify-between text-success">
            <span>Discount</span>
            <span>− {{ cart.discount | currencyInr }}</span>
          </div>
        }

        <div class="flex justify-between">
          <span class="text-dark/80">Delivery</span>
          <span class="text-success font-medium">FREE</span>
        </div>

        @if (cart.couponCode) {
          <div class="flex justify-between text-success">
            <span>Coupon ({{ cart.couponCode }})</span>
            <span>Applied ✓</span>
          </div>
        }
      </div>

      <div class="border-t border-gray-100 pt-3 flex justify-between font-bold text-base mb-5">
        <span class="text-dark">Total</span>
        <span class="text-dark">{{ cart.total | currencyInr }}</span>
      </div>

      @if (cart.discount > 0) {
        <p class="text-success text-xs mb-4 font-medium">
          You save {{ cart.discount | currencyInr }} on this order 🎉
        </p>
      }

      <a
        routerLink="/checkout"
        class="block w-full bg-navy text-white text-center font-semibold py-3 rounded-lg hover:bg-blue transition"
      >
        PROCEED TO CHECKOUT
      </a>
    </aside>
  `,
})
export class CartSummaryComponent {
  @Input({ required: true }) cart!: Cart;
  @Input() itemCount = 0;
}
