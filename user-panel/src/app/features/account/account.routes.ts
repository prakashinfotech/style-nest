import { Routes } from '@angular/router';

export const accountRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./profile.component').then((m) => m.ProfileComponent),
  },
  {
    path: 'wallet',
    loadComponent: () => import('./wallet.component').then((m) => m.WalletComponent),
  },
];
