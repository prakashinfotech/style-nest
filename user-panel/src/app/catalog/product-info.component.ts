import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Product } from '../core/models/product.model';
import { CurrencyInrPipe } from '../shared/pipes/currency-inr.pipe';
import { StarRatingComponent } from '../shared/components/star-rating.component';
import { BadgeComponent } from '../shared/components/badge.component';

@Component({
  selector: 'app-product-info',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, CurrencyInrPipe, StarRatingComponent, BadgeComponent],
  template: `
    <div class="space-y-3">
      <!-- Brand -->
      <a
        [routerLink]="['/products']"
        [queryParams]="{ brand: product.brandId }"
        class="text-blue text-sm font-medium hover:underline"
      >{{ product.brandName }}</a>

      <!-- Product name -->
      <h1 class="text-xl md:text-2xl font-bold text-dark leading-snug">{{ product.name }}</h1>

      <!-- Rating -->
      @if (product.reviewCount > 0) {
        <app-star-rating [rating]="product.rating" [reviewCount]="product.reviewCount" />
      }

      <!-- Price block — shows variant priceOverride when a variant is selected -->
      <div class="flex items-baseline gap-3">
        <span class="text-2xl font-bold text-dark">
          {{ effectivePrice | currencyInr }}
        </span>
        @if (product.salePrice != null && product.salePrice < product.price && variantPrice === null) {
          <span class="text-base text-muted line-through">{{ product.price | currencyInr }}</span>
          <app-badge variant="red">{{ discountPercent }}% off</app-badge>
        }
        @if (variantPrice !== null) {
          <span class="text-xs text-muted">Variant price</span>
        }
      </div>

      <!-- In stock indicator -->
      @if (product.inStock) {
        <p class="text-success text-sm font-medium flex items-center gap-1">
          <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
          </svg>
          In Stock
        </p>
      } @else {
        <p class="text-red text-sm font-medium">Out of Stock</p>
      }

      <!-- Taxes note -->
      <p class="text-xs text-muted">Inclusive of all taxes. Free delivery on orders above ₹500.</p>
    </div>
  `,
})
export class ProductInfoComponent {
  @Input({ required: true }) product!: Product;
  /** When a variant is selected, pass its priceOverride here to override the displayed price. */
  @Input() variantPrice: number | null = null;

  get effectivePrice(): number {
    if (this.variantPrice !== null) return this.variantPrice;
    return this.product.salePrice ?? this.product.price;
  }

  get discountPercent(): number {
    if (!this.product.salePrice || this.product.salePrice >= this.product.price) return 0;
    return Math.round((1 - this.product.salePrice / this.product.price) * 100);
  }
}
