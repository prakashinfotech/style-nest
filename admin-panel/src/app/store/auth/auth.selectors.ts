import { createFeatureSelector, createSelector } from '@ngrx/store';
import { AuthState } from './auth.state';

export const selectAuthState = createFeatureSelector<AuthState>('auth');

export const selectCurrentUser  = createSelector(selectAuthState, (s) => s.user);
export const selectToken        = createSelector(selectAuthState, (s) => s.token);
export const selectAuthLoading  = createSelector(selectAuthState, (s) => s.loading);
export const selectAuthError    = createSelector(selectAuthState, (s) => s.error);
export const selectIsLoggedIn   = createSelector(selectAuthState, (s) => !!s.user && !!s.token);
export const selectUserRoles    = createSelector(selectCurrentUser, (u) => u?.roles ?? []);
export const selectIsSuperAdmin = createSelector(selectUserRoles, (r) => r.includes('SuperAdmin'));
export const selectIsAdmin      = createSelector(selectUserRoles, (r) => r.includes('Admin') || r.includes('SuperAdmin'));
export const selectIsSeller     = createSelector(selectUserRoles, (r) => r.includes('Seller'));
export const selectMfaStep      = createSelector(selectAuthState, (s) => s.mfaStep);
export const selectMfaToken     = createSelector(selectAuthState, (s) => s.mfaToken);
