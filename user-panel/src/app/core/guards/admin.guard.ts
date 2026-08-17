import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { map, take } from 'rxjs';
import { selectIsAdmin, selectIsLoggedIn } from '../../store/auth/auth.selectors';
import { combineLatest } from 'rxjs';

export const adminGuard: CanActivateFn = () => {
  const store  = inject(Store);
  const router = inject(Router);

  return combineLatest([
    store.select(selectIsLoggedIn),
    store.select(selectIsAdmin),
  ]).pipe(
    take(1),
    map(([isLoggedIn, isAdmin]) => {
      if (!isLoggedIn) return router.createUrlTree(['/auth/login']);
      if (!isAdmin)   return router.createUrlTree(['/']);
      return true;
    }),
  );
};
