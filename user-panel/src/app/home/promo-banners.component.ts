import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

interface PromoBanner {
  title:      string;
  subtitle:   string;
  ctaLabel:   string;
  ctaLink:    string;
  imageUrl:   string;
}

@Component({
  selector: 'app-promo-banners',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink],
  template: `
    <!-- DESIGN.md §4.9 Promotional Banners (2-up / 3-up) — image zoom on hover -->
    <section class="py-8 md:py-12 px-4 max-w-layout mx-auto" aria-label="Promotions">

      <!-- 2-up row — 220px desktop -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-4">
        @for (banner of twoBanners; track banner.title) {
          <a
            [routerLink]="banner.ctaLink.split('?')[0]"
            [queryParams]="parseQuery(banner.ctaLink)"
            class="relative overflow-hidden rounded-lg group block focus-visible:ring-2 focus-visible:ring-red focus-visible:ring-offset-2"
            style="height: 220px"
            [attr.aria-label]="banner.title + ' — ' + banner.ctaLabel"
            role="figure"
          >
            <!-- Image — zooms on group hover -->
            <img
              [src]="banner.imageUrl"
              [alt]=""
              class="absolute inset-0 w-full h-full object-cover transition-transform duration-500 group-hover:scale-[1.04]"
              loading="lazy"
              aria-hidden="true"
            />
            <!-- Richer gradient: transparent top → heavy black bottom -->
            <div
              class="absolute inset-0 bg-gradient-to-t from-black/80 via-black/30 to-transparent
                     group-hover:from-black/85 transition-all duration-300"
              aria-hidden="true"
            ></div>
            <!-- Text block -->
            <div class="absolute inset-0 flex flex-col justify-end p-5">
              @if (banner.subtitle) {
                <p class="text-white/75 text-[11px] uppercase tracking-widest font-medium mb-1">
                  {{ banner.subtitle }}
                </p>
              }
              <h3 class="font-display text-white text-xl md:text-2xl font-semibold leading-tight mb-2.5">
                {{ banner.title }}
              </h3>
              <span
                class="inline-flex items-center gap-1 text-white text-sm font-semibold
                       border-b border-white/40 group-hover:border-white pb-0.5 transition-colors w-fit"
              >
                {{ banner.ctaLabel }}
                <svg class="w-3.5 h-3.5 transition-transform group-hover:translate-x-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M9 5l7 7-7 7"/>
                </svg>
              </span>
            </div>
          </a>
        }
      </div>

      <!-- 3-up row — 180px desktop -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        @for (banner of threeBanners; track banner.title) {
          <a
            [routerLink]="banner.ctaLink.split('?')[0]"
            [queryParams]="parseQuery(banner.ctaLink)"
            class="relative overflow-hidden rounded-lg group block focus-visible:ring-2 focus-visible:ring-red focus-visible:ring-offset-2"
            style="height: 180px"
            [attr.aria-label]="banner.title + ' — ' + banner.ctaLabel"
            role="figure"
          >
            <img
              [src]="banner.imageUrl"
              [alt]=""
              class="absolute inset-0 w-full h-full object-cover transition-transform duration-500 group-hover:scale-[1.04]"
              loading="lazy"
              aria-hidden="true"
            />
            <div
              class="absolute inset-0 bg-gradient-to-t from-black/75 via-black/25 to-transparent
                     group-hover:from-black/80 transition-all duration-300"
              aria-hidden="true"
            ></div>
            <div class="absolute inset-0 flex flex-col justify-end p-4">
              <h3 class="font-display text-white text-base md:text-lg font-semibold leading-tight mb-1.5">
                {{ banner.title }}
              </h3>
              <span
                class="inline-flex items-center gap-1 text-white/90 text-xs font-semibold
                       border-b border-white/30 group-hover:border-white/70 pb-0.5 transition-colors w-fit"
              >
                {{ banner.ctaLabel }}
                <svg class="w-3 h-3 transition-transform group-hover:translate-x-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M9 5l7 7-7 7"/>
                </svg>
              </span>
            </div>
          </a>
        }
      </div>
    </section>
  `,
})
export class PromoBannersComponent {
  readonly twoBanners: PromoBanner[] = [
    {
      title:      "Women's New Season",
      subtitle:   'Exclusive Collection',
      ctaLabel:   "Shop Women's",
      ctaLink:    '/products?category=women',
      imageUrl:   'https://images.unsplash.com/photo-1558618666-fcd25c85cd64?q=80&w=800&auto=format&fit=crop',
    },
    {
      title:      "Men's Edit",
      subtitle:   'Style for Every Occasion',
      ctaLabel:   "Shop Men's",
      ctaLink:    '/products?category=men',
      imageUrl:   'https://images.unsplash.com/photo-1617137968427-85924c800a22?q=80&w=800&auto=format&fit=crop',
    },
  ];

  readonly threeBanners: PromoBanner[] = [
    {
      title:      'Earn NeuCoins',
      subtitle:   '',
      ctaLabel:   'Know More',
      ctaLink:    '/products',
      imageUrl:   'https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?q=80&w=800&auto=format&fit=crop',
    },
    {
      title:      'Try & Buy',
      subtitle:   '',
      ctaLabel:   'How it Works',
      ctaLink:    '/products',
      imageUrl:   'https://images.unsplash.com/photo-1445205170230-053b83016050?q=80&w=800&auto=format&fit=crop',
    },
    {
      title:      'Easy Returns',
      subtitle:   '',
      ctaLabel:   'Return Policy',
      ctaLink:    '/products',
      imageUrl:   'https://images.unsplash.com/photo-1591085686350-798c0f9faa7f?q=80&w=800&auto=format&fit=crop',
    },
  ];

  parseQuery(url: string): Record<string, string> {
    const i = url.indexOf('?');
    if (i === -1) return {};
    return Object.fromEntries(new URLSearchParams(url.slice(i + 1)).entries());
  }
}
