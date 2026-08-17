import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { map, take } from 'rxjs';
import { selectIsLoggedIn } from '../../store/auth/auth.selectors';
import { UiActions } from '../../store/ui/ui.actions';

export const authGuard: CanActivateFn = () => {
  const store  = inject(Store);
  const router = inject(Router);

  return store.select(selectIsLoggedIn).pipe(
    take(1),
    map((isLoggedIn) => {
      if (isLoggedIn) return true;
      store.dispatch(UiActions.openAuthModal({ mode: 'login' }));
      return router.createUrlTree(['/']);
    }),
  );
};
