import { createReducer, on } from '@ngrx/store';
import { WishlistActions } from './wishlist.actions';

export interface WishlistState {
  productIds: string[];
  isLoading:  boolean;
  error:      string | null;
}

export const initialWishlistState: WishlistState = {
  productIds: [],
  isLoading:  false,
  error:      null,
};

export const wishlistReducer = createReducer(
  initialWishlistState,

  on(WishlistActions.load, (state) => ({ ...state, isLoading: true, error: null })),

  on(WishlistActions.loadSuccess, (state, { productIds }) => ({
    ...state, isLoading: false, productIds,
  })),

  on(WishlistActions.loadFailure, (state, { error }) => ({
    ...state, isLoading: false, error,
  })),

  on(WishlistActions.addSuccess, (state, { productId }) => ({
    ...state,
    productIds: state.productIds.includes(productId)
      ? state.productIds
      : [...state.productIds, productId],
  })),

  on(WishlistActions.removeSuccess, (state, { productId }) => ({
    ...state,
    productIds: state.productIds.filter((id) => id !== productId),
  })),
);
