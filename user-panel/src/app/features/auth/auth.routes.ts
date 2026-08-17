import { Routes } from '@angular/router';

export const authRoutes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () => import('./register.component').then((m) => m.RegisterComponent),
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./forgot-password.component').then((m) => m.ForgotPasswordComponent),
  },
  {
    path: 'verify-otp',
    loadComponent: () => import('./verify-otp.component').then((m) => m.VerifyOtpComponent),
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./reset-password.component').then((m) => m.ResetPasswordComponent),
  },
  {
    // ENH-AUTH-001 — Facebook redirects here with ?code=…&state=…
    path: 'facebook-callback',
    loadComponent: () =>
      import('./facebook-callback.component').then((m) => m.FacebookCallbackComponent),
  },
  {
    // ENH-AUTH-002 — Apple redirects here with #id_token=…&state=… (fragment)
    path: 'apple-callback',
    loadComponent: () =>
      import('./apple-callback.component').then((m) => m.AppleCallbackComponent),
  },
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full',
  },
];
