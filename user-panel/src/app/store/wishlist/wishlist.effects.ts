import { inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { catchError, exhaustMap, map, of, switchMap, withLatestFrom } from 'rxjs';
import { UserService } from '../../core/services/user.service';
import { WishlistActions } from './wishlist.actions';
import { selectWishlistIds } from './wishlist.selectors';

export const toggleWishlistEffect = createEffect(
  (actions$ = inject(Actions), store = inject(Store)) =>
    actions$.pipe(
      ofType(WishlistActions.toggle),
      withLatestFrom(store.select(selectWishlistIds)),
      map(([{ productId }, ids]) =>
        ids.includes(productId)
          ? WishlistActions.remove({ productId })
          : WishlistActions.add({ productId })
      ),
    ),
  { functional: true },
);

export const loadWishlistEffect = createEffect(
  (actions$ = inject(Actions), userService = inject(UserService)) =>
    actions$.pipe(
      ofType(WishlistActions.load),
      switchMap(() =>
        userService.getWishlist().pipe(
          map((productIds) => WishlistActions.loadSuccess({ productIds })),
          catchError((err: unknown) =>
            of(WishlistActions.loadFailure({ error: extractMessage(err) }))
          ),
        )
      ),
    ),
  { functional: true },
);

export const addToWishlistEffect = createEffect(
  (actions$ = inject(Actions), userService = inject(UserService)) =>
    actions$.pipe(
      ofType(WishlistActions.add),
      exhaustMap(({ productId }) =>
        userService.addToWishlist(productId).pipe(
          map(() => WishlistActions.addSuccess({ productId })),
          catchError((err: unknown) =>
            of(WishlistActions.addFailure({ error: extractMessage(err) }))
          ),
        )
      ),
    ),
  { functional: true },
);

export const removeFromWishlistEffect = createEffect(
  (actions$ = inject(Actions), userService = inject(UserService)) =>
    actions$.pipe(
      ofType(WishlistActions.remove),
      exhaustMap(({ productId }) =>
        userService.removeFromWishlist(productId).pipe(
          map(() => WishlistActions.removeSuccess({ productId })),
          catchError((err: unknown) =>
            of(WishlistActions.removeFailure({ error: extractMessage(err) }))
          ),
        )
      ),
    ),
  { functional: true },
);

function extractMessage(err: unknown): string {
  if (err instanceof Error) return err.message;
  return 'Wishlist error';
}
