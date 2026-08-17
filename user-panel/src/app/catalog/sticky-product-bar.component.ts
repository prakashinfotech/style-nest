import {
  ChangeDetectionStrategy,
  Component,
  Input,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Store } from '@ngrx/store';
import { CartActions } from '../store/cart/cart.actions';
import { CurrencyInrPipe } from '../shared/pipes/currency-inr.pipe';
import { Product } from '../core/models/product.model';

/**
 * Compact sticky bar that appears when the main ATC panel scrolls out of view.
 * Visibility is driven by an IntersectionObserver in the parent PDP component
 * via the `visible` input.
 */
@Component({
  selector: 'app-sticky-product-bar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, CurrencyInrPipe],
  template: `
    <div
      class="fixed top-0 inset-x-0 z-50 bg-white border-b border-border shadow-sticky
             transition-transform duration-300"
      [class.-translate-y-full]="!visible"
      [class.translate-y-0]="visible"
      role="region"
      aria-label="Quick purchase bar"
    >
      <div class="max-w-layout mx-auto px-4 h-14 flex items-center gap-4">
        <!-- Thumbnail -->
        @if (product.imageUrls.length > 0) {
          <img
            [src]="product.imageUrls[0]"
            [alt]="product.name"
            class="w-10 h-10 object-cover rounded flex-shrink-0"
            loading="lazy"
          />
        }

        <!-- Name + price -->
        <div class="flex-1 min-w-0">
          <p class="text-sm font-semibold text-dark truncate">{{ product.name }}</p>
          <p class="text-sm text-red font-bold">
            {{ (product.salePrice ?? product.price) | currencyInr }}
          </p>
        </div>

        <!-- Add to Bag CTA -->
        <button
          class="flex-shrink-0 h-9 px-5 bg-red text-white text-sm font-semibold rounded-md
                 hover:bg-red/90 active:scale-[0.97] transition-all
                 disabled:opacity-50 disabled:cursor-not-allowed
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red"
          [disabled]="!product.inStock || !canProceed"
          (click)="addToCart()"
          [attr.aria-label]="product.inStock ? 'Add ' + product.name + ' to bag' : 'Out of stock'"
        >
          {{ product.inStock ? 'ADD TO BAG' : 'OUT OF STOCK' }}
        </button>
      </div>
    </div>
  `,
})
export class StickyProductBarComponent {
  @Input({ required: true }) product!: Product;
  @Input() visible        = false;
  @Input() selectedSize:   string | null = null;
  @Input() selectedColour: string | null = null;
  @Input() requiresSize    = false;
  @Input() requiresColour  = false;
  @Input() quantity        = 1;

  private readonly store = inject(Store);

  get canProceed(): boolean {
    if (this.requiresSize && !this.selectedSize) return false;
    if (this.requiresColour && !this.selectedColour) return false;
    return true;
  }

  addToCart(): void {
    this.store.dispatch(CartActions.addItem({
      productId: this.product.id,
      size:      this.selectedSize,
      colour:    this.selectedColour,
      quantity:  this.quantity,
    }));
  }
}
