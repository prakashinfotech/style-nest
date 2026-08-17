import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CurrencyInrPipe } from '../shared/pipes/currency-inr.pipe';
import { BadgeComponent } from '../shared/components/badge.component';
import { CatalogService } from '../core/services/catalog.service';
import { Product } from '../core/models/product.model';

@Component({
  selector: 'app-flash-sale',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, CurrencyInrPipe, BadgeComponent],
  template: `
    <!-- Dark charcoal gradient — deliberately different from the navy footer to prevent sections merging -->
    <section
      class="relative py-8 md:py-14 text-white"
      style="background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%)"
      aria-label="Flash sale"
    >
      <div class="max-w-layout mx-auto px-4">

        <!-- Header row -->
        <div class="flex items-center justify-between mb-4 md:mb-6">
          <div class="flex items-center gap-3">
            <h2 class="text-lg md:text-2xl font-bold">⚡ Flash Sale</h2>
            <div class="bg-red px-3 py-1 rounded text-sm font-mono font-bold tabular-nums">
              {{ timerDisplay() }}
            </div>
          </div>
          <a routerLink="/products" [queryParams]="{ sale: 'flash' }"
             class="text-sm text-[#F9A825] hover:underline font-medium">
            View All
          </a>
        </div>

        <!-- Product cards -->
        <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-3 md:gap-4">
          @for (deal of deals(); track deal.id) {
            <a
              [routerLink]="['/products', deal.id]"
              class="bg-white/10 hover:bg-white/20 rounded-lg overflow-hidden transition group"
            >
              <!-- Product image -->
              <div class="aspect-[3/4] bg-white relative overflow-hidden">
                @if (deal.imageUrls && deal.imageUrls.length > 0) {
                  <img
                    [src]="deal.imageUrls[0]"
                    [alt]="deal.name"
                    class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                    loading="lazy"
                  >
                } @else {
                  <div class="w-full h-full flex items-center justify-center text-4xl text-black">🛍️</div>
                }
              </div>

              <!-- Info -->
              <div class="p-3">
                <p class="text-xs text-white/60 truncate">{{ deal.brandName }}</p>
                <p class="text-sm font-medium truncate mb-1">{{ deal.name }}</p>
                <div class="flex items-center gap-2">
                  <span class="font-bold text-[#F9A825]">{{ (deal.salePrice ?? deal.price) | currencyInr }}</span>
                  @if (deal.salePrice != null && deal.salePrice < deal.price) {
                    <span class="text-xs text-white/50 line-through">{{ deal.price | currencyInr }}</span>
                  }
                </div>
                @if (deal.salePrice != null && deal.salePrice < deal.price) {
                  <app-badge variant="red" class="mt-1">{{ getDiscount(deal.price, deal.salePrice) }}% off</app-badge>
                }
              </div>
            </a>
          }
        </div>
      </div>
    </section>
  `,
})
export class FlashSaleComponent implements OnInit, OnDestroy {
  readonly timerDisplay = signal('02:59:59');
  readonly deals = signal<Product[]>([]);

  private timer: ReturnType<typeof setInterval> | null = null;
  private secondsLeft = 10799;
  private readonly catalogService = inject(CatalogService);

  ngOnInit(): void {
    this.timer = setInterval(() => {
      if (this.secondsLeft <= 0) {
        this.secondsLeft = 10799;
      } else {
        this.secondsLeft--;
      }
      this.timerDisplay.set(this.formatTime(this.secondsLeft));
    }, 1000);

    this.catalogService.getProducts({
      categoryId: null,
      brandId: null,
      search: null,
      minPrice: null,
      maxPrice: null,
      sort: 'price_asc',
      page: 1,
      pageSize: 5
    }).subscribe(res => {
      this.deals.set(res.items);
    });
  }

  ngOnDestroy(): void {
    if (this.timer) clearInterval(this.timer);
  }

  private formatTime(seconds: number): string {
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    const s = seconds % 60;
    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
  }

  getDiscount(price: number, salePrice: number): number {
    if (!salePrice || salePrice >= price) return 0;
    return Math.round((1 - salePrice / price) * 100);
  }
}
