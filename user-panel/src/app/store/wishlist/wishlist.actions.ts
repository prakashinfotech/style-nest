import { createActionGroup, emptyProps, props } from '@ngrx/store';

export const WishlistActions = createActionGroup({
  source: 'Wishlist',
  events: {
    'Load':         emptyProps(),
    'Load Success': props<{ productIds: string[] }>(),
    'Load Failure': props<{ error: string }>(),

    'Toggle':       props<{ productId: string }>(),

    'Add':         props<{ productId: string }>(),
    'Add Success': props<{ productId: string }>(),
    'Add Failure': props<{ error: string }>(),

    'Remove':         props<{ productId: string }>(),
    'Remove Success': props<{ productId: string }>(),
    'Remove Failure': props<{ error: string }>(),
  },
});
