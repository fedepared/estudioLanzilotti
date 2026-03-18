import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: '/inicio', pathMatch: 'full' },
  { path: 'inicio', loadComponent: () => import('../app/pages/home/home').then((m) => m.Home) },
  {
    path: 'dashboard',
    loadComponent: () => import('../app/pages/dashboard/dashboard').then((m) => m.Dashboard),
  },
  { path: '**', redirectTo: '/inicio' },
];
