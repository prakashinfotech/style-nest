import { createActionGroup, emptyProps, props } from '@ngrx/store';

export type SnackbarType = 'success' | 'error' | 'info' | 'warning';

export const UiActions = createActionGroup({
  source: 'UI',
  events: {
    'Show Snackbar':  props<{ message: string; snackbarType: SnackbarType }>(),
    'Hide Snackbar':  emptyProps(),
    'Open Modal':     props<{ modalId: string }>(),
    'Close Modal':    emptyProps(),
    'Set Loading':    props<{ loading: boolean }>(),
    'Open Mobile Nav':  emptyProps(),
    'Close Mobile Nav': emptyProps(),
    'Open Auth Modal':  props<{ mode: 'login' | 'register' }>(),
  },
});
