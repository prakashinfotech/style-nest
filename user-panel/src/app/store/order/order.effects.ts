import { HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { catchError, exhaustMap, map, of, tap, withLatestFrom } from 'rxjs';
import { OrderService } from '../../core/services/order.service';
import { CartActions } from '../cart/cart.actions';
import { selectCart } from '../cart/cart.selectors';
import { OosItem, OrderActions } from './order.actions';

export const buyNowEffect = createEffect(
  (actions$ = inject(Actions), orderService = inject(OrderService)) =>
    actions$.pipe(
      ofType(OrderActions.buyNow),
      exhaustMap(({ productId, size, colour, quantity }) =>
        orderService.buyNow(productId, size, colour, quantity).pipe(
          map((order) => OrderActions.buyNowSuccess({
            orderId:     order.id,
            orderNumber: order.orderNumber,
          })),
          catchError((err: unknown) => {
            const { error, outOfStockItems } = extractOrderError(err);
            return of(OrderActions.buyNowFailure({ error, outOfStockItems }));
          }),
        )
      ),
    ),
  { functional: true },
);

export const buyNowSuccessRedirectEffect = createEffect(
  (actions$ = inject(Actions), router = inject(Router)) =>
    actions$.pipe(
      ofType(OrderActions.buyNowSuccess),
      tap(({ orderNumber }) =>
        router.navigate(['/order-confirmed'], { queryParams: { orderNumber } })
      ),
    ),
  { functional: true, dispatch: false },
);

export const placeOrderEffect = createEffect(
  (actions$ = inject(Actions), orderService = inject(OrderService), store = inject(Store)) =>
    actions$.pipe(
      ofType(OrderActions.placeOrder),
      withLatestFrom(store.select(selectCart)),
      exhaustMap(([payload, cart]) =>
        orderService.placeOrder({
          addressLine1:  payload.addressLine1,
          addressLine2:  payload.addressLine2,
          city:          payload.city,
          state:         payload.state,
          pincode:       payload.pincode,
          paymentMethod: payload.paymentMethod,
          couponCode:    cart?.couponCode ?? null,
        }).pipe(
          map((order) => OrderActions.placeOrderSuccess({
            orderId:     order.id,
            orderNumber: order.orderNumber,
          })),
          catchError((err: unknown) => {
            const { error, errorCode, outOfStockItems } = extractOrderError(err);
            return of(OrderActions.placeOrderFailure({ error, errorCode, outOfStockItems }));
          }),
        )
      ),
    ),
  { functional: true },
);

export const placeOrderSuccessClearCartEffect = createEffect(
  (actions$ = inject(Actions)) =>
    actions$.pipe(
      ofType(OrderActions.placeOrderSuccess),
      map(() => CartActions.clearCart()),
    ),
  { functional: true },
);

export const placeOrderSuccessRedirectEffect = createEffect(
  (actions$ = inject(Actions), router = inject(Router)) =>
    actions$.pipe(
      ofType(OrderActions.placeOrderSuccess),
      tap(({ orderNumber }) =>
        router.navigate(['/order-confirmed'], { queryParams: { orderNumber } })
      ),
    ),
  { functional: true, dispatch: false },
);

function extractMessage(err: unknown): string {
  if (err instanceof Error) return err.message;
  if (typeof err === 'object' && err !== null && 'error' in err) {
    const e = (err as { error: unknown }).error;
    if (typeof e === 'object' && e !== null && 'message' in e) {
      const msg = (e as Record<string, unknown>)['message'];
      if (typeof msg === 'string') return msg;
    }
  }
  return 'Failed to place order';
}

function extractOrderError(err: unknown): { error: string; errorCode?: string; outOfStockItems?: OosItem[] } {
  if (err instanceof HttpErrorResponse) {
    const body         = err.error as Record<string, unknown> | null;
    const errorCode    = typeof body?.['errorCode']   === 'string' ? body['errorCode']   : undefined;
    const message      = typeof body?.['message']     === 'string' ? body['message']     : extractMessage(err);
    const outOfStockItems = Array.isArray(body?.['outOfStockItems'])
      ? (body['outOfStockItems'] as OosItem[])
      : undefined;
    return { error: message, errorCode, outOfStockItems };
  }
  return { error: extractMessage(err) };
}
