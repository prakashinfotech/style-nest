import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

interface Brand {
  name: string;
  slug: string;
  category: string;
  imageUrl: string;
  gradient: string;
}

@Component({
  selector: 'app-brand-logo-strip',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink],
  template: `
    <!-- Top Brands — luxury dark section with image tiles + marquee ticker -->
    <section class="py-10 md:py-16 bg-navy overflow-hidden" aria-label="Top brands">

      <!-- ── Section Header ── -->
      <div class="max-w-layout mx-auto px-4 md:px-6 mb-8 md:mb-10">
        <div class="flex items-end justify-between">
          <div>
            <p class="text-[10px] tracking-[0.2em] uppercase text-gold font-semibold mb-1.5">Discover</p>
            <h2 class="font-display text-2xl md:text-3xl font-semibold text-white leading-tight">
              Top Brands
            </h2>
            <div class="mt-2 w-12 h-0.5 bg-gold rounded-full"></div>
          </div>
          <a
            routerLink="/products"
            [queryParams]="{ category: 'brands' }"
            class="text-sm font-medium text-gold hover:text-white border border-gold/40 hover:border-white/50 px-4 py-2 rounded-full transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-gold"
            aria-label="View all brands"
          >
            View All
          </a>
        </div>
      </div>

      <!-- ── Brand Image Grid ── -->
      <div class="max-w-layout mx-auto px-4 md:px-6 mb-10">
        <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-3 md:gap-4">
          @for (brand of brands; track brand.slug) {
            <a
              [routerLink]="['/products']"
              [queryParams]="{ brand: brand.slug }"
              class="relative block rounded-xl overflow-hidden group"
              style="aspect-ratio: 1"
              [attr.aria-label]="'Shop ' + brand.name + ' — ' + brand.category"
            >
              <!-- Brand gradient (always visible, image overlays this) -->
              <div
                class="absolute inset-0 transition-transform duration-500 ease-out group-hover:scale-110 will-change-transform"
                [style.background]="brand.gradient"
              ></div>

              <!-- Lifestyle fashion image -->
              <img
                [src]="brand.imageUrl"
                [alt]="brand.name"
                class="absolute inset-0 w-full h-full object-cover opacity-55 group-hover:opacity-35 transition-all duration-500 ease-out group-hover:scale-110 will-change-transform"
                loading="lazy"
                (error)="onImageError($event)"
              />

              <!-- Bottom gradient scrim for text legibility -->
              <div
                class="absolute inset-0 bg-gradient-to-t from-black/90 via-black/30 to-transparent pointer-events-none"
              ></div>

              <!-- Gold ring on hover -->
              <div
                class="absolute inset-0 rounded-xl ring-inset ring-0 group-hover:ring-1 ring-gold/50 transition-all duration-300 pointer-events-none"
              ></div>

              <!-- Brand info panel -->
              <div class="absolute bottom-0 left-0 right-0 p-2.5 md:p-3">
                <p class="text-white font-semibold text-xs md:text-sm leading-tight truncate">
                  {{ brand.name }}
                </p>
                <p class="text-white/45 text-[9px] md:text-[10px] mt-0.5 tracking-wider uppercase">
                  {{ brand.category }}
                </p>
                <!-- Hover CTA — hidden by default, slides in on hover -->
                <div
                  class="flex items-center gap-1 mt-1 opacity-0 translate-y-1 group-hover:opacity-100 group-hover:translate-y-0 transition-all duration-300 ease-out hidden md:flex"
                  aria-hidden="true"
                >
                  <span class="text-gold text-[10px] font-medium whitespace-nowrap">Shop Collection</span>
                  <svg class="w-2.5 h-2.5 text-gold flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5" aria-hidden="true">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3"/>
                  </svg>
                </div>
              </div>
            </a>
          }
        </div>
      </div>

      <!-- ── Divider ── -->
      <div class="border-t border-white/10 mb-6"></div>

      <!-- ── Infinite Scrolling Brand Marquee ── -->
      <div class="overflow-hidden" aria-hidden="true" role="presentation">
        <div class="brand-marquee flex items-center w-max">
          <!-- First set -->
          @for (brand of brands; track brand.slug) {
            <span class="flex items-center flex-shrink-0 px-5">
              <span class="text-white/55 text-[10px] font-semibold tracking-[0.18em] uppercase whitespace-nowrap hover:text-gold transition-colors duration-200 cursor-default select-none">
                {{ brand.name }}
              </span>
              <span class="ml-5 text-gold/30 text-sm" aria-hidden="true">•</span>
            </span>
          }
          <!-- Duplicate set for seamless loop -->
          @for (brand of brands; track brand.slug + '_loop') {
            <span class="flex items-center flex-shrink-0 px-5">
              <span class="text-white/55 text-[10px] font-semibold tracking-[0.18em] uppercase whitespace-nowrap hover:text-gold transition-colors duration-200 cursor-default select-none">
                {{ brand.name }}
              </span>
              <span class="ml-5 text-gold/30 text-sm" aria-hidden="true">•</span>
            </span>
          }
        </div>
      </div>

    </section>
  `,
  styles: [`
    :host { display: block; }

    @keyframes brand-ticker {
      from { transform: translateX(0); }
      to   { transform: translateX(-50%); }
    }

    .brand-marquee {
      animation: brand-ticker 32s linear infinite;
    }

    .brand-marquee:hover {
      animation-play-state: paused;
    }

    @media (prefers-reduced-motion: reduce) {
      .brand-marquee { animation: none; }
    }
  `],
})
export class BrandLogoStripComponent {
  onImageError(event: Event): void {
    (event.target as HTMLImageElement).style.display = 'none';
  }

  readonly brands: Brand[] = [
    {
      name: 'Nike',
      slug: 'nike',
      category: 'Sportswear',
      imageUrl: 'https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=400&h=400&fit=crop&q=80',
      gradient: 'linear-gradient(135deg, #111111 0%, #2d2d2d 100%)',
    },
    {
      name: 'Adidas',
      slug: 'adidas',
      category: 'Athletic',
      imageUrl: 'https://images.unsplash.com/photo-1571945105520-1c1f45a29edf?w=400&h=400&fit=crop&q=80',
      gradient: 'linear-gradient(135deg, #000000 0%, #1e1e1e 100%)',
    },
    {
      name: 'Zara',
      slug: 'zara',
      category: 'Fashion',
      imageUrl: 'https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?w=400&h=400&fit=crop&q=80',
      gradient: 'linear-gradient(135deg, #1a1a1a 0%, #3a3a3a 100%)',
    },
    {
      name: 'Calvin Klein',
      slug: 'calvinklein',
      category: 'Lifestyle',
      imageUrl: 'https://images.unsplash.com/photo-1529139574466-a303027c1d8b?w=400&h=400&fit=crop&q=80',
      gradient: 'linear-gradient(135deg, #1c1c1c 0%, #3e3e3e 100%)',
    },
    {
      name: 'Ralph Lauren',
      slug: 'ralphlauren',
      category: 'Luxury Casual',
      imageUrl: 'https://images.unsplash.com/photo-1483985988355-763728e1935b?w=400&h=400&fit=crop&q=80',
      gradient: 'linear-gradient(135deg, #1B3A6F 0%, #0C2340 100%)',
    },
    {
      name: 'Versace',
      slug: 'versace',
      category: 'Haute Couture',
      imageUrl: 'https://images.unsplash.com/photo-1524504388940-b1c1722653e4?w=400&h=400&fit=crop&q=80',
      gradient: 'linear-gradient(135deg, #7B6014 0%, #C9A84C 100%)',
    },
    {
      name: 'Puma',
      slug: 'puma',
      category: 'Sportswear',
      imageUrl: 'https://images.unsplash.com/photo-1606107557195-0e29a4b5b4aa?w=400&h=400&fit=crop&q=80',
      gradient: 'linear-gradient(135deg, #C8102E 0%, #7A0A1A 100%)',
    },
    {
      name: "Levi's",
      slug: 'levis',
      category: 'Denim',
      imageUrl: 'https://images.unsplash.com/photo-1542219550-37153d387c27?w=400&h=400&fit=crop&q=80',
      gradient: 'linear-gradient(135deg, #1B3A6F 0%, #0D1F3C 100%)',
    },
    {
      name: 'H&M',
      slug: 'hm',
      category: 'Fast Fashion',
      imageUrl: 'https://images.unsplash.com/photo-1558769132-cb1aea458c5e?w=400&h=400&fit=crop&q=80',
      gradient: 'linear-gradient(135deg, #CC0000 0%, #7A0000 100%)',
    },
    {
      name: 'Tommy Hilfiger',
      slug: 'tommy',
      category: 'American Classic',
      imageUrl: 'https://images.unsplash.com/photo-1539109136881-3be0616acf4b?w=400&h=400&fit=crop&q=80',
      gradient: 'linear-gradient(135deg, #C8102E 0%, #002868 100%)',
    },
    {
      name: 'Fossil',
      slug: 'fossil',
      category: 'Accessories',
      imageUrl: 'https://images.unsplash.com/photo-1547949003-9792a18a2841?w=400&h=400&fit=crop&q=80',
      gradient: 'linear-gradient(135deg, #B08D57 0%, #6B5133 100%)',
    },
    {
      name: 'Mango',
      slug: 'mango',
      category: 'Contemporary',
      imageUrl: 'https://images.unsplash.com/photo-1469334031218-e382a71b716b?w=400&h=400&fit=crop&q=80',
      gradient: 'linear-gradient(135deg, #D2691E 0%, #8B3A0A 100%)',
    },
  ];
}
