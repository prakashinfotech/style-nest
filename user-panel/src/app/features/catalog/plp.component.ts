import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, AsyncPipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { combineLatest, filter, map, take } from 'rxjs';
import { CatalogActions } from '../../store/catalog/catalog.actions';
import {
  selectProducts, selectTotalCount, selectFilters,
  selectCatalogLoading, selectCategories,
} from '../../store/catalog/catalog.selectors';
import { selectWishlistIds } from '../../store/wishlist/wishlist.selectors';
import { ResultsGridComponent } from '../../catalog/results-grid.component';
import { FilterSidebarComponent, FilterState } from '../../catalog/filter-sidebar.component';
import { AppliedFiltersComponent, ActiveFilter } from '../../catalog/applied-filters.component';
import { SortDropdownComponent } from '../../catalog/sort-dropdown.component';
import { Product, ProductFilters } from '../../core/models/product.model';
import { BreadcrumbComponent, BreadcrumbItem } from '../../shared/components/breadcrumb.component';
import { CatalogService, AttributeDefinition } from '../../core/services/catalog.service';
import { QuickViewModalComponent } from '../../catalog/quick-view-modal.component';

@Component({
  selector: 'app-plp',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, AsyncPipe,
    ResultsGridComponent, FilterSidebarComponent,
    AppliedFiltersComponent, SortDropdownComponent,
    BreadcrumbComponent, QuickViewModalComponent,
  ],
  template: `
    <div class="max-w-layout mx-auto px-4 py-6 min-h-screen">

      <!-- Breadcrumb — DESIGN.md §4.17 -->
      <app-breadcrumb [crumbs]="(breadcrumbs$ | async) ?? []" />

      <!-- Page title -->
      <h1 class="text-xl md:text-2xl font-bold font-display text-dark mb-4">
        {{ pageTitle$ | async }}
      </h1>

      <!-- Applied filters -->
      <app-applied-filters
        [filters]="(activeFilters$ | async) ?? []"
        (remove)="removeFilter($event)"
        (clearAll)="clearAllFilters()"
        class="mb-4 block"
      />

      <div class="flex gap-6">

        <!-- Sidebar — desktop -->
        <div class="hidden lg:block w-56 flex-shrink-0">
          <app-filter-sidebar
            [categories]="(categories$ | async) ?? []"
            [brands]="[]"
            [attributes]="categoryAttributes()"
            [currentFilters]="(sidebarFilters$ | async) ?? emptyFilterState"
            (filtersChange)="onFiltersChange($event)"
          />
        </div>

        <!-- Results -->
        <div class="flex-1 min-w-0">
          <!-- Toolbar -->
          <div class="flex items-center justify-between mb-4 flex-wrap gap-3">
            <p class="text-sm text-muted">
              {{ (totalCount$ | async) ?? 0 | number }} products
            </p>
            <div class="flex items-center gap-3">
              <!-- Mobile filter toggle -->
              <button
                class="lg:hidden flex items-center gap-1 text-sm border border-gray-200 rounded px-3 py-1.5 hover:border-navy"
                (click)="showMobileFilters = !showMobileFilters"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 4a1 1 0 011-1h16a1 1 0 010 2H4a1 1 0 01-1-1zm3 6a1 1 0 011-1h10a1 1 0 010 2H7a1 1 0 01-1-1zm3 6a1 1 0 011-1h4a1 1 0 010 2h-4a1 1 0 01-1-1z"/>
                </svg>
                Filters
              </button>
              <app-sort-dropdown
                [currentSort]="(filters$ | async)?.sort ?? null"
                (sortChange)="onSortChange($event)"
              />
            </div>
          </div>

          <!-- Mobile filter drawer -->
          @if (showMobileFilters) {
            <div class="lg:hidden mb-4">
              <app-filter-sidebar
                [categories]="(categories$ | async) ?? []"
                [brands]="[]"
                [attributes]="categoryAttributes()"
                [currentFilters]="(sidebarFilters$ | async) ?? emptyFilterState"
                (filtersChange)="onFiltersChange($event)"
              />
            </div>
          }

          <app-results-grid
            [products]="(products$ | async) ?? []"
            [wishlistIds]="(wishlistIds$ | async) ?? []"
            [totalCount]="(totalCount$ | async) ?? 0"
            [currentPage]="(filters$ | async)?.page ?? 1"
            [pageSize]="(filters$ | async)?.pageSize ?? 24"
            [isLoading]="(isLoading$ | async) ?? false"
            (pageChange)="onPageChange($event)"
            (clearFilters)="clearAllFilters()"
            (quickView)="quickViewProduct.set($event)"
            (loadMore)="onLoadMore()"
          />
        </div>
      </div>
    </div>

    <!-- ENH-CAT-004 — Quick View Modal -->
    @if (quickViewProduct()) {
      <app-quick-view-modal
        [product]="quickViewProduct()!"
        (close)="quickViewProduct.set(null)"
      />
    }
  `,
})
export class PlpComponent implements OnInit {
  private readonly store         = inject(Store);
  private readonly route         = inject(ActivatedRoute);
  private readonly router        = inject(Router);
  private readonly catalogService = inject(CatalogService);

  showMobileFilters = false;
  /** ENH-CAT-004 — product currently shown in Quick View modal; null when closed. */
  readonly quickViewProduct = signal<Product | null>(null);

  readonly products$     = this.store.select(selectProducts);
  readonly totalCount$   = this.store.select(selectTotalCount);
  readonly filters$      = this.store.select(selectFilters);
  readonly isLoading$    = this.store.select(selectCatalogLoading);
  readonly categories$   = this.store.select(selectCategories);
  readonly wishlistIds$  = this.store.select(selectWishlistIds);
  readonly categoryAttributes = signal<AttributeDefinition[]>([]);

  readonly emptyFilterState: FilterState = {
    categoryId: null, minPrice: null, maxPrice: null, selectedBrandIds: [], minDiscount: null,
    selectedAttributes: {},
  };

  readonly sidebarFilters$ = this.filters$.pipe(
    map((f): FilterState => ({
      categoryId:         f.categoryId,
      minPrice:           f.minPrice,
      maxPrice:           f.maxPrice,
      selectedBrandIds:   f.brandId ? [f.brandId] : [],
      minDiscount:        f.minDiscount ?? null,
      selectedAttributes: {},
    })),
  );

  readonly pageTitle$ = combineLatest([this.filters$, this.categories$]).pipe(
    map(([filters, cats]) => {
      if (filters.search) return `Search Results for "${filters.search}"`;
      if (!filters.categoryId) return 'All Products';
      const cat = cats.find((c) => c.id === filters.categoryId);
      return cat?.name ?? 'Products';
    }),
  );

  readonly breadcrumbs$ = combineLatest([this.filters$, this.categories$]).pipe(
    map(([filters, cats]): BreadcrumbItem[] => {
      const crumbs: BreadcrumbItem[] = [{ label: 'Home', link: '/' }];
      if (filters.search) {
        crumbs.push({ label: `Search: "${filters.search}"` });
      } else if (filters.categoryId) {
        crumbs.push({ label: 'Products', link: '/products' });
        const cat = cats.find((c) => c.id === filters.categoryId);
        if (cat) crumbs.push({ label: cat.name });
      } else {
        crumbs.push({ label: 'All Products' });
      }
      return crumbs;
    }),
  );

  readonly activeFilters$ = this.filters$.pipe(
    map((f): ActiveFilter[] => {
      const result: ActiveFilter[] = [];
      if (f.categoryId)       result.push({ key: 'categoryId', label: 'Category',         value: f.categoryId });
      if (f.brandId)          result.push({ key: 'brandId',    label: 'Brand',             value: f.brandId });
      if (f.search)           result.push({ key: 'search',     label: `Search: ${f.search}`, value: f.search });
      if (f.minPrice != null)    result.push({ key: 'minPrice',    label: `Min ₹${f.minPrice}`,        value: String(f.minPrice) });
      if (f.maxPrice != null)    result.push({ key: 'maxPrice',    label: `Max ₹${f.maxPrice}`,        value: String(f.maxPrice) });
      if (f.minDiscount != null) result.push({ key: 'minDiscount', label: `${f.minDiscount}% & above`, value: String(f.minDiscount) });
      return result;
    }),
  );

  ngOnInit(): void {
    this.store.dispatch(CatalogActions.loadCategories());

    this.route.queryParamMap.subscribe((qp) => {
      this.categories$.pipe(
        filter((cats) => cats.length > 0),
        take(1)
      ).subscribe((cats) => {
        const categorySlug = qp.get('category');
        const brandSlug    = qp.get('brand');
        const search       = qp.get('search');
        
        const partial: Partial<ProductFilters> = { page: 1 };
        
        if (categorySlug) {
          const cat = cats.find(c => c.slug === categorySlug || c.id === categorySlug);
          partial.categoryId = cat ? cat.id : null;
        } else {
          partial.categoryId = null;
        }

        if (brandSlug) {
          partial.brandId = brandSlug;
        }

        if (search) {
          partial.search = search;
        } else {
          partial.search = null;
        }

        this.store.dispatch(CatalogActions.setFilters({ filters: partial }));
      });
    });

    this.store.select(selectFilters).subscribe((f) => {
      this.store.dispatch(CatalogActions.loadProducts({ filters: f }));
      if (f.categoryId) {
        this.catalogService.getCategoryAttributes(f.categoryId).subscribe({
          next: (attrs) => this.categoryAttributes.set(attrs),
          error: () => this.categoryAttributes.set([]),
        });
      } else {
        this.categoryAttributes.set([]);
      }
    });
  }

  onFiltersChange(state: FilterState): void {
    this.store.dispatch(CatalogActions.setFilters({
      filters: {
        categoryId:  state.categoryId,
        brandId:     state.selectedBrandIds[0] ?? null,
        minPrice:    state.minPrice,
        maxPrice:    state.maxPrice,
        minDiscount: state.minDiscount,
        page:        1,
      },
    }));
  }

  onSortChange(sort: ProductFilters['sort']): void {
    this.store.dispatch(CatalogActions.setFilters({ filters: { sort, page: 1 } }));
  }

  onPageChange(page: number): void {
    this.store.dispatch(CatalogActions.setFilters({ filters: { page } }));
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  removeFilter(filter: ActiveFilter): void {
    const patch: Partial<ProductFilters> = { [filter.key]: null, page: 1 };
    this.store.dispatch(CatalogActions.setFilters({ filters: patch }));
  }

  clearAllFilters(): void {
    this.store.dispatch(CatalogActions.resetFilters());
  }

  /** ENH-CAT-005 — load the next page in infinite scroll mode (append = true). */
  onLoadMore(): void {
    this.filters$.pipe(take(1)).subscribe((f) => {
      this.store.dispatch(
        CatalogActions.loadProducts({
          filters: { ...f, page: f.page + 1 },
          append:  true,
        }),
      );
      // Also keep filter state in sync so pagination controls reflect correct page
      this.store.dispatch(CatalogActions.setFilters({ filters: { page: f.page + 1 } }));
    });
  }
}
