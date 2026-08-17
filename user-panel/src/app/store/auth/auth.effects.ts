import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { catchError, exhaustMap, map, of, switchMap, tap, withLatestFrom } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { AuthActions } from './auth.actions';
import { selectRefreshToken } from './auth.selectors';
import { UiActions } from '../ui/ui.actions';

// Note: token refresh is handled by tokenRefreshInterceptor (single-flight, ENH-AUTH-008).

export const loginEffect = createEffect(
  (actions$ = inject(Actions), authService = inject(AuthService)) =>
    actions$.pipe(
      ofType(AuthActions.login),
      exhaustMap(({ email, password }) =>
        authService.login(email, password).pipe(
          map(({ user, tokens }) => AuthActions.loginSuccess({ user, tokens })),
          catchError((err: unknown) =>
            of(AuthActions.loginFailure({ error: extractErrorMessage(err) }))
          ),
        )
      ),
    ),
  { functional: true },
);

export const registerEffect = createEffect(
  (actions$ = inject(Actions), authService = inject(AuthService)) =>
    actions$.pipe(
      ofType(AuthActions.register),
      exhaustMap(({ firstName, lastName, email, password }) =>
        authService.register(firstName, lastName, email, password).pipe(
          map(({ user, tokens }) => AuthActions.registerSuccess({ user, tokens })),
          catchError((err: unknown) =>
            of(AuthActions.registerFailure({ error: extractErrorMessage(err) }))
          ),
        )
      ),
    ),
  { functional: true },
);

export const loginSuccessRedirectEffect = createEffect(
  (actions$ = inject(Actions), router = inject(Router)) =>
    actions$.pipe(
      ofType(AuthActions.loginSuccess, AuthActions.registerSuccess),
      tap(({ user }) => {
        if (user.roles.includes('Admin')) {
          router.navigate(['/admin']);
        } else if (user.roles.includes('Seller')) {
          router.navigate(['/seller']);
        } else if (router.url.startsWith('/auth/')) {
          // Page-based auth flow: redirect home after login
          router.navigate(['/']);
        }
        // Modal flow: stay on current page — modal closes via closeAuthModalEffect
      }),
    ),
  { functional: true, dispatch: false },
);

export const closeAuthModalEffect = createEffect(
  (actions$ = inject(Actions)) =>
    actions$.pipe(
      ofType(AuthActions.loginSuccess, AuthActions.registerSuccess),
      map(() => UiActions.closeModal()),
    ),
  { functional: true },
);

export const logoutEffect = createEffect(
  (actions$ = inject(Actions), authService = inject(AuthService), store = inject(Store)) =>
    actions$.pipe(
      ofType(AuthActions.logout),
      withLatestFrom(store.select(selectRefreshToken)),
      exhaustMap(([_, refreshToken]) => {
        if (!refreshToken) return of(AuthActions.logoutSuccess());
        return authService.logout(refreshToken).pipe(
          map(() => AuthActions.logoutSuccess()),
          catchError(() => of(AuthActions.logoutSuccess())),
        )
      }),
    ),
  { functional: true },
);

export const logoutRedirectEffect = createEffect(
  (actions$ = inject(Actions), router = inject(Router)) =>
    actions$.pipe(
      ofType(AuthActions.logoutSuccess),
      tap(() => router.navigate(['/'])),
    ),
  { functional: true, dispatch: false },
);

// ── ENH-AUTH-002 — Apple Sign-In ─────────────────────────────────────────────

/** Fetches the Apple authorization URL from the backend then navigates the browser to it. */
export const appleLoginEffect = createEffect(
  (actions$ = inject(Actions), authService = inject(AuthService)) =>
    actions$.pipe(
      ofType(AuthActions.appleLogin),
      switchMap(() => {
        const redirectUri = `${window.location.origin}/auth/apple-callback`;
        return authService.getAppleLoginUrl(redirectUri).pipe(
          map((url) => { window.location.href = url; return AuthActions.clearError(); }),
          catchError((err: unknown) =>
            of(AuthActions.loginFailure({ error: extractErrorMessage(err) })),
          ),
        );
      }),
    ),
  { functional: true },
);

/** Validates the Apple id_token; dispatches loginSuccess or facebookMergeRequired. */
export const appleCallbackEffect = createEffect(
  (actions$ = inject(Actions), authService = inject(AuthService)) =>
    actions$.pipe(
      ofType(AuthActions.appleCallback),
      switchMap(({ idToken }) =>
        authService.appleCallback(idToken).pipe(
          map((result) => {
            if (result.action === 'NEW_ACCOUNT' && result.authResult) {
              return AuthActions.loginSuccess({
                user:   result.authResult.user,
                tokens: result.authResult.tokens,
              });
            }
            return AuthActions.facebookMergeRequired({
              mergeToken: result.mergeToken ?? '',
            });
          }),
          catchError((err: unknown) =>
            of(AuthActions.loginFailure({ error: extractErrorMessage(err) })),
          ),
        ),
      ),
    ),
  { functional: true },
);

// ── ENH-AUTH-001 — Facebook OAuth 2.0 ────────────────────────────────────────

/** Fetches the FB authorization URL from the backend then navigates the browser to it. */
export const facebookLoginEffect = createEffect(
  (actions$ = inject(Actions), authService = inject(AuthService)) =>
    actions$.pipe(
      ofType(AuthActions.facebookLogin),
      switchMap(() => {
        const redirectUri = `${window.location.origin}/auth/facebook-callback`;
        return authService.getFacebookLoginUrl(redirectUri).pipe(
          map((url) => { window.location.href = url; return AuthActions.clearError(); }),
          catchError((err: unknown) =>
            of(AuthActions.loginFailure({ error: extractErrorMessage(err) })),
          ),
        );
      }),
    ),
  { functional: true },
);

/** Exchanges the FB code for tokens; dispatches loginSuccess or facebookMergeRequired. */
export const facebookCallbackEffect = createEffect(
  (actions$ = inject(Actions), authService = inject(AuthService)) =>
    actions$.pipe(
      ofType(AuthActions.facebookCallback),
      switchMap(({ code }) => {
        const redirectUri = `${window.location.origin}/auth/facebook-callback`;
        return authService.facebookCallback(code, redirectUri).pipe(
          map((result) => {
            if (result.action === 'NEW_ACCOUNT' && result.authResult) {
              return AuthActions.loginSuccess({
                user:   result.authResult.user,
                tokens: result.authResult.tokens,
              });
            }
            return AuthActions.facebookMergeRequired({
              mergeToken: result.mergeToken ?? '',
            });
          }),
          catchError((err: unknown) =>
            of(AuthActions.loginFailure({ error: extractErrorMessage(err) })),
          ),
        );
      }),
    ),
  { functional: true },
);

/** Confirms the account merge with the user's password; dispatches loginSuccess on success. */
export const facebookMergeConfirmEffect = createEffect(
  (actions$ = inject(Actions), authService = inject(AuthService)) =>
    actions$.pipe(
      ofType(AuthActions.facebookMergeConfirm),
      exhaustMap(({ mergeToken, password }) =>
        authService.mergeConfirm(mergeToken, password).pipe(
          map(({ user, tokens }) => AuthActions.loginSuccess({ user, tokens })),
          catchError((err: unknown) =>
            of(AuthActions.loginFailure({ error: extractErrorMessage(err) })),
          ),
        ),
      ),
    ),
  { functional: true },
);

function extractErrorMessage(err: unknown): string {
  if (err instanceof Error) return err.message;
  if (typeof err === 'object' && err !== null && 'error' in err) {
    const e = (err as { error: unknown }).error;
    if (typeof e === 'string') return e;
    if (typeof e === 'object' && e !== null && 'message' in e) return String((e as { message: unknown }).message);
  }
  return 'An unexpected error occurred';
}
