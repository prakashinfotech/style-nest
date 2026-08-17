import {
  ChangeDetectionStrategy, Component, EventEmitter,
  Input, OnChanges, Output, signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { inject } from '@angular/core';
import { Product } from '../core/models/product.model';
import { CartActions } from '../store/cart/cart.actions';
import { WishlistActions } from '../store/wishlist/wishlist.actions';
import { CurrencyInrPipe } from '../shared/pipes/currency-inr.pipe';
import { StarRatingComponent } from '../shared/components/star-rating.component';

@Component({
  selector: 'app-product-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, CurrencyInrPipe, StarRatingComponent],
  template: `
    <!-- DESIGN.md §4.7 Product Card — wishlist filled-heart + pop animation -->
    <article
      class="bg-card rounded-md overflow-hidden group relative cursor-pointer transition-shadow duration-300 hover:shadow-card-hover"
      [attr.aria-label]="product.brandName + ' — ' + product.name"
    >
      <!-- Image container — 3:4 portrait aspect ratio -->
      <a
        [routerLink]="['/products', product.id]"
        class="block aspect-[3/4] overflow-hidden bg-gray-100 relative"
        tabindex="-1"
        aria-hidden="true"
      >
        @if (product.imageUrls.length > 0) {
          <img
            [src]="product.imageUrls[0]"
            [alt]="product.brandName + ' ' + product.name"
            class="w-full h-full object-cover transition-transform duration-300 group-hover:scale-[1.04]"
            loading="lazy"
            width="300"
            height="400"
          />
        } @else {
          <div class="w-full h-full flex items-center justify-center text-5xl bg-gray-50" aria-hidden="true">🛍️</div>
        }

        <!-- Discount badge — top-left pill -->
        @if (discountPercent > 0) {
          <div
            class="absolute top-2 left-2 z-10 bg-red text-white text-[11px] font-medium px-2.5 py-0.5 rounded-full"
            aria-label="{{ discountPercent }}% discount"
          >
            {{ discountPercent }}% off
          </div>
        }

        <!-- Wishlist button — DESIGN.md §4.7: filled heart when wishlisted, heart-pop on click -->
        <button
          class="absolute top-2 right-2 z-10 bg-white/80 hover:bg-white rounded-full p-1.5
                 md:opacity-0 md:group-hover:opacity-100 opacity-100
                 transition-all duration-200 min-h-[36px] min-w-[36px] flex items-center justify-center
                 focus-visible:opacity-100"
          [attr.aria-label]="(isWishlisted ? 'Remove ' : 'Add ') + product.name + ' to wishlist'"
          [attr.aria-pressed]="isWishlisted"
          (click)="onWishlist($event)"
        >
          <!-- Filled heart when wishlisted -->
          @if (isWishlisted) {
            <svg
              class="w-4 h-4 text-red"
              [class.heart-pop]="heartAnimating()"
              fill="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"/>
            </svg>
          } @else {
            <svg
              class="w-4 h-4 text-mid-gray hover:text-red transition-colors"
              [class.heart-pop]="heartAnimating()"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"/>
            </svg>
          }
        </button>

        <!-- ENH-CAT-004 Quick View — slides up from bottom on hover -->
        <div class="absolute inset-x-0 bottom-0 translate-y-full group-hover:translate-y-0 transition-transform duration-300 z-10">
          <button
            class="w-full bg-dark/90 hover:bg-dark text-white text-xs font-medium py-2.5 transition
                   focus-visible:outline-none focus-visible:bg-dark"
            [attr.aria-label]="'Quick view ' + product.name"
            (click)="$event.preventDefault(); $event.stopPropagation(); onQuickView()"
          >
            Quick View
          </button>
        </div>
      </a>

      <!-- Info block — DESIGN.md §4.7 -->
      <div class="pt-3 pb-2 px-1">
        <!-- Brand — 11px DM Sans UPPERCASE tracking-widest mid-gray -->
        <p class="text-[11px] uppercase tracking-widest text-mid-gray truncate mb-0.5">
          {{ product.brandName }}
        </p>

        <!-- Product name — 14px medium dark, 2-line clamp -->
        <a [routerLink]="['/products', product.id]">
          <h3 class="text-[14px] font-medium text-dark leading-snug line-clamp-2 hover:text-red transition-colors mb-1.5">
            {{ product.name }}
          </h3>
        </a>

        <!-- Price row -->
        <div class="flex items-center gap-2 flex-wrap mb-1">
          <span class="text-[15px] font-semibold text-dark">
            {{ product.salePrice ?? product.price | currencyInr }}
          </span>
          @if (product.salePrice != null && product.salePrice < product.price) {
            <span class="text-[13px] text-mid-gray line-through">{{ product.price | currencyInr }}</span>
            <span class="text-[13px] font-medium text-red">{{ discountPercent }}% off</span>
          }
        </div>

        <!-- Rating row -->
        @if (product.reviewCount > 0) {
          <app-star-rating [rating]="product.rating" [reviewCount]="product.reviewCount" />
        }
      </div>
    </article>
  `,
})
export class ProductCardComponent implements OnChanges {
  @Input({ required: true }) product!: Product;
  @Input() isWishlisted = false;
  @Output() wishlistToggle = new EventEmitter<string>();
  /** ENH-CAT-004 — emits the product when Quick View button is clicked. */
  @Output() quickView = new EventEmitter<Product>();

  private readonly store = inject(Store);

  readonly heartAnimating = signal(false);

  ngOnChanges(): void {
    // reset animation state when input changes
  }

  get discountPercent(): number {
    if (!this.product.salePrice || this.product.salePrice >= this.product.price) return 0;
    return Math.round((1 - this.product.salePrice / this.product.price) * 100);
  }

  onQuickView(): void {
    this.quickView.emit(this.product);
  }

  onWishlist(event: Event): void {
    event.preventDefault();
    event.stopPropagation();

    // Trigger heart-pop animation
    this.heartAnimating.set(true);
    setTimeout(() => this.heartAnimating.set(false), 320);

    this.store.dispatch(WishlistActions.toggle({ productId: this.product.id }));
    this.wishlistToggle.emit(this.product.id);
  }
}
