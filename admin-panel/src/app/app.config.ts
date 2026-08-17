import { ApplicationConfig, inject, provideAppInitializer, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideStore, Store } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { provideStoreDevtools } from '@ngrx/store-devtools';
import { provideAnimations } from '@angular/platform-browser/animations';
import { routes } from './app.routes';
import { authReducer } from './store/auth/auth.reducer';
import { uiReducer } from './store/ui/ui.reducer';
import { AuthEffects } from './store/auth/auth.effects';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { restoreSession } from './store/auth/auth.actions';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideStore({ auth: authReducer, ui: uiReducer }),
    provideEffects([AuthEffects]),
    provideStoreDevtools({ maxAge: 25 }),
    provideAppInitializer(() => {
      const store = inject(Store);
      const token      = localStorage.getItem('admin_token');
      const refreshToken = localStorage.getItem('admin_refresh_token');
      const userStr    = localStorage.getItem('admin_user');
      if (token && userStr) {
        try {
          const user = JSON.parse(userStr) as { id: string; email: string; firstName: string; lastName: string; roles: string[] };
          store.dispatch(restoreSession({ user, token, refreshToken: refreshToken ?? '' }));
        } catch {
          localStorage.removeItem('admin_token');
          localStorage.removeItem('admin_refresh_token');
          localStorage.removeItem('admin_user');
        }
      }
    }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    provideAnimations(),
  ],
};
