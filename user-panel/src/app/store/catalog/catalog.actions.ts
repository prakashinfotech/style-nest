import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { Category, PaginatedResult, Product, ProductFilters } from '../../core/models/product.model';
import { CreateReviewRequest, PagedReviews, Review } from '../../core/models/review.model';

export const CatalogActions = createActionGroup({
  source: 'Catalog',
  events: {
    'Load Products':         props<{ filters: ProductFilters; append?: boolean }>(),
    'Load Products Success': props<{ result: PaginatedResult<Product>; append: boolean }>(),
    'Load Products Failure': props<{ error: string }>(),

    'Load Product':         props<{ id: string }>(),
    'Load Product Success': props<{ product: Product }>(),
    'Load Product Failure': props<{ error: string; pdpError: string }>(),

    'Load Categories':         emptyProps(),
    'Load Categories Success': props<{ categories: Category[] }>(),
    'Load Categories Failure': props<{ error: string }>(),

    'Set Filters':   props<{ filters: Partial<ProductFilters> }>(),
    'Reset Filters': emptyProps(),

    'Clear Selected Product': emptyProps(),

    'Load Related Products':         props<{ id: string }>(),
    'Load Related Products Success': props<{ products: Product[] }>(),
    'Load Related Products Failure': props<{ error: string }>(),

    // ── Reviews ────────────────────────────────────────────────────────────
    'Load Reviews':         props<{ productId: string; page: number }>(),
    'Load Reviews Success': props<{ result: PagedReviews; append: boolean }>(),
    'Load Reviews Failure': props<{ error: string }>(),

    'Post Review':         props<{ productId: string; request: CreateReviewRequest }>(),
    'Post Review Success': props<{ review: Review }>(),
    'Post Review Failure': props<{ error: string }>(),

    'Clear Reviews': emptyProps(),

    // ── Recently viewed ────────────────────────────────────────────────────
    'Clear Recently Viewed': emptyProps(),
  },
});
