import { createReducer, on } from '@ngrx/store';
import { Cart, CartItem } from '../../core/models/cart.model';
import { CartActions } from './cart.actions';

const SAVED_KEY = 'sn_saved_for_later';

function loadSaved(): CartItem[] {
  try {
    return JSON.parse(sessionStorage.getItem(SAVED_KEY) ?? '[]') as CartItem[];
  } catch {
    return [];
  }
}

function persistSaved(items: CartItem[]): void {
  sessionStorage.setItem(SAVED_KEY, JSON.stringify(items));
}

export interface CartState {
  cart: Cart | null;
  optimisticSnapshot: Cart | null;
  isLoading: boolean;
  error: string | null;
  couponStatus: 'idle' | 'success' | 'error';
  couponMessage: string | null;
  savedForLater: CartItem[];
}

export const initialCartState: CartState = {
  cart:               null,
  optimisticSnapshot: null,
  isLoading:          false,
  error:              null,
  couponStatus:       'idle',
  couponMessage:      null,
  savedForLater:      loadSaved(),
};

const emptyCart: Cart = {
  items:      [],
  subtotal:   0,
  discount:   0,
  total:      0,
  couponCode: null,
};

export const cartReducer = createReducer(
  initialCartState,

  on(CartActions.loadCart, CartActions.addItem, CartActions.applyCoupon,
    (state) => ({ ...state, isLoading: true, error: null })),

  // Optimistic: apply quantity change immediately; save snapshot for rollback
  on(CartActions.updateItem, (state, { itemId, quantity }) => {
    if (!state.cart) return { ...state, error: null };
    const items = state.cart.items.map((i) => (i.id === itemId ? { ...i, quantity } : i));
    return {
      ...state,
      error:              null,
      optimisticSnapshot: state.cart,
      cart:               recalculate({ ...state.cart, items }),
    };
  }),

  // Optimistic: remove item immediately; save snapshot for rollback
  on(CartActions.removeItem, (state, { itemId }) => {
    if (!state.cart) return { ...state, error: null };
    const items = state.cart.items.filter((i) => i.id !== itemId);
    return {
      ...state,
      error:              null,
      optimisticSnapshot: state.cart,
      cart:               recalculate({ ...state.cart, items }),
    };
  }),

  on(CartActions.loadCartSuccess, (state, { cart }) => ({
    ...state, isLoading: false, cart,
  })),

  on(CartActions.applyCouponSuccess, (state, { cart }) => ({
    ...state, isLoading: false, cart,
    couponStatus: 'success' as const,
    couponMessage: cart.couponCode ? `Coupon "${cart.couponCode}" applied! You save ₹${cart.discount.toFixed(0)}` : 'Coupon applied!',
  })),

  on(CartActions.addItemSuccess, (state, { item }) => {
    const cart = state.cart ?? { ...emptyCart };
    const existing = cart.items.find((i) => i.id === item.id);
    const items = existing
      ? cart.items.map((i) => (i.id === item.id ? item : i))
      : [...cart.items, item];
    return { ...state, isLoading: false, cart: recalculate({ ...cart, items }) };
  }),

  // Reconcile with server truth; clear snapshot
  on(CartActions.updateItemSuccess, (state, { item }) => {
    const cart = state.cart ?? { ...emptyCart };
    const items = cart.items.map((i) => (i.id === item.id ? item : i));
    return { ...state, isLoading: false, optimisticSnapshot: null, cart: recalculate({ ...cart, items }) };
  }),

  // Item already removed optimistically; just clear snapshot
  on(CartActions.removeItemSuccess, (state) => ({
    ...state, isLoading: false, optimisticSnapshot: null,
  })),

  on(CartActions.loadCartFailure, CartActions.addItemFailure,
    (state, { error }) => ({ ...state, isLoading: false, error })),

  // Rollback optimistic update on failure
  on(CartActions.updateItemFailure, (state, { error }) => ({
    ...state,
    isLoading:          false,
    error,
    cart:               state.optimisticSnapshot ?? state.cart,
    optimisticSnapshot: null,
  })),

  // Rollback optimistic remove on failure
  on(CartActions.removeItemFailure, (state, { error }) => ({
    ...state,
    isLoading:          false,
    error,
    cart:               state.optimisticSnapshot ?? state.cart,
    optimisticSnapshot: null,
  })),

  on(CartActions.applyCouponFailure, (state, { error, errorCode }) => ({
    ...state, isLoading: false, error,
    couponStatus: 'error' as const,
    couponMessage: mapCouponErrorCode(error, errorCode),
  })),

  on(CartActions.saveForLater, (state, { itemId }) => {
    const cart = state.cart ?? { ...emptyCart };
    const item = cart.items.find((i) => i.id === itemId);
    if (!item) return state;
    const items = cart.items.filter((i) => i.id !== itemId);
    const savedForLater = [item, ...state.savedForLater.filter((s) => s.id !== itemId)];
    persistSaved(savedForLater);
    return { ...state, cart: recalculate({ ...cart, items }), savedForLater };
  }),

  on(CartActions.moveToCart, (state, { item }) => {
    const cart = state.cart ?? { ...emptyCart };
    const savedForLater = state.savedForLater.filter((s) => s.id !== item.id);
    const existing = cart.items.find((i) => i.id === item.id);
    const items = existing
      ? cart.items.map((i) => (i.id === item.id ? { ...i, quantity: i.quantity + 1 } : i))
      : [...cart.items, item];
    persistSaved(savedForLater);
    return { ...state, cart: recalculate({ ...cart, items }), savedForLater };
  }),

  on(CartActions.removeSaved, (state, { itemId }) => {
    const savedForLater = state.savedForLater.filter((s) => s.id !== itemId);
    persistSaved(savedForLater);
    return { ...state, savedForLater };
  }),

  on(CartActions.clearCart, (state) => ({ ...state, cart: { ...emptyCart } })),
);

function recalculate(cart: Cart): Cart {
  const subtotal = cart.items.reduce(
    (sum, i) => sum + (i.salePrice ?? i.price) * i.quantity, 0,
  );
  return { ...cart, subtotal, total: subtotal - cart.discount };
}

function mapCouponErrorCode(serverMessage: string, errorCode?: string): string {
  switch (errorCode) {
    case 'COUPON_NOT_FOUND':           return 'Coupon code not found. Please check and try again.';
    case 'COUPON_INACTIVE':            return 'This coupon is no longer active.';
    case 'COUPON_EXPIRED':             return 'This coupon has expired.';
    case 'COUPON_NOT_YET_VALID':       return 'This coupon is not yet valid.';
    case 'COUPON_USAGE_LIMIT_REACHED': return 'This coupon has reached its usage limit.';
    case 'COUPON_USER_LIMIT_REACHED':  return 'You have already used this coupon the maximum number of times.';
    case 'COUPON_MIN_ORDER_NOT_MET':   return serverMessage; // Server includes the ₹ amount
    default:                           return 'Invalid or expired coupon code.';
  }
}
