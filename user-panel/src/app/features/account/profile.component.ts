import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

interface AccountLink {
  label: string;
  description: string;
  route: string;
  icon: string;
}

const ACCOUNT_LINKS: AccountLink[] = [
  { label: 'My Orders', description: 'Track and manage your orders', route: '/orders', icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2' },
  { label: 'My Wallet', description: 'StyleNest Cash balance and transactions', route: '/account/wallet', icon: 'M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z' },
  { label: 'Saved Addresses', description: 'Manage your delivery addresses', route: '/account', icon: 'M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z' },
  { label: 'Wishlist', description: 'Products you\'ve saved for later', route: '/account', icon: 'M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z' },
];

@Component({
  selector: 'app-profile',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <div class="max-w-2xl mx-auto px-4 py-8">
      <h1 class="font-display text-2xl font-bold text-dark mb-2">My Account</h1>
      <p class="text-muted text-sm mb-8">Manage your orders, wallet, and preferences</p>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        @for (link of links; track link.label) {
          <a [routerLink]="link.route"
             class="bg-card rounded-2xl border border-border p-5 flex items-start gap-4 hover:border-red/40 hover:shadow-sm transition-all group">
            <div class="w-10 h-10 bg-red/10 rounded-xl flex items-center justify-center shrink-0 group-hover:bg-red/20 transition-colors">
              <svg class="w-5 h-5 text-red" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" [attr.d]="link.icon"/>
              </svg>
            </div>
            <div>
              <p class="text-sm font-semibold text-dark">{{ link.label }}</p>
              <p class="text-xs text-muted mt-0.5">{{ link.description }}</p>
            </div>
          </a>
        }
      </div>
    </div>
  `,
})
export class ProfileComponent {
  readonly links = ACCOUNT_LINKS;
}
