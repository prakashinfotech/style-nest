import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AsyncPipe } from '@angular/common';
import { map } from 'rxjs';

@Component({
  selector: 'app-order-confirmed',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, AsyncPipe],
  template: `
    <div class="min-h-screen bg-bg flex items-center justify-center px-4">
      <div class="w-full max-w-md bg-white rounded-2xl shadow-lg p-8 text-center">

        <!-- Success icon -->
        <div class="w-20 h-20 bg-green-50 rounded-full flex items-center justify-center mx-auto mb-6">
          <svg class="w-10 h-10 text-success" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
          </svg>
        </div>

        <h1 class="text-2xl font-bold text-dark mb-2">Order Confirmed!</h1>
        <p class="text-muted text-sm mb-6">
          Your purchase was successful. We'll start processing your order right away.
        </p>

        @if (orderNumber$ | async; as num) {
          <div class="bg-gray-50 rounded-lg px-6 py-4 mb-6">
            <p class="text-xs text-muted mb-1">Order Number</p>
            <p class="font-bold text-dark font-mono text-lg">{{ num }}</p>
          </div>
        }

        <p class="text-xs text-muted mb-8">
          Delivery estimated in 3–5 business days.
        </p>

        <div class="flex flex-col sm:flex-row gap-3">
          <a routerLink="/products"
             class="flex-1 border-2 border-navy text-navy font-semibold py-3 rounded-lg hover:bg-navy hover:text-white transition text-sm">
            Continue Shopping
          </a>
          <a routerLink="/"
             class="flex-1 bg-navy text-white font-semibold py-3 rounded-lg hover:bg-blue transition text-sm">
            Go Home
          </a>
        </div>
      </div>
    </div>
  `,
})
export class OrderConfirmedComponent {
  private readonly route = inject(ActivatedRoute);

  readonly orderNumber$ = this.route.queryParamMap.pipe(
    map((p) => p.get('orderNumber'))
  );
}
