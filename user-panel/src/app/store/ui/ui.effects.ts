import { inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { delay, map } from 'rxjs/operators';
import { UiActions } from './ui.actions';

export const autoHideSnackbar$ = createEffect(
  () => {
    const actions$ = inject(Actions);
    return actions$.pipe(
      ofType(UiActions.showSnackbar),
      delay(5000),
      map(() => UiActions.hideSnackbar()),
    );
  },
  { functional: true },
);
