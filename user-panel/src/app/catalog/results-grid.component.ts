import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Product } from '../core/models/product.model';
import { ProductCardComponent } from './product-card.component';
import { SkeletonLoaderComponent } from '../shared/components/skeleton-loader.component';
import { EmptyStateComponent } from '../shared/components/empty-state/empty-state.component';
import { InfiniteScrollDirective } from '../shared/directives/infinite-scroll.directive';

/** ENH-CAT-005 — persisted localStorage key for scroll mode preference. */
const SCROLL_MODE_KEY = 'sn_scroll_mode';

@Component({
  selector: 'app-results-grid',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ProductCardComponent, SkeletonLoaderComponent, EmptyStateComponent, InfiniteScrollDirective],
  template: `
    <!-- ENH-CAT-005 — Scroll mode toggle (top-right of grid) -->
    @if (!isLoading && products.length > 0) {
      <div class="flex justify-end mb-3">
        <div
          class="inline-flex rounded-md border border-border overflow-hidden text-xs font-medium"
          role="group"
          aria-label="Product display mode"
        >
          <button
            type="button"
            [attr.aria-pressed]="scrollMode() === 'paginate'"
            class="px-3 py-1.5 transition"
            [class.bg-navy]="scrollMode() === 'paginate'"
            [class.text-white]="scrollMode() === 'paginate'"
            [class.text-dark]="scrollMode() !== 'paginate'"
            [class.hover:bg-gray-50]="scrollMode() !== 'paginate'"
            (click)="setScrollMode('paginate')"
          >
            Paginate
          </button>
          <button
            type="button"
            [attr.aria-pressed]="scrollMode() === 'infinite'"
            class="px-3 py-1.5 border-l border-border transition"
            [class.bg-navy]="scrollMode() === 'infinite'"
            [class.text-white]="scrollMode() === 'infinite'"
            [class.text-dark]="scrollMode() !== 'infinite'"
            [class.hover:bg-gray-50]="scrollMode() !== 'infinite'"
            (click)="setScrollMode('infinite')"
          >
            Infinite Scroll
          </button>
        </div>
      </div>
    }

    <!-- Loading skeletons -->
    @if (isLoading && products.length === 0) {
      <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3 md:gap-4">
        @for (n of skeletons; track n) {
          <div class="bg-card rounded-lg overflow-hidden shadow-sm">
            <app-skeleton-loader height="260px" />
            <div class="p-3 space-y-2">
              <app-skeleton-loader height="12px" width="60%" />
              <app-skeleton-loader height="14px" />
              <app-skeleton-loader height="14px" width="80%" />
              <app-skeleton-loader height="16px" width="40%" />
            </div>
          </div>
        }
      </div>
    }

    <!-- Empty state -->
    @if (!isLoading && products.length === 0) {
      <app-empty-state
        icon="🔍"
        title="No products found"
        subtitle="Try adjusting your filters or search terms."
        ctaLabel="Clear Filters"
        (ctaClick)="clearFilters.emit()"
      />
    }

    <!-- Product grid -->
    @if (products.length > 0) {
      <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3 md:gap-4">
        @for (product of products; track product.id) {
          <app-product-card
            [product]="product"
            [isWishlisted]="wishlistIds.includes(product.id)"
            (wishlistToggle)="wishlistToggle.emit($event)"
            (quickView)="quickView.emit($event)"
          />
        }
      </div>

      <!-- ── PAGINATE mode ── -->
      @if (scrollMode() === 'paginate' && totalCount > pageSize) {
        <div class="flex justify-center items-center gap-2 mt-8">
          <button
            class="px-3 py-1.5 border border-gray-200 rounded text-sm hover:border-navy hover:text-navy transition disabled:opacity-40"
            [disabled]="currentPage <= 1"
            (click)="pageChange.emit(currentPage - 1)"
          >← Prev</button>

          <span class="text-sm text-muted px-2">
            Page {{ currentPage }} of {{ totalPages }}
          </span>

          <button
            class="px-3 py-1.5 border border-gray-200 rounded text-sm hover:border-navy hover:text-navy transition disabled:opacity-40"
            [disabled]="currentPage >= totalPages"
            (click)="pageChange.emit(currentPage + 1)"
          >Next →</button>
        </div>
      }

      <!-- ── INFINITE SCROLL mode — sentinel + loader ── -->
      @if (scrollMode() === 'infinite' && products.length < totalCount) {
        <div
          appInfiniteScroll
          (scrolled)="onSentinelVisible()"
          class="py-6 flex justify-center"
          aria-label="Loading more products"
        >
          @if (isLoading) {
            <span class="text-sm text-muted animate-pulse">Loading more products…</span>
          } @else {
            <!-- Invisible trigger zone -->
            <span class="sr-only">Scroll sentinel</span>
          }
        </div>
      }

      <!-- End-of-list message in infinite mode -->
      @if (scrollMode() === 'infinite' && products.length >= totalCount && totalCount > 0) {
        <p class="text-center text-xs text-muted mt-6 py-2">
          You've seen all {{ totalCount }} products.
        </p>
      }
    }
  `,
})
export class ResultsGridComponent {
  @Input({ required: true }) products: Product[] = [];
  @Input() wishlistIds: string[] = [];
  @Input() totalCount   = 0;
  @Input() currentPage  = 1;
  @Input() pageSize     = 24;
  @Input() isLoading    = false;
  @Output() pageChange      = new EventEmitter<number>();
  @Output() wishlistToggle  = new EventEmitter<string>();
  @Output() clearFilters    = new EventEmitter<void>();
  /** ENH-CAT-004 — bubbles Quick View product selection to host. */
  @Output() quickView       = new EventEmitter<Product>();
  /** ENH-CAT-005 — request the next page in infinite scroll mode. */
  @Output() loadMore        = new EventEmitter<void>();

  readonly skeletons = Array.from({ length: 12 }, (_, i) => i);

  /** ENH-CAT-005 — scroll mode preference, initialised from localStorage. */
  readonly scrollMode = signal<'paginate' | 'infinite'>(
    (localStorage.getItem(SCROLL_MODE_KEY) as 'paginate' | 'infinite' | null) ?? 'paginate',
  );

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  setScrollMode(mode: 'paginate' | 'infinite'): void {
    this.scrollMode.set(mode);
    localStorage.setItem(SCROLL_MODE_KEY, mode);
  }

  /** Called by InfiniteScrollDirective when the sentinel enters the viewport. */
  onSentinelVisible(): void {
    if (!this.isLoading && this.products.length < this.totalCount) {
      this.loadMore.emit();
    }
  }
}
