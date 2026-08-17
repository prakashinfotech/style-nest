import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-order-confirmation',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="flex flex-col items-center justify-center py-16 text-center max-w-md mx-auto">
      <div class="w-20 h-20 bg-success/10 rounded-full flex items-center justify-center mb-6">
        <svg class="w-10 h-10 text-success" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
        </svg>
      </div>

      <h1 class="text-2xl font-bold text-dark mb-2">Order Placed! 🎉</h1>
      <p class="text-muted text-sm mb-6">
        Thank you for your order. We've received your order and will start processing it shortly.
      </p>

      @if (orderId) {
        <div class="bg-gray-50 rounded-lg px-6 py-4 mb-6 w-full">
          <p class="text-xs text-muted mb-1">Order ID</p>
          <p class="font-bold text-dark font-mono">{{ orderId }}</p>
        </div>
      }

      <p class="text-xs text-muted mb-8">
        A confirmation email has been sent to your registered email address.
      </p>

      <div class="flex flex-col sm:flex-row gap-3 w-full">
        <a routerLink="/account/orders"
           class="flex-1 border-2 border-navy text-navy font-semibold py-3 rounded-lg hover:bg-navy hover:text-white transition text-center text-sm">
          Track Order
        </a>
        <a routerLink="/"
           class="flex-1 bg-navy text-white font-semibold py-3 rounded-lg hover:bg-blue transition text-center text-sm">
          Continue Shopping
        </a>
      </div>
    </div>
  `,
})
export class OrderConfirmationComponent {
  @Input() orderId: string | null = null;
}
