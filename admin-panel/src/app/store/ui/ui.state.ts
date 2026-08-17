export interface UiState {
  sidebarCollapsed: boolean;
  toastMessage: string | null;
  toastType: 'success' | 'error' | 'info' | null;
}

export const initialUiState: UiState = {
  sidebarCollapsed: false,
  toastMessage: null,
  toastType: null,
};
