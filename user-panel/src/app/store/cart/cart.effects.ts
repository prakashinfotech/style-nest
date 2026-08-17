import { HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, exhaustMap, map, of, switchMap, tap } from 'rxjs';
import { CartService } from '../../core/services/cart.service';
import { CartActions } from './cart.actions';
import { UiActions } from '../ui/ui.actions';
import { Store } from '@ngrx/store';

export const loadCartEffect = createEffect(
  (actions$ = inject(Actions), cartService = inject(CartService)) =>
    actions$.pipe(
      ofType(CartActions.loadCart),
      switchMap(() =>
        cartService.getCart().pipe(
          map((cart) => CartActions.loadCartSuccess({ cart })),
          catchError((err: unknown) => of(CartActions.loadCartFailure({ error: extractMessage(err) }))),
        )
      ),
    ),
  { functional: true },
);

export const addItemEffect = createEffect(
  (actions$ = inject(Actions), cartService = inject(CartService)) =>
    actions$.pipe(
      ofType(CartActions.addItem),
      exhaustMap(({ productId, size, colour, quantity }) =>
        cartService.addItem(productId, size, colour, quantity).pipe(
          map((item) => CartActions.addItemSuccess({ item })),
          catchError((err: unknown) => of(CartActions.addItemFailure({ error: extractMessage(err) }))),
        )
      ),
    ),
  { functional: true },
);

/** Show "Added to bag" snackbar on successful cart add */
export const addItemSuccessToastEffect = createEffect(
  (actions$ = inject(Actions), store = inject(Store)) =>
    actions$.pipe(
      ofType(CartActions.addItemSuccess),
      map(() =>
        UiActions.showSnackbar({ message: 'Added to bag ✓', snackbarType: 'success' }),
      ),
    ),
  { functional: true },
);

export const addItemFailureToastEffect = createEffect(
  (actions$ = inject(Actions)) =>
    actions$.pipe(
      ofType(CartActions.addItemFailure),
      map(({ error }) =>
        UiActions.showSnackbar({ message: error || 'Failed to add item to bag.', snackbarType: 'error' }),
      ),
    ),
  { functional: true },
);

export const updateItemFailureToastEffect = createEffect(
  (actions$ = inject(Actions)) =>
    actions$.pipe(
      ofType(CartActions.updateItemFailure),
      map(({ error }) =>
        UiActions.showSnackbar({ message: error || 'Failed to update quantity. Changes reverted.', snackbarType: 'error' }),
      ),
    ),
  { functional: true },
);

export const removeItemFailureToastEffect = createEffect(
  (actions$ = inject(Actions)) =>
    actions$.pipe(
      ofType(CartActions.removeItemFailure),
      map(({ error }) =>
        UiActions.showSnackbar({ message: error || 'Failed to remove item. Changes reverted.', snackbarType: 'error' }),
      ),
    ),
  { functional: true },
);

export const updateItemEffect = createEffect(
  (actions$ = inject(Actions), cartService = inject(CartService)) =>
    actions$.pipe(
      ofType(CartActions.updateItem),
      exhaustMap(({ itemId, quantity }) =>
        cartService.updateItem(itemId, quantity).pipe(
          map((item) => CartActions.updateItemSuccess({ item })),
          catchError((err: unknown) => of(CartActions.updateItemFailure({ error: extractMessage(err) }))),
        )
      ),
    ),
  { functional: true },
);

export const removeItemEffect = createEffect(
  (actions$ = inject(Actions), cartService = inject(CartService)) =>
    actions$.pipe(
      ofType(CartActions.removeItem),
      exhaustMap(({ itemId }) =>
        cartService.removeItem(itemId).pipe(
          map(() => CartActions.removeItemSuccess({ itemId })),
          catchError((err: unknown) => of(CartActions.removeItemFailure({ error: extractMessage(err) }))),
        )
      ),
    ),
  { functional: true },
);

export const applyCouponEffect = createEffect(
  (actions$ = inject(Actions), cartService = inject(CartService)) =>
    actions$.pipe(
      ofType(CartActions.applyCoupon),
      exhaustMap(({ couponCode }) =>
        cartService.applyCoupon(couponCode).pipe(
          map((cart) => CartActions.applyCouponSuccess({ cart })),
          catchError((err: unknown) => {
            const { error, errorCode } = extractCouponError(err);
            return of(CartActions.applyCouponFailure({ error, errorCode }));
          }),
        )
      ),
    ),
  { functional: true },
);

function extractMessage(err: unknown): string {
  if (err instanceof Error) return err.message;
  return 'An unexpected error occurred';
}

function extractCouponError(err: unknown): { error: string; errorCode?: string } {
  if (err instanceof HttpErrorResponse) {
    const body = err.error as Record<string, unknown> | null;
    const errorCode = typeof body?.['errorCode'] === 'string' ? body['errorCode'] : undefined;
    const message   = typeof body?.['message']   === 'string' ? body['message']   : extractMessage(err);
    return { error: message, errorCode };
  }
  return { error: extractMessage(err) };
}
