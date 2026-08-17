import { inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { catchError, exhaustMap, map, of, switchMap, withLatestFrom } from 'rxjs';
import { CatalogService } from '../../core/services/catalog.service';
import { CatalogActions } from './catalog.actions';
import { selectProductCache, selectReviewsPage } from './catalog.selectors';

export const loadProductsEffect = createEffect(
  (actions$ = inject(Actions), catalogService = inject(CatalogService)) =>
    actions$.pipe(
      ofType(CatalogActions.loadProducts),
      switchMap(({ filters, append }) =>
        catalogService.getProducts(filters).pipe(
          // ENH-CAT-005: pass append flag through to success so reducer can accumulate
          map((result) => CatalogActions.loadProductsSuccess({ result, append: append ?? false })),
          catchError((err: unknown) =>
            of(CatalogActions.loadProductsFailure({ error: extractMessage(err) }))
          ),
        )
      ),
    ),
  { functional: true },
);

export const loadProductEffect = createEffect(
  (actions$ = inject(Actions), catalogService = inject(CatalogService), store = inject(Store)) =>
    actions$.pipe(
      ofType(CatalogActions.loadProduct),
      withLatestFrom(store.select(selectProductCache)),
      switchMap(([{ id }, cache]) => {
        // Cache hit — skip HTTP call
        if (cache[id]) {
          return of(CatalogActions.loadProductSuccess({ product: cache[id] }));
        }
        return catalogService.getProduct(id).pipe(
          map((product) => CatalogActions.loadProductSuccess({ product })),
          catchError((err: unknown) => {
            const msg = extractMessage(err);
            return of(CatalogActions.loadProductFailure({ error: msg, pdpError: msg }));
          }),
        );
      }),
    ),
  { functional: true },
);

export const loadCategoriesEffect = createEffect(
  (actions$ = inject(Actions), catalogService = inject(CatalogService)) =>
    actions$.pipe(
      ofType(CatalogActions.loadCategories),
      exhaustMap(() =>
        catalogService.getCategories().pipe(
          map((categories) => CatalogActions.loadCategoriesSuccess({ categories })),
          catchError((err: unknown) =>
            of(CatalogActions.loadCategoriesFailure({ error: extractMessage(err) }))
          ),
        )
      ),
    ),
  { functional: true },
);

/**
 * Stub effect — triggers related-products load after a product is successfully loaded.
 * Full implementation in PDP-6 (backend endpoint + real HTTP call).
 */
export const loadRelatedProductsEffect = createEffect(
  (actions$ = inject(Actions)) =>
    actions$.pipe(
      ofType(CatalogActions.loadProductSuccess),
      map(({ product }) => CatalogActions.loadRelatedProducts({ id: product.id })),
    ),
  { functional: true },
);

export const fetchRelatedProductsEffect = createEffect(
  (actions$ = inject(Actions), catalogService = inject(CatalogService)) =>
    actions$.pipe(
      ofType(CatalogActions.loadRelatedProducts),
      switchMap(({ id }) =>
        catalogService.getRelatedProducts(id).pipe(
          map((products) => CatalogActions.loadRelatedProductsSuccess({ products })),
          catchError((err: unknown) =>
            of(CatalogActions.loadRelatedProductsFailure({ error: extractMessage(err) })),
          ),
        ),
      ),
    ),
  { functional: true },
);

// ── Reviews ──────────────────────────────────────────────────────────────────

/**
 * Trigger initial reviews load (page 1) whenever a product is successfully loaded.
 * Clears any stale reviews from the previous PDP visit first.
 */
export const triggerReviewsOnProductLoadEffect = createEffect(
  (actions$ = inject(Actions)) =>
    actions$.pipe(
      ofType(CatalogActions.loadProductSuccess),
      map(({ product }) =>
        CatalogActions.loadReviews({ productId: product.id, page: 1 }),
      ),
    ),
  { functional: true },
);

export const loadReviewsEffect = createEffect(
  (actions$ = inject(Actions), catalogService = inject(CatalogService), store = inject(Store)) =>
    actions$.pipe(
      ofType(CatalogActions.loadReviews),
      withLatestFrom(store.select(selectReviewsPage)),
      switchMap(([{ productId, page }]) => {
        const append = page > 1;
        return catalogService.getReviews(productId, page).pipe(
          map((result) => CatalogActions.loadReviewsSuccess({ result, append })),
          catchError((err: unknown) =>
            of(CatalogActions.loadReviewsFailure({ error: extractMessage(err) })),
          ),
        );
      }),
    ),
  { functional: true },
);

export const postReviewEffect = createEffect(
  (actions$ = inject(Actions), catalogService = inject(CatalogService)) =>
    actions$.pipe(
      ofType(CatalogActions.postReview),
      exhaustMap(({ productId, request }) =>
        catalogService.postReview(productId, request).pipe(
          map((review) => CatalogActions.postReviewSuccess({ review })),
          catchError((err: unknown) =>
            of(CatalogActions.postReviewFailure({ error: extractMessage(err) })),
          ),
        ),
      ),
    ),
  { functional: true },
);

function extractMessage(err: unknown): string {
  if (err instanceof Error) return err.message;
  return 'An unexpected error occurred';
}
