import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: '/inicio', pathMatch: 'full' },
  { path: 'inicio', loadComponent: () => import('./pages/home/home').then((m) => m.Home) },
  { path: 'escritos-rojos', loadComponent: () => import('./pages/escritos-rojos/escritos-rojos').then((m) => m.EscritosRojos) },
  { path: '**', redirectTo: '/inicio' },
];
