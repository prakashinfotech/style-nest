import { createFeatureSelector, createSelector } from '@ngrx/store';
import { WishlistState } from './wishlist.reducer';

export const selectWishlistState = createFeatureSelector<WishlistState>('wishlist');

export const selectWishlistIds      = createSelector(selectWishlistState, (s) => s.productIds);
export const selectWishlistLoading  = createSelector(selectWishlistState, (s) => s.isLoading);

export const selectIsWishlisted = (productId: string) =>
  createSelector(selectWishlistIds, (ids) => ids.includes(productId));
