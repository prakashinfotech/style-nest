import { createActionGroup, emptyProps, props } from '@ngrx/store';

export interface OosItem {
  variantId:         string;
  productName:       string;
  variantDetails:    string;
  requestedQuantity: number;
  availableQuantity: number;
}

export const OrderActions = createActionGroup({
  source: 'Order',
  events: {
    'Buy Now':         props<{ productId: string; size: string | null; colour: string | null; quantity: number }>(),
    'Buy Now Success': props<{ orderNumber: string; orderId: string }>(),
    'Buy Now Failure': props<{ error: string; outOfStockItems?: OosItem[] }>(),

    'Place Order': props<{
      addressLine1:  string;
      addressLine2:  string;
      city:          string;
      state:         string;
      pincode:       string;
      paymentMethod: string;
    }>(),
    'Place Order Success': props<{ orderNumber: string; orderId: string }>(),
    'Place Order Failure': props<{ error: string; errorCode?: string; outOfStockItems?: OosItem[] }>(),

    'Clear Oos Items': emptyProps(),
  },
});
