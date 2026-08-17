import { describe, it, expect } from 'vitest';
import { catalogReducer, initialCatalogState } from './catalog.reducer';
import { CatalogActions } from './catalog.actions';
import { Product } from '../../core/models/product.model';
import { Review } from '../../core/models/review.model';

// ── Helpers ──────────────────────────────────────────────────────────────────

function makeProduct(overrides: Partial<Product> = {}): Product {
  return {
    id:           'prod-1',
    name:         'Test Product',
    slug:         'test-product',
    description:  'A test product',
    price:        1000,
    salePrice:    null,
    brandId:      'brand-1',
    brandName:    'Test Brand',
    categoryId:   'cat-1',
    categoryName: 'Test Category',
    imageUrls:    ['https://example.com/img.jpg'],
    variants:     [],
    rating:       4.2,
    reviewCount:  10,
    inStock:      true,
    ...overrides,
  };
}

function makeReview(overrides: Partial<Review> = {}): Review {
  return {
    id:        'rev-1',
    productId: 'prod-1',
    userId:    'user-1',
    author:    'Alice',
    rating:    5,
    title:     'Great product',
    body:      'Really loved it',
    date:      '2026-05-15T00:00:00Z',
    ...overrides,
  };
}

// ── loadProductSuccess ────────────────────────────────────────────────────────

describe('catalogReducer — loadProductSuccess', () => {
  it('sets selectedProduct and clears isLoadingProduct', () => {
    const product = makeProduct();
    const state = catalogReducer(
      { ...initialCatalogState, isLoadingProduct: true },
      CatalogActions.loadProductSuccess({ product }),
    );

    expect(state.selectedProduct).toEqual(product);
    expect(state.isLoadingProduct).toBe(false);
  });

  it('populates productCache keyed by product id', () => {
    const product = makeProduct({ id: 'abc-123' });
    const state = catalogReducer(
      initialCatalogState,
      CatalogActions.loadProductSuccess({ product }),
    );

    expect(state.productCache['abc-123']).toEqual(product);
  });

  it('prepends to recentlyViewed and caps at 6', () => {
    const existing = Array.from({ length: 6 }, (_, i) =>
      makeProduct({ id: `old-${i}`, name: `Old ${i}` }),
    );
    const newProduct = makeProduct({ id: 'new-1', name: 'New Product' });

    const state = catalogReducer(
      { ...initialCatalogState, recentlyViewed: existing },
      CatalogActions.loadProductSuccess({ product: newProduct }),
    );

    expect(state.recentlyViewed).toHaveLength(6);
    expect(state.recentlyViewed[0].id).toBe('new-1');
    // oldest entry dropped
    expect(state.recentlyViewed.map((p) => p.id)).not.toContain('old-5');
  });

  it('deduplicates recentlyViewed when same product viewed again', () => {
    const product = makeProduct({ id: 'dup-1' });
    const stateAfterFirst = catalogReducer(
      initialCatalogState,
      CatalogActions.loadProductSuccess({ product }),
    );
    const stateAfterSecond = catalogReducer(
      stateAfterFirst,
      CatalogActions.loadProductSuccess({ product }),
    );

    expect(stateAfterSecond.recentlyViewed.filter((p) => p.id === 'dup-1')).toHaveLength(1);
  });
});

// ── loadProductFailure ────────────────────────────────────────────────────────

describe('catalogReducer — loadProductFailure', () => {
  it('sets pdpError and clears isLoadingProduct', () => {
    const state = catalogReducer(
      { ...initialCatalogState, isLoadingProduct: true },
      CatalogActions.loadProductFailure({ error: 'Not found', pdpError: 'Product not found' }),
    );

    expect(state.isLoadingProduct).toBe(false);
    expect(state.pdpError).toBe('Product not found');
  });

  it('does not affect selectedProduct', () => {
    const product = makeProduct();
    const state = catalogReducer(
      { ...initialCatalogState, selectedProduct: product },
      CatalogActions.loadProductFailure({ error: 'err', pdpError: 'err' }),
    );

    expect(state.selectedProduct).toEqual(product);
  });
});

// ── clearSelectedProduct ──────────────────────────────────────────────────────

describe('catalogReducer — clearSelectedProduct', () => {
  it('nulls selectedProduct and clears relatedProducts and pdpError', () => {
    const product = makeProduct();
    const state = catalogReducer(
      {
        ...initialCatalogState,
        selectedProduct: product,
        relatedProducts: [product],
        pdpError: 'some error',
      },
      CatalogActions.clearSelectedProduct(),
    );

    expect(state.selectedProduct).toBeNull();
    expect(state.relatedProducts).toHaveLength(0);
    expect(state.pdpError).toBeNull();
  });
});

// ── postReviewSuccess ─────────────────────────────────────────────────────────

describe('catalogReducer — postReviewSuccess', () => {
  it('prepends new review and increments reviewsTotalCount', () => {
    const existing = makeReview({ id: 'rev-old' });
    const newReview = makeReview({ id: 'rev-new', title: 'New review' });

    const state = catalogReducer(
      { ...initialCatalogState, reviews: [existing], reviewsTotalCount: 1 },
      CatalogActions.postReviewSuccess({ review: newReview }),
    );

    expect(state.reviews[0].id).toBe('rev-new');
    expect(state.reviews).toHaveLength(2);
    expect(state.reviewsTotalCount).toBe(2);
    expect(state.postingReview).toBe(false);
  });
});

// ── clearReviews ──────────────────────────────────────────────────────────────

describe('catalogReducer — clearReviews', () => {
  it('resets all review state fields to initial values', () => {
    const review = makeReview();
    const state = catalogReducer(
      {
        ...initialCatalogState,
        reviews:           [review],
        reviewsTotalCount: 5,
        reviewsPage:       3,
        reviewsLoading:    true,
        reviewsError:      'some error',
        postingReview:     true,
        postReviewError:   'post error',
      },
      CatalogActions.clearReviews(),
    );

    expect(state.reviews).toHaveLength(0);
    expect(state.reviewsTotalCount).toBe(0);
    expect(state.reviewsPage).toBe(1);
    expect(state.reviewsLoading).toBe(false);
    expect(state.reviewsError).toBeNull();
    expect(state.postingReview).toBe(false);
    expect(state.postReviewError).toBeNull();
  });
});

// ── loadProductsSuccess ───────────────────────────────────────────────────────

describe('catalogReducer — loadProductsSuccess', () => {
  it('populates products and totalCount, clears isLoadingProducts', () => {
    const p1 = makeProduct({ id: 'p1' });
    const p2 = makeProduct({ id: 'p2' });

    const state = catalogReducer(
      { ...initialCatalogState, isLoadingProducts: true },
      CatalogActions.loadProductsSuccess({
        result: { items: [p1, p2], totalCount: 2, page: 1, pageSize: 24 },
      }),
    );

    expect(state.products).toHaveLength(2);
    expect(state.totalCount).toBe(2);
    expect(state.isLoadingProducts).toBe(false);
  });

  it('replaces previous product list on new page load', () => {
    const old   = makeProduct({ id: 'old' });
    const fresh = makeProduct({ id: 'fresh' });

    const stateWithOld = catalogReducer(
      initialCatalogState,
      CatalogActions.loadProductsSuccess({ result: { items: [old], totalCount: 1, page: 1, pageSize: 24 } }),
    );
    const state = catalogReducer(
      stateWithOld,
      CatalogActions.loadProductsSuccess({ result: { items: [fresh], totalCount: 1, page: 2, pageSize: 24 } }),
    );

    expect(state.products).toHaveLength(1);
    expect(state.products[0].id).toBe('fresh');
  });
});

// ── setFilters / resetFilters ─────────────────────────────────────────────────

describe('catalogReducer — setFilters', () => {
  it('merges partial filters into existing filters', () => {
    const state = catalogReducer(
      initialCatalogState,
      CatalogActions.setFilters({ filters: { categoryId: 'cat-1', page: 2 } }),
    );

    expect(state.filters.categoryId).toBe('cat-1');
    expect(state.filters.page).toBe(2);
    expect(state.filters.pageSize).toBe(24); // untouched
  });

  it('does not overwrite unspecified filter fields', () => {
    const stateWithBrand = catalogReducer(
      initialCatalogState,
      CatalogActions.setFilters({ filters: { brandId: 'brand-x' } }),
    );
    const state = catalogReducer(
      stateWithBrand,
      CatalogActions.setFilters({ filters: { categoryId: 'cat-y' } }),
    );

    expect(state.filters.brandId).toBe('brand-x');
    expect(state.filters.categoryId).toBe('cat-y');
  });
});

describe('catalogReducer — resetFilters', () => {
  it('resets all filters back to defaults', () => {
    const stateWithFilters = catalogReducer(
      initialCatalogState,
      CatalogActions.setFilters({ filters: { categoryId: 'cat-1', brandId: 'b-1', page: 5, sort: 'newest' } }),
    );

    const state = catalogReducer(stateWithFilters, CatalogActions.resetFilters());

    expect(state.filters.categoryId).toBeNull();
    expect(state.filters.brandId).toBeNull();
    expect(state.filters.page).toBe(1);
    expect(state.filters.sort).toBeNull();
  });
});

// ── loadRelatedProductsSuccess ────────────────────────────────────────────────

describe('catalogReducer — loadRelatedProductsSuccess', () => {
  it('populates relatedProducts', () => {
    const related = [makeProduct({ id: 'rel-1' }), makeProduct({ id: 'rel-2' })];

    const state = catalogReducer(
      initialCatalogState,
      CatalogActions.loadRelatedProductsSuccess({ products: related }),
    );

    expect(state.relatedProducts).toHaveLength(2);
    expect(state.relatedProducts[0].id).toBe('rel-1');
  });

  it('clears relatedProducts on loadRelatedProducts (in-flight reset)', () => {
    const stateWithRelated = catalogReducer(
      initialCatalogState,
      CatalogActions.loadRelatedProductsSuccess({
        products: [makeProduct({ id: 'old-rel' })],
      }),
    );

    const state = catalogReducer(stateWithRelated, CatalogActions.loadRelatedProducts({ id: 'prod-2' }));

    expect(state.relatedProducts).toHaveLength(0);
  });
});

// ── loadReviewsSuccess (append) ───────────────────────────────────────────────

describe('catalogReducer — loadReviewsSuccess', () => {
  it('replaces reviews when append=false', () => {
    const old = makeReview({ id: 'old' });
    const fresh = makeReview({ id: 'fresh' });

    const state = catalogReducer(
      { ...initialCatalogState, reviews: [old] },
      CatalogActions.loadReviewsSuccess({
        result: { items: [fresh], totalCount: 1, page: 1, pageSize: 10 },
        append: false,
      }),
    );

    expect(state.reviews).toHaveLength(1);
    expect(state.reviews[0].id).toBe('fresh');
  });

  it('appends reviews when append=true', () => {
    const page1 = makeReview({ id: 'p1' });
    const page2 = makeReview({ id: 'p2' });

    const state = catalogReducer(
      { ...initialCatalogState, reviews: [page1] },
      CatalogActions.loadReviewsSuccess({
        result: { items: [page2], totalCount: 2, page: 2, pageSize: 10 },
        append: true,
      }),
    );

    expect(state.reviews).toHaveLength(2);
    expect(state.reviews[1].id).toBe('p2');
  });
});
