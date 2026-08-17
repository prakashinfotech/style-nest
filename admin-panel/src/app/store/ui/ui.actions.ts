import { createAction, props } from '@ngrx/store';

export const toggleSidebar = createAction('[UI] Toggle Sidebar');
export const collapseSidebar = createAction('[UI] Collapse Sidebar');
export const expandSidebar = createAction('[UI] Expand Sidebar');

export const showToast = createAction(
  '[UI] Show Toast',
  props<{ message: string; toastType: 'success' | 'error' | 'info' }>()
);

export const clearToast = createAction('[UI] Clear Toast');
