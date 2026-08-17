import { createFeatureSelector, createSelector } from '@ngrx/store';
import { UiState } from './ui.reducer';

export const selectUiState     = createFeatureSelector<UiState>('ui');

export const selectIsLoading    = createSelector(selectUiState, (s) => s.isLoading);
export const selectSnackbar     = createSelector(selectUiState, (s) => s.snackbar);
export const selectOpenModalId  = createSelector(selectUiState, (s) => s.openModalId);
export const selectAuthModalMode = createSelector(selectUiState, (s) => s.authModalMode);
export const selectMobileNav    = createSelector(selectUiState, (s) => s.mobileNavOpen);
