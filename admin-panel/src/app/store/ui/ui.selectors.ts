import { createFeatureSelector, createSelector } from '@ngrx/store';
import { UiState } from './ui.state';

export const selectUiState        = createFeatureSelector<UiState>('ui');
export const selectSidebarCollapsed = createSelector(selectUiState, (s) => s.sidebarCollapsed);
export const selectToast            = createSelector(selectUiState, (s) => ({ message: s.toastMessage, type: s.toastType }));
