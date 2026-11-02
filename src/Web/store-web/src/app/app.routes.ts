import { Routes } from '@angular/router';

// Lazy loading por feature: o código do catálogo só é baixado quando a rota é acessada.
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'products' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login').then((m) => m.Login),
    title: 'Entrar',
  },
  {
    path: 'products',
    loadChildren: () => import('./features/catalog/catalog.routes').then((m) => m.CATALOG_ROUTES),
  },
  { path: '**', redirectTo: 'products' },
];
