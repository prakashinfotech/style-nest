import { createReducer, on } from '@ngrx/store';
import { Category, Product, ProductFilters } from '../../core/models/product.model';
import { Review } from '../../core/models/review.model';
import { CatalogActions } from './catalog.actions';

export interface CatalogState {
  products:          Product[];
  totalCount:        number;
  selectedProduct:   Product | null;
  categories:        Category[];
  filters:           ProductFilters;
  /** Loading flag for the PLP (product list) */
  isLoadingProducts: boolean;
  /** Loading flag for the PDP (single product) */
  isLoadingProduct:  boolean;
  /** General list/category error */
  error:             string | null;
  /** PDP-specific error — separate from list error */
  pdpError:          string | null;
  /** Per-product cache — keyed by product id */
  productCache:      Record<string, Product>;
  /** Related products for the currently viewed PDP */
  relatedProducts:   Product[];
  // ── Recently viewed ───────────────────────────────────────────────────────
  /** Last 6 products viewed — persisted in sessionStorage via meta-reducer */
  recentlyViewed:    Product[];
  // ── Reviews ──────────────────────────────────────────────────────────────
  reviews:           Review[];
  reviewsTotalCount: number;
  reviewsPage:       number;
  reviewsLoading:    boolean;
  reviewsError:      string | null;
  /** True while a POST /reviews request is in-flight */
  postingReview:     boolean;
  postReviewError:   string | null;
}

const defaultFilters: ProductFilters = {
  categoryId:  null,
  brandId:     null,
  search:      null,
  minPrice:    null,
  maxPrice:    null,
  minDiscount: null,
  sort:        null,
  page:        1,
  pageSize:    24,
};

export const initialCatalogState: CatalogState = {
  products:          [],
  totalCount:        0,
  selectedProduct:   null,
  categories:        [],
  filters:           defaultFilters,
  isLoadingProducts: false,
  isLoadingProduct:  false,
  error:             null,
  pdpError:          null,
  productCache:      {},
  relatedProducts:   [],
  recentlyViewed:    [],
  reviews:           [],
  reviewsTotalCount: 0,
  reviewsPage:       1,
  reviewsLoading:    false,
  reviewsError:      null,
  postingReview:     false,
  postReviewError:   null,
};

export const catalogReducer = createReducer(
  initialCatalogState,

  // ── PLP loading ──────────────────────────────────────────────────────────
  on(CatalogActions.loadProducts, (state) => ({
    ...state, isLoadingProducts: true, error: null,
  })),

  on(CatalogActions.loadProductsSuccess, (state, { result, append }) => ({
    ...state,
    isLoadingProducts: false,
    // ENH-CAT-005: append mode accumulates pages for infinite scroll
    products:   append ? [...state.products, ...result.items] : result.items,
    totalCount: result.totalCount,
  })),

  on(CatalogActions.loadProductsFailure, (state, { error }) => ({
    ...state, isLoadingProducts: false, error,
  })),

  // ── PDP loading ──────────────────────────────────────────────────────────
  on(CatalogActions.loadProduct, (state) => ({
    ...state, isLoadingProduct: true, pdpError: null,
  })),

  on(CatalogActions.loadProductSuccess, (state, { product }) => ({
    ...state,
    isLoadingProduct: false,
    selectedProduct:  product,
    // Populate cache on every successful load
    productCache: { ...state.productCache, [product.id]: product },
    // Prepend to recently viewed (max 6, deduplicated)
    recentlyViewed: [
      product,
      ...state.recentlyViewed.filter((p) => p.id !== product.id),
    ].slice(0, 6),
  })),

  on(CatalogActions.loadProductFailure, (state, { pdpError }) => ({
    ...state, isLoadingProduct: false, pdpError,
  })),

  on(CatalogActions.clearSelectedProduct, (state) => ({
    ...state, selectedProduct: null, relatedProducts: [], pdpError: null,
  })),

  // ── Categories ───────────────────────────────────────────────────────────
  on(CatalogActions.loadCategories, (state) => ({
    ...state, isLoadingProducts: true, error: null,
  })),

  on(CatalogActions.loadCategoriesSuccess, (state, { categories }) => ({
    ...state, isLoadingProducts: false, categories,
  })),

  on(CatalogActions.loadCategoriesFailure, (state, { error }) => ({
    ...state, isLoadingProducts: false, error,
  })),

  // ── Related products ─────────────────────────────────────────────────────
  on(CatalogActions.loadRelatedProducts, (state) => ({
    ...state, relatedProducts: [],
  })),

  on(CatalogActions.loadRelatedProductsSuccess, (state, { products }) => ({
    ...state, relatedProducts: products,
  })),

  on(CatalogActions.loadRelatedProductsFailure, (state) => ({
    ...state, relatedProducts: [],
  })),

  // ── Filters ──────────────────────────────────────────────────────────────
  on(CatalogActions.setFilters, (state, { filters }) => ({
    ...state, filters: { ...state.filters, ...filters },
  })),

  on(CatalogActions.resetFilters, (state) => ({
    ...state, filters: defaultFilters,
  })),

  // ── Reviews ──────────────────────────────────────────────────────────────
  on(CatalogActions.loadReviews, (state) => ({
    ...state, reviewsLoading: true, reviewsError: null,
  })),

  on(CatalogActions.loadReviewsSuccess, (state, { result, append }) => ({
    ...state,
    reviewsLoading:    false,
    reviews:           append ? [...state.reviews, ...result.items] : result.items,
    reviewsTotalCount: result.totalCount,
    reviewsPage:       result.page,
  })),

  on(CatalogActions.loadReviewsFailure, (state, { error }) => ({
    ...state, reviewsLoading: false, reviewsError: error,
  })),

  on(CatalogActions.postReview, (state) => ({
    ...state, postingReview: true, postReviewError: null,
  })),

  on(CatalogActions.postReviewSuccess, (state, { review }) => ({
    ...state,
    postingReview:     false,
    // Prepend the new review so it appears at the top
    reviews:           [review, ...state.reviews],
    reviewsTotalCount: state.reviewsTotalCount + 1,
  })),

  on(CatalogActions.postReviewFailure, (state, { error }) => ({
    ...state, postingReview: false, postReviewError: error,
  })),

  on(CatalogActions.clearReviews, (state) => ({
    ...state,
    reviews:           [],
    reviewsTotalCount: 0,
    reviewsPage:       1,
    reviewsLoading:    false,
    reviewsError:      null,
    postingReview:     false,
    postReviewError:   null,
  })),

  on(CatalogActions.clearRecentlyViewed, (state) => ({
    ...state, recentlyViewed: [],
  })),
);
