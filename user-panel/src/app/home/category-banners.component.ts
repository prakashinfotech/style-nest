import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

interface CategoryItem {
  label:    string;
  slug:     string;
  imageUrl: string;
  accent:   string;
}

@Component({
  selector: 'app-category-banners',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink],
  template: `
    <!-- DESIGN.md §4.5 Category Shortcut Strip — full-width grid on desktop, scroll on mobile -->
    <section class="py-6 md:py-10" aria-label="Shop by category">

      <!-- Section header — constrained to layout width -->
      <div class="max-w-layout mx-auto px-4 md:px-6">
        <div class="flex items-center justify-between mb-5 md:mb-7">
          <div>
            <p class="text-[11px] tracking-widest uppercase text-red font-medium mb-0.5">Explore</p>
            <h2 class="font-display text-xl md:text-2xl font-semibold text-dark">Shop By Category</h2>
            <div class="mt-1.5 w-10 h-0.5 bg-red"></div>
          </div>
          <a
            routerLink="/products"
            class="text-sm font-medium text-red hover:underline"
            aria-label="View all categories"
          >View All</a>
        </div>
      </div>

      <!--
        Desktop (md+): CSS grid — cards stretch to fill the full container width evenly.
        Mobile (<md):  horizontal scroll strip — cards keep a comfortable fixed width.

        We break out of the max-w-layout constraint on desktop so the grid spans
        edge-to-edge (matching the hero carousel), with px-4/px-6 as the only margin.
      -->

      <!-- ── Mobile: horizontal scroll strip ─────────────────── -->
      <div
        class="md:hidden flex gap-3 overflow-x-auto px-4 pb-2 scrollbar-hide"
        style="scrollbar-width: none; -ms-overflow-style: none;"
        role="list"
        aria-label="Product categories"
      >
        @for (cat of categories; track cat.slug) {
          <a
            [routerLink]="['/products']"
            [queryParams]="{ category: cat.slug }"
            class="flex-shrink-0 flex flex-col items-center gap-2 group"
            role="listitem"
            [attr.aria-label]="'Shop ' + cat.label"
          >
            <div
              class="relative w-[88px] h-[110px] rounded-2xl overflow-hidden shadow-sm
                     ring-2 ring-transparent group-hover:ring-red/50
                     group-hover:shadow-lg transition-all duration-300"
            >
              <img
                [src]="cat.imageUrl"
                [alt]="cat.label"
                class="w-full h-full object-cover transition-transform duration-500 group-hover:scale-[1.08]"
                loading="lazy"
              />
              <div
                class="absolute inset-0 bg-gradient-to-t from-black/70 via-black/20 to-transparent"
                aria-hidden="true"
              ></div>
              <div class="absolute bottom-0 inset-x-0 pb-2 flex items-center justify-center">
                <span class="text-white text-[11px] font-semibold tracking-wide drop-shadow-sm text-center leading-tight px-1">
                  {{ cat.label }}
                </span>
              </div>
            </div>
            <span
              class="w-1.5 h-1.5 rounded-full transition-all duration-300 group-hover:scale-125"
              [style.background]="cat.accent"
              aria-hidden="true"
            ></span>
          </a>
        }
      </div>

      <!-- ── Desktop: full-width grid ─────────────────────────── -->
      <!--
        Constrained to max-w-layout with px-4 md:px-6 — consistent with all other
        home sections (featured-products, promo-banners, brand-logo-strip, etc.).
        grid-cols-5 on md–lg, grid-cols-10 on lg+ so all 10 cards fill the row.
      -->
      <div class="max-w-layout mx-auto px-4 md:px-6">
      <div
        class="hidden md:grid md:grid-cols-5 lg:grid-cols-10 gap-3 lg:gap-4"
        role="list"
        aria-label="Product categories"
      >
        @for (cat of categories; track cat.slug) {
          <a
            [routerLink]="['/products']"
            [queryParams]="{ category: cat.slug }"
            class="group flex flex-col items-center gap-2.5"
            role="listitem"
            [attr.aria-label]="'Shop ' + cat.label"
          >
            <!--
              Aspect-ratio card — 3:4 portrait ratio scales with the grid column width.
              No fixed px dimensions: the card fills its column and maintains the ratio.
            -->
            <div
              class="relative w-full aspect-[3/4] rounded-2xl overflow-hidden shadow-sm
                     ring-2 ring-transparent group-hover:ring-red/50
                     group-hover:shadow-lg transition-all duration-300"
            >
              <img
                [src]="cat.imageUrl"
                [alt]="cat.label"
                class="absolute inset-0 w-full h-full object-cover
                       transition-transform duration-500 group-hover:scale-[1.06]"
                loading="lazy"
              />
              <!-- Gradient overlay -->
              <div
                class="absolute inset-0 bg-gradient-to-t from-black/65 via-black/15 to-transparent"
                aria-hidden="true"
              ></div>
              <!-- Label -->
              <div class="absolute bottom-0 inset-x-0 pb-3 flex items-center justify-center">
                <span
                  class="text-white text-[12px] lg:text-[13px] font-semibold tracking-wide
                         drop-shadow-sm text-center leading-tight px-1"
                >
                  {{ cat.label }}
                </span>
              </div>
              <!-- Hover shine sweep -->
              <div
                class="absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity duration-300
                       bg-gradient-to-tr from-transparent via-white/8 to-transparent"
                aria-hidden="true"
              ></div>
            </div>

            <!-- Accent dot -->
            <span
              class="w-1.5 h-1.5 rounded-full transition-all duration-300 group-hover:scale-150"
              [style.background]="cat.accent"
              aria-hidden="true"
            ></span>
          </a>
        }
      </div>
      </div>

    </section>
  `,
  styles: [`
    :host { display: block; }
    div::-webkit-scrollbar { display: none; }
  `],
})
export class CategoryBannersComponent {
  readonly categories: CategoryItem[] = [
    {
      label:    'Women',
      slug:     'women',
      imageUrl: 'https://images.unsplash.com/photo-1581044777550-4cfa60707c03?w=220&h=280&fit=crop&auto=format',
      accent:   '#E91E8C',
    },
    {
      label:    'Men',
      slug:     'men',
      imageUrl: 'https://images.unsplash.com/photo-1617137968427-85924c800a22?w=220&h=280&fit=crop&auto=format',
      accent:   '#1C2B4A',
    },
    {
      label:    'Kids',
      slug:     'kids',
      imageUrl: 'https://images.unsplash.com/photo-1519457431-44ccd64a579b?w=220&h=280&fit=crop&auto=format',
      accent:   '#F9A825',
    },
    {
      label:    'Footwear',
      slug:     'footwear',
      imageUrl: 'https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=220&h=280&fit=crop&auto=format',
      accent:   '#0071C2',
    },
    {
      label:    'Jewellery',
      slug:     'jewellery',
      imageUrl: 'https://images.unsplash.com/photo-1601821765780-754fa98637c1?w=220&h=280&fit=crop&auto=format',
      accent:   '#C9A84C',
    },
    {
      label:    'Beauty',
      slug:     'beauty',
      imageUrl: 'https://images.unsplash.com/photo-1522335789203-aabd1fc54bc9?w=220&h=280&fit=crop&auto=format',
      accent:   '#E91E8C',
    },
    {
      label:    'Bags',
      slug:     'bags',
      imageUrl: 'https://images.unsplash.com/photo-1548036328-c9fa89d128fa?w=220&h=280&fit=crop&auto=format',
      accent:   '#795548',
    },
    {
      label:    'Luxury',
      slug:     'luxury',
      imageUrl: 'https://images.unsplash.com/photo-1490481651871-ab68de25d43d?w=220&h=280&fit=crop&auto=format',
      accent:   '#C9A84C',
    },
    {
      label:    'Ethnic Wear',
      slug:     'ethnic-wear',
      imageUrl: 'https://images.unsplash.com/photo-1583391733956-3750e0ff4e8b?w=220&h=280&fit=crop&auto=format',
      accent:   '#E31837',
    },
    {
      label:    'Sale',
      slug:     'sale',
      imageUrl: 'https://images.unsplash.com/photo-1607082348824-0a96f2a4b9da?w=220&h=280&fit=crop&auto=format',
      accent:   '#E31837',
    },
  ];
}
