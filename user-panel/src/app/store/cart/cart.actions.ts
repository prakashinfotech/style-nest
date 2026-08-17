import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { Cart, CartItem } from '../../core/models/cart.model';

export const CartActions = createActionGroup({
  source: 'Cart',
  events: {
    'Load Cart':         emptyProps(),
    'Load Cart Success': props<{ cart: Cart }>(),
    'Load Cart Failure': props<{ error: string }>(),

    'Add Item':         props<{ productId: string; size: string | null; colour: string | null; quantity: number }>(),
    'Add Item Success': props<{ item: CartItem }>(),
    'Add Item Failure': props<{ error: string }>(),

    'Update Item':         props<{ itemId: string; quantity: number }>(),
    'Update Item Success': props<{ item: CartItem }>(),
    'Update Item Failure': props<{ error: string }>(),

    'Remove Item':         props<{ itemId: string }>(),
    'Remove Item Success': props<{ itemId: string }>(),
    'Remove Item Failure': props<{ error: string }>(),

    'Apply Coupon':         props<{ couponCode: string }>(),
    'Apply Coupon Success': props<{ cart: Cart }>(),
    'Apply Coupon Failure': props<{ error: string; errorCode?: string }>(),

    'Save For Later': props<{ itemId: string }>(),
    'Move To Cart':   props<{ item: CartItem }>(),
    'Remove Saved':   props<{ itemId: string }>(),

    'Clear Cart': emptyProps(),
  },
});
