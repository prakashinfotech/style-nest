export interface CartItem {
  id: string;
  productId: string;
  variantId: string | null;
  name: string;
  imageUrl: string;
  price: number;
  salePrice: number | null;
  quantity: number;
  size: string | null;
  colour: string | null;
}

export interface Cart {
  items: CartItem[];
  subtotal: number;
  discount: number;
  total: number;
  couponCode: string | null;
}
