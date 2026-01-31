import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/features',
    pathMatch: 'full'
  },
  {
    path: 'features',
    loadComponent: () => import('./FeatureList/feature-list-component/feature-list-component.component').then(m => m.FeatureListComponent)
  }
];
