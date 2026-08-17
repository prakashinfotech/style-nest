import { createFeatureSelector, createSelector } from '@ngrx/store';
import { CatalogState } from './catalog.reducer';

export const selectCatalogState = createFeatureSelector<CatalogState>('catalog');

// ── Products (PLP) ──────────────────────────────────────────────────────────
export const selectProducts        = createSelector(selectCatalogState, (s) => s.products);
export const selectTotalCount      = createSelector(selectCatalogState, (s) => s.totalCount);
export const selectCategories      = createSelector(selectCatalogState, (s) => s.categories);
export const selectFilters         = createSelector(selectCatalogState, (s) => s.filters);
export const selectCatalogError    = createSelector(selectCatalogState, (s) => s.error);

/** PLP-specific loading flag */
export const selectPlpLoading      = createSelector(selectCatalogState, (s) => s.isLoadingProducts);

/** Legacy alias — kept for backward compatibility with existing PLP components */
export const selectCatalogLoading  = selectPlpLoading;

// ── Selected product (PDP) ──────────────────────────────────────────────────
export const selectSelectedProduct = createSelector(selectCatalogState, (s) => s.selectedProduct);
export const selectProductCache    = createSelector(selectCatalogState, (s) => s.productCache);
export const selectPdpError        = createSelector(selectCatalogState, (s) => s.pdpError);

/** PDP-specific loading flag */
export const selectPdpLoading      = createSelector(selectCatalogState, (s) => s.isLoadingProduct);

// ── Related products ────────────────────────────────────────────────────────
export const selectRelatedProducts = createSelector(selectCatalogState, (s) => s.relatedProducts);

// ── Reviews ─────────────────────────────────────────────────────────────────
export const selectReviews           = createSelector(selectCatalogState, (s) => s.reviews);
export const selectReviewsTotalCount = createSelector(selectCatalogState, (s) => s.reviewsTotalCount);
export const selectReviewsPage       = createSelector(selectCatalogState, (s) => s.reviewsPage);
export const selectReviewsLoading    = createSelector(selectCatalogState, (s) => s.reviewsLoading);
export const selectReviewsError      = createSelector(selectCatalogState, (s) => s.reviewsError);
export const selectPostingReview     = createSelector(selectCatalogState, (s) => s.postingReview);
export const selectPostReviewError   = createSelector(selectCatalogState, (s) => s.postReviewError);
/** True when there are more pages of reviews to load */
export const selectHasMoreReviews    = createSelector(
  selectReviewsTotalCount,
  selectReviews,
  (total, reviews) => reviews.length < total,
);

// ── Recently viewed ──────────────────────────────────────────────────────────
export const selectRecentlyViewed = createSelector(selectCatalogState, (s) => s.recentlyViewed);

// ── Computed: selected variant ──────────────────────────────────────────────
/**
 * Returns the matching ProductVariant for the currently selected size + colour.
 * Requires the parent component to pass selectedSize and selectedColour as props.
 */
export const selectSelectedVariant = (selectedSize: string | null, selectedColour: string | null) =>
  createSelector(selectSelectedProduct, (product) => {
    if (!product) return null;
    return (
      product.variants.find(
        (v) =>
          (selectedSize   === null || v.size   === selectedSize) &&
          (selectedColour === null || v.colour === selectedColour),
      ) ?? null
    );
  });
