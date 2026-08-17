import { ChangeDetectionStrategy, Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';

export interface DeliveryAddress {
  fullName: string;
  phone: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  state: string;
  pincode: string;
}

@Component({
  selector: 'app-address-step',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <section>
      <h2 class="text-base font-bold text-dark mb-4">Delivery Address</h2>

      <form [formGroup]="form" (ngSubmit)="submit()" class="space-y-3">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label class="block text-xs text-muted mb-1">Full Name *</label>
            <input formControlName="fullName" type="text" class="input-field" placeholder="Jane Doe" />
            @if (form.get('fullName')?.invalid && form.get('fullName')?.touched) {
              <p class="text-red text-xs mt-1">Full name is required</p>
            }
          </div>
          <div>
            <label class="block text-xs text-muted mb-1">Phone *</label>
            <input formControlName="phone" type="tel" class="input-field" placeholder="10-digit number" />
            @if (form.get('phone')?.invalid && form.get('phone')?.touched) {
              <p class="text-red text-xs mt-1">Valid 10-digit number required</p>
            }
          </div>
        </div>

        <div>
          <label class="block text-xs text-muted mb-1">Address Line 1 *</label>
          <input formControlName="addressLine1" type="text" class="input-field" placeholder="House no., Street, Area" />
          @if (form.get('addressLine1')?.invalid && form.get('addressLine1')?.touched) {
            <p class="text-red text-xs mt-1">Address line 1 is required</p>
          }
        </div>
        <div>
          <label class="block text-xs text-muted mb-1">Address Line 2</label>
          <input formControlName="addressLine2" type="text" class="input-field" placeholder="Landmark (optional)" />
        </div>

        <div class="grid grid-cols-2 sm:grid-cols-3 gap-3">
          <div>
            <label class="block text-xs text-muted mb-1">Pincode *</label>
            <input formControlName="pincode" type="text" class="input-field" placeholder="6 digits" maxlength="6" />
            @if (form.get('pincode')?.invalid && form.get('pincode')?.touched) {
              <p class="text-red text-xs mt-1">Valid 6-digit pincode required</p>
            }
          </div>
          <div>
            <label class="block text-xs text-muted mb-1">City *</label>
            <input formControlName="city" type="text" class="input-field" placeholder="City" />
            @if (form.get('city')?.invalid && form.get('city')?.touched) {
              <p class="text-red text-xs mt-1">City is required</p>
            }
          </div>
          <div>
            <label class="block text-xs text-muted mb-1">State *</label>
            <input formControlName="state" type="text" class="input-field" placeholder="State" />
            @if (form.get('state')?.invalid && form.get('state')?.touched) {
              <p class="text-red text-xs mt-1">State is required</p>
            }
          </div>
        </div>

        <button
          type="submit"
          class="w-full bg-navy text-white font-semibold py-3 rounded-lg hover:bg-blue transition mt-2 disabled:opacity-50"
          [disabled]="form.invalid"
        >
          CONTINUE TO PAYMENT
        </button>
      </form>
    </section>
  `,
  styles: [`
    .input-field {
      @apply w-full border border-gray-200 rounded px-3 py-2 text-sm outline-none;
    }
    .input-field:focus { @apply border-navy; }
  `],
})
export class AddressStepComponent {
  @Output() addressSubmit = new EventEmitter<DeliveryAddress>();

  private readonly fb = new FormBuilder();

  readonly form = this.fb.nonNullable.group({
    fullName:     ['', Validators.required],
    phone:        ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
    addressLine1: ['', Validators.required],
    addressLine2: [''],
    city:         ['', Validators.required],
    state:        ['', Validators.required],
    pincode:      ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
  });

  submit(): void {
    if (this.form.valid) {
      this.addressSubmit.emit(this.form.getRawValue());
    } else {
      this.form.markAllAsTouched();
    }
  }
}
