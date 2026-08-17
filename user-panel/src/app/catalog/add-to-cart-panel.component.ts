import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
  inject,
} from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { Store } from '@ngrx/store';
import { map } from 'rxjs';
import { Product } from '../core/models/product.model';
import { CartActions } from '../store/cart/cart.actions';
import { OrderActions } from '../store/order/order.actions';
import { WishlistActions } from '../store/wishlist/wishlist.actions';
import { selectWishlistIds } from '../store/wishlist/wishlist.selectors';
import { selectIsLoggedIn } from '../store/auth/auth.selectors';

@Component({
  selector: 'app-add-to-cart-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AsyncPipe],
  template: `
    <!-- DESIGN.md §4.10 Add to Cart / Buy Now Buttons -->
    <div class="space-y-3">

      <!-- Quantity selector -->
      <div class="flex items-center gap-3">
        <span class="text-sm font-medium text-dark">Qty:</span>
        <div class="flex items-center border border-border rounded-md overflow-hidden">
          <button
            class="w-10 h-10 flex items-center justify-center hover:bg-bg text-lg font-medium disabled:opacity-40 transition"
            [disabled]="quantity <= 1"
            (click)="decrement()"
            aria-label="Decrease quantity"
          >−</button>
          <span class="w-10 text-center text-sm font-semibold" aria-live="polite" aria-atomic="true">{{ quantity }}</span>
          <button
            class="w-10 h-10 flex items-center justify-center hover:bg-bg text-lg font-medium disabled:opacity-40 transition"
            [disabled]="quantity >= 10"
            (click)="increment()"
            aria-label="Increase quantity"
          >+</button>
        </div>
      </div>

      <!-- Low-stock warning -->
      @if (variantStock !== null && variantStock > 0 && variantStock < 5) {
        <p class="text-xs text-red font-semibold flex items-center gap-1" role="alert">
          <svg class="w-3.5 h-3.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
            <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
          </svg>
          Only {{ variantStock }} left in stock!
        </p>
      }

      <!-- CTA buttons — DESIGN.md §4.10 -->
      <div class="flex gap-3">
        <!-- Add to Cart: white bg, red border, red text → hover: red bg white text -->
        <button
          class="flex-1 h-12 bg-white border-2 border-red text-red font-semibold rounded-md hover:bg-red hover:text-white active:scale-[0.97] transition-all text-[15px] tracking-[0.03em] disabled:opacity-50 disabled:cursor-not-allowed"
          [disabled]="!product.inStock || !canProceed"
          (click)="addToCart()"
          [attr.aria-label]="product.inStock ? 'Add ' + product.name + ' to cart' : 'Out of stock'"
        >
          {{ product.inStock ? 'ADD TO BAG' : 'OUT OF STOCK' }}
        </button>

        <!-- Buy Now: red bg, white text → hover: darken -->
        <button
          class="flex-1 h-12 bg-red text-white font-semibold rounded-md hover:bg-red/90 active:scale-[0.97] transition-all text-[15px] tracking-[0.03em] disabled:opacity-50 disabled:cursor-not-allowed"
          [disabled]="!product.inStock || !canProceed"
          (click)="buyNow()"
          [attr.aria-label]="'Buy ' + product.name + ' now'"
        >
          BUY NOW
        </button>
      </div>

      <!-- Wishlist toggle -->
      @if (isLoggedIn$ | async) {
        <button
          class="w-full flex items-center justify-center gap-2 py-2.5 border border-border rounded-md text-sm text-muted hover:border-red hover:text-red transition"
          (click)="toggleWishlist()"
          [attr.aria-label]="(isWishlisted$ | async) ? 'Remove from wishlist' : 'Add to wishlist'"
          [attr.aria-pressed]="isWishlisted$ | async"
        >
          <svg
            class="w-4 h-4"
            [attr.fill]="(isWishlisted$ | async) ? 'currentColor' : 'none'"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"/>
          </svg>
          {{ (isWishlisted$ | async) ? 'Remove from Wishlist' : 'Add to Wishlist' }}
        </button>
      }

      <!-- StyleNest Promise trust badges — DESIGN.md §8.1 -->
      <div class="flex flex-wrap gap-3 pt-1" role="list" aria-label="StyleNest Promise">
        @for (badge of trustBadges; track badge.label) {
          <div class="flex items-center gap-1.5 bg-bg rounded-full px-3 py-1.5" role="listitem">
            <span class="text-success" aria-hidden="true">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" [attr.d]="badge.iconPath"/>
              </svg>
            </span>
            <span class="text-[11px] font-medium text-dark">{{ badge.label }}</span>
          </div>
        }
      </div>
    </div>
  `,
})
export class AddToCartPanelComponent {
  @Input({ required: true }) product!: Product;
  @Input() selectedSize:   string | null = null;
  @Input() selectedColour: string | null = null;
  @Input() requiresSize    = false;
  @Input() requiresColour  = false;
  /** Quantity lifted to parent so desktop + mobile ATC instances share one value. */
  @Input() quantity        = 1;
  @Output() quantityChange = new EventEmitter<number>();
  /**
   * Stock for the currently selected variant.
   * Shows "Only X left!" warning when 0 < variantStock < 5.
   */
  @Input() variantStock: number | null = null;

  private readonly store = inject(Store);

  readonly isLoggedIn$   = this.store.select(selectIsLoggedIn);
  readonly wishlistIds$  = this.store.select(selectWishlistIds);
  readonly isWishlisted$ = this.wishlistIds$.pipe(
    map((ids) => ids.includes(this.product?.id ?? ''))
  );

  readonly trustBadges = [
    { label: 'Genuine Products', iconPath: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z' },
    { label: 'Free Delivery',    iconPath: 'M5 8h14M5 8a2 2 0 110-4h14a2 2 0 110 4M5 8v10a2 2 0 002 2h10a2 2 0 002-2V8m-9 4h4' },
    { label: 'Easy Returns',     iconPath: 'M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15' },
    { label: 'Quality Assured',  iconPath: 'M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z' },
  ];

  get canProceed(): boolean {
    if (this.requiresSize && !this.selectedSize) return false;
    if (this.requiresColour && !this.selectedColour) return false;
    return true;
  }

  increment(): void { this.quantityChange.emit(Math.min(this.quantity + 1, 10)); }
  decrement(): void { this.quantityChange.emit(Math.max(this.quantity - 1, 1)); }

  addToCart(): void {
    this.store.dispatch(CartActions.addItem({
      productId: this.product.id,
      size:      this.selectedSize,
      colour:    this.selectedColour,
      quantity:  this.quantity,
    }));
  }

  buyNow(): void {
    this.store.dispatch(OrderActions.buyNow({
      productId: this.product.id,
      size:      this.selectedSize,
      colour:    this.selectedColour,
      quantity:  this.quantity,
    }));
  }

  toggleWishlist(): void {
    this.store.dispatch(WishlistActions.toggle({ productId: this.product.id }));
  }
}
