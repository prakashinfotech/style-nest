import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Store } from '@ngrx/store';
import {
  BehaviorSubject,
  Observable,
  catchError,
  filter,
  switchMap,
  take,
  throwError,
} from 'rxjs';
import { AuthService } from '../services/auth.service';
import { AuthActions } from '../../store/auth/auth.actions';
import { selectRefreshToken } from '../../store/auth/auth.selectors';

// Module-level state shared across all interceptor invocations — enables single-flight behaviour.
let isRefreshing = false;
const refreshSubject$ = new BehaviorSubject<string | null>(null);

export const tokenRefreshInterceptor: HttpInterceptorFn = (req, next) => {
  const store = inject(Store);
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !req.url.includes('/auth/refresh')) {
        return handleUnauthorized(req, next, store, authService);
      }
      return throwError(() => error);
    }),
  );
};

function handleUnauthorized(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  store: Store,
  authService: AuthService,
): Observable<HttpEvent<unknown>> {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshSubject$.next(null);

    return store.select(selectRefreshToken).pipe(
      take(1),
      switchMap((refreshToken) => {
        if (!refreshToken) {
          isRefreshing = false;
          store.dispatch(AuthActions.logout());
          return throwError(() => new Error('No refresh token available'));
        }

        return authService.refreshToken(refreshToken).pipe(
          switchMap((tokens) => {
            isRefreshing = false;
            refreshSubject$.next(tokens.accessToken);
            store.dispatch(AuthActions.refreshTokenSuccess({ tokens }));
            return next(withBearer(req, tokens.accessToken));
          }),
          catchError((err) => {
            isRefreshing = false;
            refreshSubject$.next(null);
            store.dispatch(AuthActions.logout());
            return throwError(() => err);
          }),
        );
      }),
    );
  }

  // Another request already triggered a refresh — wait for the new token then retry.
  return refreshSubject$.pipe(
    filter((token): token is string => token !== null),
    take(1),
    switchMap((token) => next(withBearer(req, token))),
  );
}

function withBearer(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}
