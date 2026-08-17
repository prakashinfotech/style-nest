import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeroCarouselComponent } from '../../home/hero-carousel.component';
import { CategoryBannersComponent } from '../../home/category-banners.component';
import { FlashSaleComponent } from '../../home/flash-sale.component';
import { PromoBannersComponent } from '../../home/promo-banners.component';
import { BrandLogoStripComponent } from '../../home/brand-logo-strip.component';
import { FeaturedProductsComponent } from '../../home/featured-products.component';

@Component({
  selector: 'app-home',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    HeroCarouselComponent,
    CategoryBannersComponent,
    FeaturedProductsComponent,
    PromoBannersComponent,
    BrandLogoStripComponent,
    FlashSaleComponent,
  ],
  template: `
    <!-- DESIGN.md §5.1 Homepage section order -->
    <main id="main-content" tabindex="-1" class="page-fade-in">

      <!-- 3. Hero Carousel — full-width, progress bar, pause-on-hover -->
      <app-hero-carousel />

      <!-- 4. Category Shortcut Strip -->
      <app-category-banners />

      <!-- 5. Flash Sale — urgency/FOMO placed early to drive conversions -->
      <app-flash-sale />

      <!-- 6. 2-up & 3-up Promo Banners -->
      <app-promo-banners />

      <!-- 7. "New Arrivals" — compact 5-col single row from live Catalog.API -->
      <app-featured-products
        title="New Arrivals"
        eyebrow="Just In"
        viewAllLink="/products"
        [viewAllParams]="{ sort: 'newest' }"
        [pageSize]="5"
        sort="newest"
        [compact]="true"
      />

      <!-- 8. Top Brands — brand trust signal -->
      <app-brand-logo-strip />

      <!-- 9. "Trending Now" — top-rated products, 5-col compact row matching New Arrivals -->
      <app-featured-products
        title="Trending Now"
        eyebrow="Most Popular"
        viewAllLink="/products"
        [viewAllParams]="{ sort: 'rating' }"
        [pageSize]="5"
        sort="rating"
        [compact]="true"
      />

      <!-- Explicit spacing between Flash Sale and footer -->
      <div class="h-0" aria-hidden="true"></div>

    </main>
  `,
})
export class HomeComponent {}
