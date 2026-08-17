import {
  ChangeDetectionStrategy, Component, Input, OnInit, inject,
} from '@angular/core';
import { AsyncPipe, CommonModule } from '@angular/common';
import { Store } from '@ngrx/store';
import { Observable, catchError, combineLatest, map, of, startWith } from 'rxjs';
import { CatalogService } from '../core/services/catalog.service';
import { Product, ProductFilters } from '../core/models/product.model';
import { selectWishlistIds } from '../store/wishlist/wishlist.selectors';
import { ProductCardComponent } from '../catalog/product-card.component';
import { SectionHeaderComponent } from '../shared/components/section-header.component';
import { SkeletonLoaderComponent } from '../shared/components/skeleton-loader.component';

interface FeaturedVm {
  products:    Product[];
  wishlistIds: string[];
  loading:     boolean;
  error:       boolean;
}

@Component({
  selector: 'app-featured-products',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, AsyncPipe,
    ProductCardComponent,
    SectionHeaderComponent,
    SkeletonLoaderComponent,
  ],
  template: `
    <!-- DESIGN.md §5.1 Homepage — featured product section with live API data -->
    <section class="max-w-layout mx-auto px-4 md:px-6 py-8 md:py-12">

      <app-section-header
        [title]="title"
        [eyebrow]="eyebrow"
        [viewAllLink]="viewAllLink"
        [viewAllParams]="viewAllParams"
      />

      @if (vm$ | async; as vm) {

        <!-- Loading skeletons — DESIGN.md §4.15 card skeleton pattern -->
        @if (vm.loading) {
          <div [class]="gridClass">
            @for (_ of skeletons; track $index) {
              <div class="rounded-md overflow-hidden bg-card">
                <div [class]="compact ? 'aspect-[2/3] w-full shimmer' : 'aspect-[3/4] w-full shimmer'"></div>
                <div class="p-2 space-y-2 mt-1">
                  <app-skeleton-loader height="10px" width="60%" />
                  <app-skeleton-loader height="14px" width="90%" />
                  <app-skeleton-loader height="12px" width="50%" />
                </div>
              </div>
            }
          </div>
        }

        <!-- Error state — DESIGN.md §13 -->
        @if (!vm.loading && vm.error) {
          <div
            class="border-l-4 border-red bg-red/5 rounded-md p-4 text-sm text-red"
            role="alert"
          >
            Could not load products. Please try again later.
          </div>
        }

        <!-- Product grid — compact: 5-col desktop / standard: 4-col desktop -->
        @if (!vm.loading && !vm.error && vm.products.length > 0) {
          <div [class]="gridClass">
            @for (product of vm.products; track product.id) {
              <app-product-card
                [product]="product"
                [isWishlisted]="vm.wishlistIds.includes(product.id)"
              />
            }
          </div>
        }

      }

    </section>
  `,
})
export class FeaturedProductsComponent implements OnInit {
  @Input() title = 'New Arrivals';
  @Input() eyebrow: string | null = 'Just In';
  @Input() viewAllLink: string | null = '/products';
  @Input() viewAllParams: Record<string, string> | null = null;
  @Input() pageSize = 8;
  @Input() sort: ProductFilters['sort'] = 'newest';
  /** compact: smaller cards, more columns — ideal for single-row homepage sections */
  @Input() compact = false;

  private readonly catalogService = inject(CatalogService);
  private readonly store          = inject(Store);

  get skeletons(): null[] { return new Array(this.pageSize).fill(null); }

  get gridClass(): string {
    return this.compact
      ? 'grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-3 md:gap-4'
      : 'grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-4 gap-4 md:gap-5';
  }

  vm$!: Observable<FeaturedVm>;

  ngOnInit(): void {
    const products$ = this.catalogService.getProducts({
      categoryId:  null,
      brandId:     null,
      search:      null,
      minPrice:    null,
      maxPrice:    null,
      minDiscount: null,
      sort:        this.sort,
      page:        1,
      pageSize:    this.pageSize,
    }).pipe(
      map((result) => ({ products: result.items, loading: false, error: false })),
      startWith({ products: [] as Product[], loading: true, error: false }),
      catchError(() => of({ products: [] as Product[], loading: false, error: true })),
    );

    const wishlistIds$ = this.store.select(selectWishlistIds);

    this.vm$ = combineLatest([products$, wishlistIds$]).pipe(
      map(([state, wishlistIds]) => ({ ...state, wishlistIds })),
    );
  }
}
