import { createFeatureSelector, createSelector } from '@ngrx/store';
import { AuthState } from './auth.reducer';

export const selectAuthState = createFeatureSelector<AuthState>('auth');

export const selectCurrentUser   = createSelector(selectAuthState, (s) => s.user);
export const selectRefreshToken  = createSelector(selectAuthState, (s) => s.refreshToken);
export const selectAccessToken   = createSelector(selectAuthState, (s) => s.accessToken);
export const selectIsLoggedIn    = createSelector(selectAuthState, (s) => s.user !== null && s.accessToken !== null);
export const selectAuthLoading   = createSelector(selectAuthState, (s) => s.isLoading);
export const selectAuthError     = createSelector(selectAuthState, (s) => s.error);
export const selectIsAdmin       = createSelector(selectCurrentUser, (u) => u?.roles.includes('Admin') ?? false);
export const selectIsSeller      = createSelector(selectCurrentUser, (u) => u?.roles.includes('Seller') ?? false);
/** ENH-AUTH-001 — pending account merge token for Facebook login */
export const selectMergeToken    = createSelector(selectAuthState, (s) => s.mergeToken);
