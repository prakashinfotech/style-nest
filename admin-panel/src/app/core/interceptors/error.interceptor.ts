import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Store } from '@ngrx/store';
import { catchError, throwError } from 'rxjs';
import { showToast } from '../../store/ui/ui.actions';
import { logout } from '../../store/auth/auth.actions';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const store = inject(Store);

  return next(req).pipe(
    catchError((err) => {
      if (err.status === 401) {
        store.dispatch(logout());
      } else if (err.status >= 500) {
        store.dispatch(showToast({ message: 'Server error. Please try again.', toastType: 'error' }));
      }
      return throwError(() => err);
    })
  );
};
