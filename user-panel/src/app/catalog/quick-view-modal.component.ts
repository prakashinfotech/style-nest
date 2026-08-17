/**
 * ENH-CAT-004 — Quick View Modal.
 *
 * Full-screen backdrop dialog that shows an abbreviated product panel:
 *   image carousel dots · brand · name · price · size selector · ATC button · "View Full Details" link.
 *
 * Opened by ProductCardComponent via an event that bubbles up to PlpComponent.
 *
 * Accessibility:
 *   - role="dialog" aria-modal="true" aria-labelledby ties the modal title
 *   - Keyboard: Escape closes, Tab trapped inside (browser default for modal)
 *   - Backdrop click closes; inner panel stops propagation
 */

import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  HostListener,
  Input,
  OnInit,
  Output,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { Product } from '../core/models/product.model';
import { CartActions } from '../store/cart/cart.actions';
import { CurrencyInrPipe } from '../shared/pipes/currency-inr.pipe';
import { StarRatingComponent } from '../shared/components/star-rating.component';

@Component({
  selector: 'app-quick-view-modal',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, CurrencyInrPipe, StarRatingComponent],
  template: `
    <!-- Backdrop -->
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
      role="dialog"
      aria-modal="true"
      [attr.aria-labelledby]="'qv-title-' + product.id"
      (click)="onBackdropClick($event)"
    >
      <!-- Modal panel — stops backdrop click propagation -->
      <div
        class="relative bg-white rounded-xl shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-y-auto
               flex flex-col sm:flex-row"
        (click)="$event.stopPropagation()"
      >
        <!-- Close button -->
        <button
          class="absolute top-3 right-3 z-10 w-8 h-8 rounded-full bg-gray-100 hover:bg-gray-200
                 flex items-center justify-center transition
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-navy"
          aria-label="Close quick view"
          (click)="close.emit()"
        >
          <svg class="w-4 h-4 text-dark" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
          </svg>
        </button>

        <!-- Image panel -->
        <div class="sm:w-56 flex-shrink-0 bg-gray-50 rounded-t-xl sm:rounded-l-xl sm:rounded-tr-none
                    overflow-hidden relative" style="min-height:220px">
          @if (product.imageUrls.length > 0) {
            <img
              [src]="product.imageUrls[activeImage()]"
              [alt]="product.brandName + ' ' + product.name"
              class="w-full h-full object-cover absolute inset-0"
              loading="eager"
            />
          } @else {
            <div class="w-full h-full flex items-center justify-center text-5xl" aria-hidden="true">🛍️</div>
          }

          <!-- Discount badge -->
          @if (discountPercent > 0) {
            <div
              class="absolute top-2 left-2 bg-red text-white text-[11px] font-medium px-2.5 py-0.5 rounded-full z-10"
              aria-label="{{ discountPercent }}% discount"
            >
              {{ discountPercent }}% off
            </div>
          }

          <!-- Dot navigation for multiple images -->
          @if (product.imageUrls.length > 1) {
            <div class="absolute bottom-2 inset-x-0 flex justify-center gap-1.5 z-10">
              @for (img of product.imageUrls; track img; let i = $index) {
                <button
                  type="button"
                  [attr.aria-label]="'View image ' + (i + 1)"
                  class="w-2 h-2 rounded-full transition focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-white"
                  [class.bg-dark]="activeImage() === i"
                  [class.bg-white]="activeImage() !== i"
                  [class.opacity-60]="activeImage() !== i"
                  (click)="activeImage.set(i)"
                ></button>
              }
            </div>
          }
        </div>

        <!-- Info panel -->
        <div class="flex-1 p-5 flex flex-col min-w-0">
          <!-- Brand -->
          <p class="text-[11px] uppercase tracking-widest text-mid-gray mb-0.5 truncate">
            {{ product.brandName }}
          </p>

          <!-- Product name -->
          <h2
            [id]="'qv-title-' + product.id"
            class="text-base font-semibold text-dark leading-snug mb-3"
          >
            {{ product.name }}
          </h2>

          <!-- Price row -->
          <div class="flex items-center gap-2 flex-wrap mb-3">
            <span class="text-xl font-bold text-dark">
              {{ product.salePrice ?? product.price | currencyInr }}
            </span>
            @if (product.salePrice != null && product.salePrice < product.price) {
              <span class="text-sm text-mid-gray line-through">{{ product.price | currencyInr }}</span>
              <span class="text-sm font-medium text-red">{{ discountPercent }}% off</span>
            }
          </div>

          <!-- Star rating -->
          @if (product.reviewCount > 0) {
            <div class="mb-3">
              <app-star-rating [rating]="product.rating" [reviewCount]="product.reviewCount" />
            </div>
          }

          <!-- Size selector -->
          @if (sizes().length > 0) {
            <div class="mb-4">
              <p class="text-xs font-medium text-dark mb-2">
                Size
                @if (selectedSize()) {
                  <span class="font-normal text-muted ml-1">— {{ selectedSize() }}</span>
                }
              </p>
              <div class="flex flex-wrap gap-2" role="group" aria-label="Select size">
                @for (sz of sizes(); track sz.size) {
                  <button
                    type="button"
                    [attr.aria-label]="'Size ' + sz.size + (sz.inStock ? '' : ', out of stock')"
                    [attr.aria-pressed]="selectedSize() === sz.size"
                    [disabled]="!sz.inStock"
                    class="px-3 py-1.5 text-xs border rounded transition
                           disabled:opacity-40 disabled:cursor-not-allowed
                           focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-navy"
                    [class.border-navy]="selectedSize() === sz.size"
                    [class.bg-navy]="selectedSize() === sz.size"
                    [class.text-white]="selectedSize() === sz.size"
                    [class.border-border]="selectedSize() !== sz.size"
                    [class.text-dark]="selectedSize() !== sz.size"
                    (click)="selectSize(sz.size)"
                  >
                    {{ sz.size }}
                  </button>
                }
              </div>
              @if (sizeRequired()) {
                <p class="text-xs text-red-600 mt-1" role="alert">Please select a size.</p>
              }
            </div>
          }

          <!-- Out of stock notice -->
          @if (!product.inStock) {
            <p class="text-sm font-medium text-red mb-3">Out of Stock</p>
          }

          <!-- CTA buttons — pushed to bottom of flex column -->
          <div class="mt-auto flex flex-col gap-2 pt-3">
            <button
              type="button"
              [disabled]="!product.inStock || addingToCart()"
              class="w-full py-2.5 bg-red text-white text-sm font-semibold rounded
                     hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed
                     transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500"
              (click)="onAddToCart()"
            >
              @if (addingToCart()) { Adding… } @else { Add to Cart }
            </button>

            <a
              [routerLink]="['/products', product.id]"
              (click)="close.emit()"
              class="w-full py-2.5 border border-navy text-navy text-sm font-semibold rounded text-center
                     hover:bg-navy hover:text-white transition-colors
                     focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-navy"
            >
              View Full Details →
            </a>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class QuickViewModalComponent implements OnInit {
  @Input({ required: true }) product!: Product;
  @Output() close = new EventEmitter<void>();

  private readonly store = inject(Store);

  readonly activeImage  = signal(0);
  readonly selectedSize = signal<string | null>(null);
  readonly sizeRequired = signal(false);
  readonly addingToCart = signal(false);

  ngOnInit(): void {
    // Pre-select the first in-stock size
    const first = this.product.variants.find((v) => v.stockQuantity > 0);
    if (first) this.selectedSize.set(first.size);
  }

  /** Deduplicated sizes from variants with per-size in-stock status. */
  sizes(): Array<{ size: string; inStock: boolean }> {
    const seen = new Set<string>();
    const result: Array<{ size: string; inStock: boolean }> = [];
    for (const v of this.product.variants) {
      if (!seen.has(v.size)) {
        seen.add(v.size);
        result.push({ size: v.size, inStock: v.stockQuantity > 0 });
      }
    }
    return result;
  }

  get discountPercent(): number {
    if (!this.product.salePrice || this.product.salePrice >= this.product.price) return 0;
    return Math.round((1 - this.product.salePrice / this.product.price) * 100);
  }

  selectSize(size: string): void {
    this.selectedSize.set(size);
    this.sizeRequired.set(false);
  }

  onAddToCart(): void {
    if (!this.product.inStock) return;
    const hasSizes = this.product.variants.length > 0;
    if (hasSizes && !this.selectedSize()) {
      this.sizeRequired.set(true);
      return;
    }
    this.sizeRequired.set(false);
    this.addingToCart.set(true);
    this.store.dispatch(
      CartActions.addItem({
        productId: this.product.id,
        size:      this.selectedSize(),
        colour:    null,
        quantity:  1,
      }),
    );
    // Brief "Adding…" feedback before closing
    setTimeout(() => {
      this.addingToCart.set(false);
      this.close.emit();
    }, 600);
  }

  onBackdropClick(e: MouseEvent): void {
    if (e.target === e.currentTarget) this.close.emit();
  }

  @HostListener('document:keydown.Escape')
  onEscape(): void {
    this.close.emit();
  }
}
