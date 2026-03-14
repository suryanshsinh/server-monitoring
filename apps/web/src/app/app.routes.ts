import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'servers/:id',
    loadComponent: () => import('./components/server-detail/server-detail.component').then(m => m.ServerDetailComponent)
  },
  {
    path: 'manage',
    loadComponent: () => import('./components/server-management/server-management.component').then(m => m.ServerManagementComponent)
  },
  {
    path: '**',
    redirectTo: ''
  }
];
