import { ChangeDetectionStrategy, Component, EventEmitter, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export type PaymentMethod = 'card' | 'upi' | 'netbanking' | 'cod';

@Component({
  selector: 'app-payment-step',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  template: `
    <section>
      <h2 class="text-base font-bold text-dark mb-4">Payment Method</h2>

      <div class="space-y-3">
        @for (option of paymentOptions; track option.value) {
          <label
            class="flex items-center gap-3 border-2 rounded-lg px-4 py-3 cursor-pointer transition"
            [class.border-navy]="selected() === option.value"
            [class.border-gray-200]="selected() !== option.value"
          >
            <input
              type="radio"
              name="payment"
              [value]="option.value"
              [checked]="selected() === option.value"
              class="accent-navy"
              (change)="selected.set(option.value)"
            />
            <span class="text-xl">{{ option.icon }}</span>
            <div>
              <p class="text-sm font-medium text-dark">{{ option.label }}</p>
              <p class="text-xs text-muted">{{ option.description }}</p>
            </div>
          </label>
        }
      </div>

      <button
        class="w-full bg-navy text-white font-semibold py-3 rounded-lg hover:bg-blue transition mt-6"
        (click)="paymentSubmit.emit(selected())"
      >
        PLACE ORDER
      </button>
    </section>
  `,
})
export class PaymentStepComponent {
  @Output() paymentSubmit = new EventEmitter<PaymentMethod>();

  readonly selected = signal<PaymentMethod>('card');

  readonly paymentOptions: Array<{ value: PaymentMethod; label: string; description: string; icon: string }> = [
    { value: 'card',       label: 'Credit / Debit Card', description: 'Visa, Mastercard, Amex, RuPay', icon: '💳' },
    { value: 'upi',        label: 'UPI',                  description: 'GPay, PhonePe, Paytm UPI',      icon: '📱' },
    { value: 'netbanking', label: 'Net Banking',           description: 'All major Indian banks',         icon: '🏦' },
    { value: 'cod',        label: 'Cash on Delivery',      description: 'Pay when you receive',           icon: '💵' },
  ];
}
