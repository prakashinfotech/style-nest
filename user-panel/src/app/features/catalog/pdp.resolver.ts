import { inject } from '@angular/core';
import { ResolveFn, ActivatedRouteSnapshot } from '@angular/router';
import { Store } from '@ngrx/store';
import { filter, first, tap } from 'rxjs';
import { CatalogActions } from '../../store/catalog/catalog.actions';
import { selectSelectedProduct } from '../../store/catalog/catalog.selectors';
import { Product } from '../../core/models/product.model';

/**
 * PDP Resolver — dispatches `loadProduct` and waits for a non-null
 * `selectedProduct` before the route activates.
 * This prevents the PDP skeleton from flashing on first load.
 */
export const pdpResolver: ResolveFn<Product | null> = (route: ActivatedRouteSnapshot) => {
  const store = inject(Store);
  const id    = route.paramMap.get('id') ?? '';

  if (id) {
    store.dispatch(CatalogActions.loadProduct({ id }));
  }

  return store.select(selectSelectedProduct).pipe(
    filter((product): product is Product => product !== null && product.id === id),
    first(),
  );
};
