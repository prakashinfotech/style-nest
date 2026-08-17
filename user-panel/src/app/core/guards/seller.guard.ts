import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { combineLatest, map, take } from 'rxjs';
import { selectIsLoggedIn, selectIsSeller } from '../../store/auth/auth.selectors';

export const sellerGuard: CanActivateFn = () => {
  const store  = inject(Store);
  const router = inject(Router);

  return combineLatest([
    store.select(selectIsLoggedIn),
    store.select(selectIsSeller),
  ]).pipe(
    take(1),
    map(([isLoggedIn, isSeller]) => {
      if (!isLoggedIn) return router.createUrlTree(['/auth/login']);
      if (!isSeller)   return router.createUrlTree(['/']);
      return true;
    }),
  );
};
