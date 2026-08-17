import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Store } from '@ngrx/store';
import { catchError, throwError } from 'rxjs';
import { UiActions } from '../../store/ui/ui.actions';

function getErrorMessage(error: HttpErrorResponse): string {
  if (error.status === 0)   return 'Service unavailable. Please check your connection and try again.';
  if (error.status === 503) return 'Service temporarily unavailable. Please try again later.';
  if (error.status === 409) return (error.error as { message?: string } | null)?.message
                               ?? 'A conflict occurred. Please refresh and try again.';
  if (error.status === 403) return 'You do not have permission to perform this action.';
  if (error.status === 404) return 'The requested resource was not found.';
  if (error.status >= 500)  return 'A server error occurred. Please try again later.';
  const detail = (error.error as { detail?: unknown; message?: unknown } | null)?.detail
              ?? (error.error as { detail?: unknown; message?: unknown } | null)?.message;
  if (detail && typeof detail === 'string') return detail;
  return 'An unexpected error occurred. Please try again.';
}

// 401 handling is owned by tokenRefreshInterceptor — this interceptor only shows toasts.
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const store = inject(Store);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        store.dispatch(UiActions.showSnackbar({
          message: getErrorMessage(error),
          snackbarType: 'error',
        }));
      }
      return throwError(() => error);
    }),
  );
};
