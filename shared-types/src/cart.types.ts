export interface CartItem {
  id: string;
  productId: string;
  productName: string;
  productImageUrl?: string;
  brandName: string;
  variantId: string;
  size: string;
  colour?: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface Cart {
  id: string;
  userId: string;
  items: CartItem[];
  subtotal: number;
  discountAmount: number;
  total: number;
  couponCode?: string;
}

export interface AddCartItemRequest {
  productId: string;
  variantId: string;
  quantity: number;
}

export interface ApplyCouponRequest {
  code: string;
}
