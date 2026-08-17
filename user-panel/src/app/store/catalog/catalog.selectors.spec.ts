import { describe, it, expect } from 'vitest';
import {
  selectSelectedProduct,
  selectPdpLoading,
  selectPlpLoading,
  selectHasMoreReviews,
  selectSelectedVariant,
  selectRecentlyViewed,
} from './catalog.selectors';
import { initialCatalogState, CatalogState } from './catalog.reducer';
import { Product, ProductVariant } from '../../core/models/product.model';
import { Review } from '../../core/models/review.model';

// ── Helpers ──────────────────────────────────────────────────────────────────

function makeVariant(overrides: Partial<ProductVariant> = {}): ProductVariant {
  return {
    id:            'var-1',
    size:          'M',
    colour:        'Red',
    stockQuantity: 10,
    priceOverride: null,
    ...overrides,
  };
}

function makeProduct(overrides: Partial<Product> = {}): Product {
  return {
    id:           'prod-1',
    name:         'Test Product',
    slug:         'test-product',
    description:  'desc',
    price:        1000,
    salePrice:    null,
    brandId:      'brand-1',
    brandName:    'Brand',
    categoryId:   'cat-1',
    categoryName: 'Category',
    imageUrls:    [],
    variants:     [],
    rating:       4,
    reviewCount:  5,
    inStock:      true,
    ...overrides,
  };
}

function makeReview(overrides: Partial<Review> = {}): Review {
  return {
    id: 'r1', productId: 'p1', userId: 'u1',
    author: 'Bob', rating: 4, title: 'Good', body: 'Nice', date: '2026-01-01',
    ...overrides,
  };
}

function stateWith(overrides: Partial<CatalogState>): { catalog: CatalogState } {
  return { catalog: { ...initialCatalogState, ...overrides } };
}

// ── Basic selectors ───────────────────────────────────────────────────────────

describe('selectSelectedProduct', () => {
  it('returns null from initial state', () => {
    expect(selectSelectedProduct(stateWith({}))).toBeNull();
  });

  it('returns the selected product', () => {
    const product = makeProduct();
    expect(selectSelectedProduct(stateWith({ selectedProduct: product }))).toEqual(product);
  });
});

describe('selectPdpLoading', () => {
  it('returns false from initial state', () => {
    expect(selectPdpLoading(stateWith({}))).toBe(false);
  });

  it('returns true when isLoadingProduct is true', () => {
    expect(selectPdpLoading(stateWith({ isLoadingProduct: true }))).toBe(true);
  });
});

describe('selectPlpLoading', () => {
  it('returns false from initial state', () => {
    expect(selectPlpLoading(stateWith({}))).toBe(false);
  });

  it('returns true when isLoadingProducts is true', () => {
    expect(selectPlpLoading(stateWith({ isLoadingProducts: true }))).toBe(true);
  });
});

// ── selectHasMoreReviews ──────────────────────────────────────────────────────

describe('selectHasMoreReviews', () => {
  it('returns false when reviews.length === totalCount', () => {
    const reviews = [makeReview()];
    expect(selectHasMoreReviews(stateWith({ reviews, reviewsTotalCount: 1 }))).toBe(false);
  });

  it('returns true when reviews.length < totalCount', () => {
    const reviews = [makeReview()];
    expect(selectHasMoreReviews(stateWith({ reviews, reviewsTotalCount: 5 }))).toBe(true);
  });

  it('returns false when no reviews and totalCount is 0', () => {
    expect(selectHasMoreReviews(stateWith({ reviews: [], reviewsTotalCount: 0 }))).toBe(false);
  });
});

// ── selectSelectedVariant ─────────────────────────────────────────────────────

describe('selectSelectedVariant', () => {
  const variantM_Red  = makeVariant({ id: 'v1', size: 'M',  colour: 'Red',  stockQuantity: 5  });
  const variantL_Blue = makeVariant({ id: 'v2', size: 'L',  colour: 'Blue', stockQuantity: 3  });
  const variantM_Blue = makeVariant({ id: 'v3', size: 'M',  colour: 'Blue', stockQuantity: 0  });

  const product = makeProduct({ variants: [variantM_Red, variantL_Blue, variantM_Blue] });

  it('returns null when no product is selected', () => {
    const selector = selectSelectedVariant('M', 'Red');
    expect(selector(stateWith({ selectedProduct: null }))).toBeNull();
  });

  it('returns matching variant for size + colour', () => {
    const selector = selectSelectedVariant('M', 'Red');
    expect(selector(stateWith({ selectedProduct: product }))).toEqual(variantM_Red);
  });

  it('returns matching variant for different size + colour', () => {
    const selector = selectSelectedVariant('L', 'Blue');
    expect(selector(stateWith({ selectedProduct: product }))).toEqual(variantL_Blue);
  });

  it('returns null when no variant matches', () => {
    const selector = selectSelectedVariant('XL', 'Green');
    expect(selector(stateWith({ selectedProduct: product }))).toBeNull();
  });

  it('matches by size only when colour is null', () => {
    const selector = selectSelectedVariant('M', null);
    // First M variant found
    const result = selector(stateWith({ selectedProduct: product }));
    expect(result?.size).toBe('M');
  });

  it('matches by colour only when size is null', () => {
    const selector = selectSelectedVariant(null, 'Blue');
    const result = selector(stateWith({ selectedProduct: product }));
    expect(result?.colour).toBe('Blue');
  });

  it('returns first variant when both size and colour are null', () => {
    const selector = selectSelectedVariant(null, null);
    const result = selector(stateWith({ selectedProduct: product }));
    expect(result).toEqual(variantM_Red);
  });
});

// ── selectRecentlyViewed ──────────────────────────────────────────────────────

describe('selectRecentlyViewed', () => {
  it('returns empty array from initial state', () => {
    expect(selectRecentlyViewed(stateWith({}))).toHaveLength(0);
  });

  it('returns the recently viewed products', () => {
    const products = [makeProduct({ id: 'a' }), makeProduct({ id: 'b' })];
    expect(selectRecentlyViewed(stateWith({ recentlyViewed: products }))).toHaveLength(2);
  });
});
