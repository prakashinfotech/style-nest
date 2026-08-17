import { createReducer, on } from '@ngrx/store';
import { SnackbarType, UiActions } from './ui.actions';

export interface UiState {
  isLoading:     boolean;
  snackbar: {
    visible:      boolean;
    message:      string;
    type:         SnackbarType;
  };
  openModalId:   string | null;
  authModalMode: 'login' | 'register';
  mobileNavOpen: boolean;
}

export const initialUiState: UiState = {
  isLoading: false,
  snackbar: {
    visible: false,
    message: '',
    type:    'info',
  },
  openModalId:   null,
  authModalMode: 'login',
  mobileNavOpen: false,
};

export const uiReducer = createReducer(
  initialUiState,

  on(UiActions.setLoading, (state, { loading }) => ({
    ...state, isLoading: loading,
  })),

  on(UiActions.showSnackbar, (state, { message, snackbarType }) => ({
    ...state,
    snackbar: { visible: true, message, type: snackbarType },
  })),

  on(UiActions.hideSnackbar, (state) => ({
    ...state,
    snackbar: { ...state.snackbar, visible: false },
  })),

  on(UiActions.openModal, (state, { modalId }) => ({
    ...state, openModalId: modalId,
  })),

  on(UiActions.closeModal, (state) => ({
    ...state, openModalId: null,
  })),

  on(UiActions.openMobileNav, (state) => ({ ...state, mobileNavOpen: true })),
  on(UiActions.closeMobileNav, (state) => ({ ...state, mobileNavOpen: false })),

  on(UiActions.openAuthModal, (state, { mode }) => ({
    ...state,
    openModalId:   'auth',
    authModalMode: mode,
  })),
);
