export interface Category {
  id: string;
  name: string;
  slug: string;
  description?: string;
  imageUrl?: string;
  parentId?: string;
  children?: Category[];
}

export interface Brand {
  id: string;
  name: string;
  slug: string;
  logoUrl?: string;
  description?: string;
}

export interface ProductVariant {
  id: string;
  productId: string;
  size: string;
  colour?: string;
  sku: string;
  stockQuantity: number;
  priceOverride?: number;
}

export interface ProductImage {
  id: string;
  url: string;
  altText?: string;
  displayOrder: number;
}

export interface ProductAttribute {
  attributeId: string;
  attributeName: string;
  displayName: string;
  value: string;
}

export interface Product {
  id: string;
  name: string;
  slug: string;
  description: string;
  price: number;
  discountPercent: number;
  categoryId: string;
  categoryName: string;
  brandId: string;
  brandName: string;
  rating: number;
  reviewCount: number;
  isActive: boolean;
  images: ProductImage[];
  variants: ProductVariant[];
  attributes: ProductAttribute[];
}

export interface AttributeDefinition {
  id: string;
  name: string;
  displayName: string;
  dataType: 'Text' | 'Number' | 'Boolean' | 'Select';
  isFilterable: boolean;
  isRequired: boolean;
  allowedValues?: string[];
}

export interface Review {
  id: string;
  productId: string;
  userId: string;
  authorName: string;
  rating: number;
  title: string;
  body: string;
  createdAt: Date;
}
