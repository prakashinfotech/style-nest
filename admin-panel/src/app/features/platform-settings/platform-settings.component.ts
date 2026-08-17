import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';

interface SettingsGroup {
  label: string;
  icon: string;
  settings: SettingField[];
}

interface SettingField {
  key: string;
  label: string;
  description: string;
  type: 'text' | 'number' | 'toggle' | 'select';
  value: string | boolean | number;
  options?: string[];
}

@Component({
  selector: 'app-platform-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <div class="space-y-6">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-xl font-bold text-dark">Platform Settings</h1>
          <p class="text-sm text-muted mt-0.5">Configure global platform behaviour and policies.</p>
        </div>
        <button
          type="button"
          class="px-4 py-2 bg-navy text-white text-sm rounded-lg hover:bg-navy/90 transition-colors disabled:opacity-50"
          [disabled]="saving()"
          (click)="save()"
        >
          {{ saving() ? 'Saving…' : 'Save Changes' }}
        </button>
      </div>

      @if (saved()) {
        <div class="flex items-center gap-2 text-sm text-success bg-success/10 border border-success/20 px-4 py-3 rounded-lg">
          <span>✓</span> Settings saved successfully.
        </div>
      }

      @for (group of groups; track group.label) {
        <div class="bg-white rounded-xl shadow-sm border border-border overflow-hidden">
          <div class="px-5 py-4 border-b border-border flex items-center gap-2">
            <span class="text-lg" aria-hidden="true">{{ group.icon }}</span>
            <h2 class="font-semibold text-dark text-sm">{{ group.label }}</h2>
          </div>
          <div class="divide-y divide-border">
            @for (field of group.settings; track field.key) {
              <div class="px-5 py-4 flex items-center justify-between gap-4">
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-dark">{{ field.label }}</p>
                  <p class="text-xs text-muted mt-0.5">{{ field.description }}</p>
                </div>
                <div class="flex-shrink-0 w-56">
                  @if (field.type === 'toggle') {
                    <button
                      type="button"
                      role="switch"
                      [attr.aria-checked]="field.value === true"
                      class="relative inline-flex h-6 w-11 rounded-full transition-colors focus-visible:ring-2 focus-visible:ring-navy"
                      [class]="field.value ? 'bg-navy' : 'bg-border'"
                      (click)="field.value = !field.value"
                    >
                      <span
                        class="inline-block h-4 w-4 rounded-full bg-white shadow transform transition-transform mt-1"
                        [class]="field.value ? 'translate-x-6' : 'translate-x-1'"
                      ></span>
                    </button>
                  } @else if (field.type === 'select') {
                    <select
                      class="w-full border border-border rounded-lg px-3 py-1.5 text-sm text-dark focus:outline-none focus:ring-2 focus:ring-navy/50"
                      [(ngModel)]="field.value"
                    >
                      @for (opt of field.options; track opt) {
                        <option [value]="opt">{{ opt }}</option>
                      }
                    </select>
                  } @else if (field.type === 'number') {
                    <input
                      type="number"
                      class="w-full border border-border rounded-lg px-3 py-1.5 text-sm text-dark focus:outline-none focus:ring-2 focus:ring-navy/50"
                      [(ngModel)]="field.value"
                    />
                  } @else {
                    <input
                      type="text"
                      class="w-full border border-border rounded-lg px-3 py-1.5 text-sm text-dark focus:outline-none focus:ring-2 focus:ring-navy/50"
                      [(ngModel)]="field.value"
                    />
                  }
                </div>
              </div>
            }
          </div>
        </div>
      }
    </div>
  `,
})
export class PlatformSettingsComponent {
  saving = signal(false);
  saved = signal(false);

  groups: SettingsGroup[] = [
    {
      label: 'General',
      icon: '⚙️',
      settings: [
        { key: 'site_name',    label: 'Site Name',       description: 'Displayed in browser title and emails.',    type: 'text',   value: 'StyleNest Fashion' },
        { key: 'site_tagline', label: 'Tagline',          description: 'Short brand slogan shown in footer.',       type: 'text',   value: 'India\'s Fashion Destination' },
        { key: 'maintenance',  label: 'Maintenance Mode', description: 'Block all public traffic to the storefront.',type: 'toggle', value: false },
      ],
    },
    {
      label: 'Orders & Commerce',
      icon: '🛒',
      settings: [
        { key: 'min_order',         label: 'Minimum Order Value (₹)', description: 'Orders below this value are rejected.',             type: 'number',  value: 299 },
        { key: 'free_shipping_min', label: 'Free Shipping Threshold (₹)',description: 'Orders above this get free standard shipping.',  type: 'number',  value: 999 },
        { key: 'cod_enabled',       label: 'Cash on Delivery',         description: 'Allow COD payment method at checkout.',           type: 'toggle',  value: true },
        { key: 'guest_checkout',    label: 'Guest Checkout',           description: 'Allow purchases without user account.',           type: 'toggle',  value: false },
      ],
    },
    {
      label: 'Seller Policy',
      icon: '🏪',
      settings: [
        { key: 'seller_commission',   label: 'Default Commission (%)',  description: 'Platform commission on each sale.',              type: 'number',  value: 12 },
        { key: 'seller_approval',     label: 'Auto-Approve Sellers',    description: 'Skip manual review for new seller registrations.',type: 'toggle',  value: false },
        { key: 'payout_cycle',        label: 'Payout Cycle',            description: 'How often seller payouts are processed.',        type: 'select',  value: 'Weekly', options: ['Daily', 'Weekly', 'Bi-weekly', 'Monthly'] },
      ],
    },
    {
      label: 'Reviews & Content',
      icon: '⭐',
      settings: [
        { key: 'review_approval',  label: 'Moderate Reviews',     description: 'Require admin approval before reviews go live.',  type: 'toggle', value: true },
        { key: 'review_verified',  label: 'Verified Purchase Only',description: 'Only allow reviews from verified purchasers.',    type: 'toggle', value: true },
      ],
    },
    {
      label: 'Security',
      icon: '🔐',
      settings: [
        { key: 'jwt_expiry',     label: 'Access Token Expiry (min)', description: 'How long access tokens are valid.',            type: 'number', value: 60 },
        { key: 'max_login_fail', label: 'Max Login Failures',        description: 'Account locked after N failed attempts.',      type: 'number', value: 5 },
        { key: 'otp_expiry',     label: 'OTP Expiry (min)',          description: 'Time window for OTP verification.',            type: 'number', value: 10 },
      ],
    },
  ];

  save(): void {
    this.saving.set(true);
    this.saved.set(false);
    setTimeout(() => {
      this.saving.set(false);
      this.saved.set(true);
      setTimeout(() => this.saved.set(false), 3000);
    }, 800);
  }
}
