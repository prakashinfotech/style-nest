import { createFeatureSelector, createSelector } from '@ngrx/store';
import { CartState } from './cart.reducer';

export const selectCartState   = createFeatureSelector<CartState>('cart');

export const selectCart        = createSelector(selectCartState, (s) => s.cart);
export const selectCartItems   = createSelector(selectCart, (c) => c?.items ?? []);
export const selectCartTotal   = createSelector(selectCart, (c) => c?.total ?? 0);
export const selectCartCount   = createSelector(selectCartItems, (items) =>
  items.reduce((sum, i) => sum + i.quantity, 0),
);
export const selectCartLoading    = createSelector(selectCartState, (s) => s.isLoading);
export const selectCartError      = createSelector(selectCartState, (s) => s.error);
export const selectCouponStatus   = createSelector(selectCartState, (s) => s.couponStatus);
export const selectCouponMessage  = createSelector(selectCartState, (s) => s.couponMessage);
export const selectSavedForLater  = createSelector(selectCartState, (s) => s.savedForLater);
