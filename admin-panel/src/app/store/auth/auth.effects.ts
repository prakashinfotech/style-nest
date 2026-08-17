import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, exhaustMap, map, of, tap } from 'rxjs';
import { AuthApiService } from '../../core/services/auth-api.service';
import * as AuthActions from './auth.actions';

@Injectable()
export class AuthEffects {
  private actions$ = inject(Actions);
  private authApi  = inject(AuthApiService);
  private router   = inject(Router);

  login$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.login),
      exhaustMap(({ email, password }) =>
        this.authApi.login(email, password).pipe(
          map((res) => {
            // Backend returns { action: 'MFA_REQUIRED', mfaToken } for Admin/SuperAdmin
            if (res.action === 'MFA_REQUIRED' && res.mfaToken) {
              return AuthActions.mfaRequired({ mfaToken: res.mfaToken });
            }
            const payload = this.authApi.parseToken(res.accessToken!);
            const user = {
              id: payload.sub,
              email: payload.email,
              firstName: payload.given_name,
              lastName: payload.family_name,
              roles: payload.roles ?? [],
            };
            localStorage.setItem('admin_token', res.accessToken!);
            localStorage.setItem('admin_refresh_token', res.refreshToken!);
            localStorage.setItem('admin_user', JSON.stringify(user));
            return AuthActions.loginSuccess({ user, token: res.accessToken!, refreshToken: res.refreshToken! });
          }),
          catchError((err) =>
            of(AuthActions.loginFailure({ error: err?.error?.message ?? 'Login failed' }))
          )
        )
      )
    )
  );

  verifyMfa$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.verifyMfa),
      exhaustMap(({ mfaToken, otpCode }) =>
        this.authApi.verifyMfa(mfaToken, otpCode).pipe(
          map((res) => {
            const payload = this.authApi.parseToken(res.accessToken!);
            const user = {
              id: payload.sub,
              email: payload.email,
              firstName: payload.given_name,
              lastName: payload.family_name,
              roles: payload.roles ?? [],
            };
            localStorage.setItem('admin_token', res.accessToken!);
            localStorage.setItem('admin_refresh_token', res.refreshToken!);
            localStorage.setItem('admin_user', JSON.stringify(user));
            return AuthActions.loginSuccess({ user, token: res.accessToken!, refreshToken: res.refreshToken! });
          }),
          catchError((err) =>
            of(AuthActions.loginFailure({ error: err?.error?.message ?? err?.error?.Message ?? 'Invalid OTP code' }))
          )
        )
      )
    )
  );

  loginSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.loginSuccess),
        tap(({ user }) => {
          if (user.roles.includes('SuperAdmin') || user.roles.includes('Admin')) {
            this.router.navigate(['/dashboard']);
          } else if (user.roles.includes('Seller')) {
            this.router.navigate(['/seller/dashboard']);
          } else {
            this.router.navigate(['/login']);
          }
        })
      ),
    { dispatch: false }
  );

  restoreSession$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.restoreSession),
        tap(({ user }) => {
          if (user.roles.includes('SuperAdmin') || user.roles.includes('Admin')) {
            this.router.navigate(['/dashboard']);
          } else if (user.roles.includes('Seller')) {
            this.router.navigate(['/seller/dashboard']);
          }
        })
      ),
    { dispatch: false }
  );

  logout$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.logout),
        tap(() => {
          localStorage.removeItem('admin_token');
          localStorage.removeItem('admin_refresh_token');
          localStorage.removeItem('admin_user');
          this.router.navigate(['/login']);
        })
      ),
    { dispatch: false }
  );
}
