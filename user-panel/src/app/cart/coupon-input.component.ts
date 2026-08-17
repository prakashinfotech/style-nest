import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-coupon-input',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="border border-gray-200 rounded-lg p-4">
      <h3 class="text-sm font-semibold text-dark mb-3">Apply Coupon</h3>
      <div class="flex gap-2">
        <input
          type="text"
          placeholder="Enter coupon code"
          [ngModel]="code()"
          class="flex-1 border border-gray-200 rounded px-3 py-2 text-sm outline-none focus:border-navy uppercase"
          (ngModelChange)="code.set($event.toUpperCase())"
          (keydown.enter)="apply()"
        />
        <button
          class="bg-navy text-white px-4 py-2 rounded text-sm font-semibold hover:bg-blue transition disabled:opacity-50"
          [disabled]="!code().trim()"
          (click)="apply()"
        >Apply</button>
      </div>

      @if (couponStatus === 'success' && couponMessage) {
        <p class="mt-2 text-xs font-medium text-success flex items-center gap-1">
          <svg class="w-3.5 h-3.5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/>
          </svg>
          {{ couponMessage }}
        </p>
      }
      @if (couponStatus === 'error' && couponMessage) {
        <p class="mt-2 text-xs font-medium text-red flex items-center gap-1">
          <svg class="w-3.5 h-3.5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
          </svg>
          {{ couponMessage }}
        </p>
      }
    </div>
  `,
})
export class CouponInputComponent {
  @Input() couponStatus: 'idle' | 'success' | 'error' = 'idle';
  @Input() couponMessage: string | null = null;
  @Output() applyCoupon = new EventEmitter<string>();

  readonly code = signal('');

  apply(): void {
    const trimmed = this.code().trim();
    if (trimmed) {
      this.applyCoupon.emit(trimmed);
    }
  }
}
