import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProductVariant } from '../core/models/product.model';

@Component({
  selector: 'app-size-selector',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: `
    <!-- DESIGN.md §4.11 Size Selector — variant-aware stock display -->
    <div>
      <div class="flex items-center justify-between mb-3">
        <span class="text-sm font-semibold text-dark">Select Size</span>
        <button
          class="text-xs text-red hover:underline"
          aria-label="View size guide"
          (click)="sizeGuideClick.emit()"
        >Size Guide</button>
      </div>

      <!-- Pill chips — horizontal flex wrap -->
      <div class="flex flex-wrap gap-2" role="radiogroup" aria-label="Available sizes">
        @for (size of uniqueSizes; track size) {
          @let oos = isOutOfStock(size);
          @let lowStock = getLowStock(size);
          <div class="relative">
            <button
              class="min-w-[36px] h-9 px-2 border-2 rounded text-sm font-medium transition-all"
              [class.border-red]="selectedSize === size && !oos"
              [class.bg-red]="selectedSize === size && !oos"
              [class.text-white]="selectedSize === size && !oos"
              [class.border-border]="selectedSize !== size && !oos"
              [class.text-dark]="selectedSize !== size && !oos"
              [class.hover:border-red]="selectedSize !== size && !oos"
              [class.border-border]="oos"
              [class.text-muted]="oos"
              [class.line-through]="oos"
              [class.opacity-50]="oos"
              [class.cursor-not-allowed]="oos"
              [disabled]="oos"
              role="radio"
              [attr.aria-checked]="selectedSize === size"
              [attr.aria-label]="'Size ' + size + (oos ? ' — out of stock' : (lowStock !== null ? ' — only ' + lowStock + ' left' : ''))"
              (click)="!oos && sizeChange.emit(size)"
            >{{ size }}</button>
            <!-- "Only X left" badge -->
            @if (lowStock !== null && !oos) {
              <span
                class="absolute -top-2 -right-2 bg-red text-white text-[9px] font-bold
                       leading-none px-1 py-0.5 rounded-full pointer-events-none"
                aria-hidden="true"
              >{{ lowStock }}</span>
            }
          </div>
        }
      </div>

      <!-- Low-stock inline hint for selected size -->
      @if (selectedSize) {
        @let hint = getLowStock(selectedSize);
        @if (hint !== null) {
          <p class="mt-2 text-xs text-red font-medium">Only {{ hint }} left in this size!</p>
        }
      }
    </div>
  `,
})
export class SizeSelectorComponent {
  /** Full variant list — used for stock-aware display. */
  @Input({ required: true }) variants: ProductVariant[] = [];
  @Input() selectedSize: string | null = null;
  @Output() sizeChange     = new EventEmitter<string>();
  @Output() sizeGuideClick = new EventEmitter<void>();

  /** Unique sizes, excluding ONE SIZE, preserving order. */
  get uniqueSizes(): string[] {
    const seen = new Set<string>();
    const result: string[] = [];
    for (const v of this.variants) {
      if (v.size !== 'ONE SIZE' && !seen.has(v.size)) {
        seen.add(v.size);
        result.push(v.size);
      }
    }
    return result;
  }

  /** True when every variant for this size has stockQuantity === 0. */
  isOutOfStock(size: string): boolean {
    const matching = this.variants.filter((v) => v.size === size);
    return matching.length > 0 && matching.every((v) => v.stockQuantity === 0);
  }

  /**
   * Returns the total stock for this size when it is low (< 5) and > 0.
   * Returns null when stock is fine or the size is OOS.
   */
  getLowStock(size: string): number | null {
    const total = this.variants
      .filter((v) => v.size === size)
      .reduce((sum, v) => sum + v.stockQuantity, 0);
    return total > 0 && total < 5 ? total : null;
  }
}
