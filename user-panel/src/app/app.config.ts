import { ApplicationConfig, provideBrowserGlobalErrorListeners, isDevMode, APP_INITIALIZER } from '@angular/core';
import { provideRouter, withComponentInputBinding, withViewTransitions } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideStore } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { provideStoreDevtools } from '@ngrx/store-devtools';

import { routes } from './app.routes';
import { authReducer } from './store/auth/auth.reducer';
import { cartReducer } from './store/cart/cart.reducer';
import { catalogReducer } from './store/catalog/catalog.reducer';
import { uiReducer } from './store/ui/ui.reducer';
import { wishlistReducer } from './store/wishlist/wishlist.reducer';
import { orderReducer } from './store/order/order.reducer';
import * as authEffects from './store/auth/auth.effects';
import * as cartEffects from './store/cart/cart.effects';
import * as catalogEffects from './store/catalog/catalog.effects';
import * as wishlistEffects from './store/wishlist/wishlist.effects';
import * as orderEffects from './store/order/order.effects';
import * as uiEffects from './store/ui/ui.effects';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { tokenRefreshInterceptor } from './core/interceptors/token-refresh.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { AnalyticsService } from './core/services/analytics.service';
import { WebVitalsService } from './core/services/web-vitals.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding(), withViewTransitions()),
    provideHttpClient(withInterceptors([authInterceptor, tokenRefreshInterceptor, errorInterceptor])),
    provideStore({
      auth:     authReducer,
      cart:     cartReducer,
      catalog:  catalogReducer,
      ui:       uiReducer,
      wishlist: wishlistReducer,
      order:    orderReducer,
    }),
    provideEffects(authEffects, cartEffects, catalogEffects, wishlistEffects, orderEffects, uiEffects),
    provideStoreDevtools({ maxAge: 25, logOnly: !isDevMode() }),
    // ENH-AI-005 — Initialise analytics (GA4 + Meta Pixel + Mixpanel) once the app starts
    {
      provide: APP_INITIALIZER,
      useFactory: (analytics: AnalyticsService) => () => analytics.init(),
      deps: [AnalyticsService],
      multi: true,
    },
    // ENH-INFRA-012 — App Insights RUM + Core Web Vitals (LCP/INP/CLS/TTFB) measurement
    {
      provide: APP_INITIALIZER,
      useFactory: (wv: WebVitalsService) => () => wv.init(),
      deps: [WebVitalsService],
      multi: true,
    },
  ],
};
