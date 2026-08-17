import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { map, take } from 'rxjs';
import { selectIsSuperAdmin } from '../../store/auth/auth.selectors';

export const superAdminGuard: CanActivateFn = () => {
  const store  = inject(Store);
  const router = inject(Router);

  return store.select(selectIsSuperAdmin).pipe(
    take(1),
    map((isSuperAdmin) => isSuperAdmin || router.createUrlTree(['/dashboard']))
  );
};
