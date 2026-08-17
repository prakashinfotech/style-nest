import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-footer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink],
  template: `
    <!-- DESIGN.md §4.13 Footer -->
    <div class="h-0.5 bg-red" aria-hidden="true"></div>
    <footer class="bg-navy text-white" role="contentinfo">
      <div class="max-w-layout mx-auto px-6 pt-12 pb-6">

        <!-- 4-column grid -->
        <div class="grid grid-cols-2 md:grid-cols-4 gap-8 mb-10">

          <!-- Column 1: Brand / About -->
          <div>
            <div class="mb-4">
              <span class="font-display text-xl font-bold">
                TATA <span class="text-red">StyleNest</span>
              </span>
              <p class="text-sm text-white/60 mt-2 leading-relaxed">
                Premium fashion &amp; lifestyle shopping. Authentic. Curated. Trusted.
              </p>
            </div>
            <!-- Social icons -->
            <div class="flex gap-3 mt-4" role="list" aria-label="Social media links">
              <a
                href="https://facebook.com"
                class="w-8 h-8 rounded-full border border-white/20 flex items-center justify-center hover:bg-white/10 transition"
                aria-label="Facebook"
                target="_blank"
                rel="noopener noreferrer"
                role="listitem"
              >
                <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path d="M18 2h-3a5 5 0 00-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 011-1h3z"/>
                </svg>
              </a>
              <a
                href="https://instagram.com"
                class="w-8 h-8 rounded-full border border-white/20 flex items-center justify-center hover:bg-white/10 transition"
                aria-label="Instagram"
                target="_blank"
                rel="noopener noreferrer"
                role="listitem"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <rect x="2" y="2" width="20" height="20" rx="5" ry="5" stroke-width="2"/>
                  <circle cx="12" cy="12" r="4" stroke-width="2"/>
                  <circle cx="17.5" cy="6.5" r="1" fill="currentColor"/>
                </svg>
              </a>
              <a
                href="https://twitter.com"
                class="w-8 h-8 rounded-full border border-white/20 flex items-center justify-center hover:bg-white/10 transition"
                aria-label="Twitter / X"
                target="_blank"
                rel="noopener noreferrer"
                role="listitem"
              >
                <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/>
                </svg>
              </a>
            </div>
          </div>

          <!-- Column 2: Shopping -->
          <div>
            <h3 class="text-sm font-semibold mb-4 text-gold uppercase tracking-widest">Shopping</h3>
            <nav aria-label="Shopping links">
              <ul class="space-y-2.5">
                @for (link of shoppingLinks; track link.label) {
                  <li>
                    <a [routerLink]="link.href" class="text-sm text-white/70 hover:text-white transition">{{ link.label }}</a>
                  </li>
                }
              </ul>
            </nav>
          </div>

          <!-- Column 3: Policies -->
          <div>
            <h3 class="text-sm font-semibold mb-4 text-gold uppercase tracking-widest">Policies</h3>
            <nav aria-label="Policy links">
              <ul class="space-y-2.5">
                @for (link of policyLinks; track link.label) {
                  <li>
                    <a [routerLink]="link.href" class="text-sm text-white/70 hover:text-white transition">{{ link.label }}</a>
                  </li>
                }
              </ul>
            </nav>
          </div>

          <!-- Column 4: Download App -->
          <div>
            <h3 class="text-sm font-semibold mb-4 text-gold uppercase tracking-widest">Download App</h3>
            <p class="text-sm text-white/60 mb-3 leading-relaxed">Shop on the go with the StyleNest app.</p>
            <div class="flex flex-col gap-2">
              <a
                href="#"
                class="flex items-center gap-2 bg-white/10 hover:bg-white/20 border border-white/20 rounded-md px-3 py-2 text-sm transition"
                aria-label="Download on the App Store"
              >
                <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path d="M18.71 19.5c-.83 1.24-1.71 2.45-3.05 2.47-1.34.03-1.77-.79-3.29-.79-1.53 0-2 .77-3.27.82-1.31.05-2.3-1.32-3.14-2.53C4.25 17 2.94 12.45 4.7 9.39c.87-1.52 2.43-2.48 4.12-2.51 1.28-.02 2.5.87 3.29.87.78 0 2.26-1.07 3.8-.91.65.03 2.47.26 3.64 1.98-.09.06-2.17 1.28-2.15 3.81.03 3.02 2.65 4.03 2.68 4.04-.03.07-.42 1.44-1.38 2.83M13 3.5c.73-.83 1.94-1.46 2.94-1.5.13 1.17-.34 2.35-1.04 3.19-.69.85-1.83 1.51-2.95 1.42-.15-1.15.41-2.35 1.05-3.11z"/>
                </svg>
                App Store
              </a>
              <a
                href="#"
                class="flex items-center gap-2 bg-white/10 hover:bg-white/20 border border-white/20 rounded-md px-3 py-2 text-sm transition"
                aria-label="Get it on Google Play"
              >
                <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path d="M3.18 23.76c.41.23.87.24 1.28.04l12.69-7.33-2.78-2.78L3.18 23.76zM20.1 10.04L17.18 8.3 14.1 11.38l3.07 3.07 3.05-1.76a1.78 1.78 0 000-2.65zM2.01 1.11A1.78 1.78 0 001.5 2.4v19.2c0 .5.19.96.51 1.29l.07.06L13.36 12 2.07 1.06l-.06.05zM4.46.2L16.83 7.42l-2.73 2.74L4.46.2z"/>
                </svg>
                Google Play
              </a>
            </div>
          </div>
        </div>

        <!-- Bottom bar -->
        <div class="border-t border-white/10 pt-6 flex flex-col md:flex-row items-center justify-between gap-4">
          <p class="text-sm" style="color: #78909C;">© 2026 StyleNest Fashion. All rights reserved.</p>

          <!-- Payment icons row -->
          <div class="flex items-center gap-3" aria-label="Accepted payment methods">
            @for (payment of paymentMethods; track payment) {
              <span
                class="text-[11px] font-semibold px-2 py-1 bg-white/10 rounded border border-white/20 text-white/80"
                [attr.aria-label]="payment"
              >{{ payment }}</span>
            }
          </div>
        </div>
      </div>
    </footer>
  `,
})
export class FooterComponent {
  readonly shoppingLinks = [
    { label: 'Track Order',    href: '/account/orders' },
    { label: 'Returns',        href: '/help/returns' },
    { label: 'Size Guide',     href: '/help/size-guide' },
    { label: 'FAQs',           href: '/help/faq' },
    { label: 'Contact Us',     href: '/help/contact' },
  ];

  readonly policyLinks = [
    { label: 'Privacy Policy',   href: '/help/privacy' },
    { label: 'Terms & Conditions', href: '/help/terms' },
    { label: 'Accessibility',    href: '/help/accessibility' },
    { label: 'Sitemap',          href: '/sitemap' },
    { label: 'Sell on StyleNest',     href: '/sell' },
  ];

  readonly paymentMethods = ['Visa', 'Mastercard', 'UPI', 'PayTM', 'NB'];
}
