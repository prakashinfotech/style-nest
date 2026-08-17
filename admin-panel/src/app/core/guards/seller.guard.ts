import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { map, take } from 'rxjs';
import { selectIsSeller } from '../../store/auth/auth.selectors';

export const sellerGuard: CanActivateFn = () => {
  const store  = inject(Store);
  const router = inject(Router);

  return store.select(selectIsSeller).pipe(
    take(1),
    map((isSeller) => isSeller || router.createUrlTree(['/login']))
  );
};
