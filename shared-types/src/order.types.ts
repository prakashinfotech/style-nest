export type OrderStatus =
  | 'Placed'
  | 'Confirmed'
  | 'Shipped'
  | 'Delivered'
  | 'Cancelled'
  | 'ReturnRequested'
  | 'Returned';

export interface OrderItem {
  id: string;
  productId: string;
  productName: string;
  brandName: string;
  size: string;
  colour?: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  imageUrl?: string;
}

export interface OrderStatusHistory {
  status: OrderStatus;
  changedAt: Date;
  note?: string;
}

export interface Order {
  id: string;
  orderNumber: string;
  userId: string;
  status: OrderStatus;
  subtotal: number;
  discountAmount: number;
  total: number;
  couponCode?: string;
  shippingAddress: string;
  placedAt: Date;
  items: OrderItem[];
  statusHistory: OrderStatusHistory[];
}

export interface PlaceOrderRequest {
  shippingAddressId: string;
  paymentMethod: string;
  couponCode?: string;
}
