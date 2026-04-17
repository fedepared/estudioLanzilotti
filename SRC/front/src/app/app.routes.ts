import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: '/inicio', pathMatch: 'full' },
  { path: 'inicio', loadComponent: () => import('../app/pages/home/home').then((m) => m.Home) },
 
  { path: '**', redirectTo: '/inicio' },
];
