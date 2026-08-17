import { createAction, props } from '@ngrx/store';
import { AuthUser } from './auth.state';

export const login = createAction(
  '[Auth] Login',
  props<{ email: string; password: string }>()
);

export const loginSuccess = createAction(
  '[Auth] Login Success',
  props<{ user: AuthUser; token: string; refreshToken: string }>()
);

export const loginFailure = createAction(
  '[Auth] Login Failure',
  props<{ error: string }>()
);

export const mfaRequired = createAction(
  '[Auth] MFA Required',
  props<{ mfaToken: string }>()
);

export const verifyMfa = createAction(
  '[Auth] Verify MFA',
  props<{ mfaToken: string; otpCode: string }>()
);

export const resetMfaStep = createAction('[Auth] Reset MFA Step');

export const logout = createAction('[Auth] Logout');

export const restoreSession = createAction(
  '[Auth] Restore Session',
  props<{ user: AuthUser; token: string; refreshToken: string }>()
);
