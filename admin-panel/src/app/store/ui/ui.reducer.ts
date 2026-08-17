import { createReducer, on } from '@ngrx/store';
import { initialUiState } from './ui.state';
import * as UiActions from './ui.actions';

export const uiReducer = createReducer(
  initialUiState,
  on(UiActions.toggleSidebar, (s) => ({ ...s, sidebarCollapsed: !s.sidebarCollapsed })),
  on(UiActions.collapseSidebar, (s) => ({ ...s, sidebarCollapsed: true })),
  on(UiActions.expandSidebar, (s) => ({ ...s, sidebarCollapsed: false })),
  on(UiActions.showToast, (s, { message, toastType }) => ({ ...s, toastMessage: message, toastType })),
  on(UiActions.clearToast, (s) => ({ ...s, toastMessage: null, toastType: null }))
);
