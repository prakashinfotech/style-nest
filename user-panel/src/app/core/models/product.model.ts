export interface ProductVariant {
  id: string;
  size: string;
  colour: string | null;
  stockQuantity: number;
  priceOverride: number | null;
}

export interface Product {
  id: string;
  name: string;
  slug: string;
  description: string;
  price: number;
  salePrice: number | null;
  brandId: string;
  brandName: string;
  categoryId: string;
  categoryName: string;
  imageUrls: string[];
  variants: ProductVariant[];
  rating: number;
  reviewCount: number;
  inStock: boolean;
  /** ENH-PDP-007 — When true, the PDP renders a 360° drag-to-spin image gallery. */
  has360View?: boolean;
}

export interface Category {
  id: string;
  name: string;
  slug: string;
  parentId: string | null;
  imageUrl: string | null;
}

export interface ProductFilters {
  categoryId: string | null;
  brandId: string | null;
  search?: string | null;
  minPrice: number | null;
  maxPrice: number | null;
  minDiscount?: number | null;
  sort: 'price_asc' | 'price_desc' | 'newest' | 'rating' | null;
  page: number;
  pageSize: number;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
