import { createReducer, on } from '@ngrx/store';
import { initialAuthState } from './auth.state';
import * as AuthActions from './auth.actions';

export const authReducer = createReducer(
  initialAuthState,

  on(AuthActions.login, (state) => ({ ...state, loading: true, error: null })),

  on(AuthActions.mfaRequired, (state, { mfaToken }) => ({
    ...state,
    loading: false,
    error: null,
    mfaStep: 'mfa' as const,
    mfaToken,
  })),

  on(AuthActions.verifyMfa, (state) => ({ ...state, loading: true, error: null })),

  on(AuthActions.loginSuccess, (state, { user, token, refreshToken }) => ({
    ...state,
    user,
    token,
    refreshToken,
    loading: false,
    error: null,
    mfaStep: 'login' as const,
    mfaToken: null,
  })),

  on(AuthActions.loginFailure, (state, { error }) => ({
    ...state,
    loading: false,
    error,
  })),

  on(AuthActions.resetMfaStep, (state) => ({
    ...state,
    mfaStep: 'login' as const,
    mfaToken: null,
    error: null,
  })),

  on(AuthActions.logout, () => ({
    user: null,
    token: null,
    refreshToken: null,
    loading: false,
    error: null,
    mfaStep: 'login' as const,
    mfaToken: null,
  })),

  on(AuthActions.restoreSession, (state, { user, token, refreshToken }) => ({
    ...state,
    user,
    token,
    refreshToken,
    mfaStep: 'login' as const,
    mfaToken: null,
  }))
);
