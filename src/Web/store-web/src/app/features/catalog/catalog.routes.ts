import { Routes } from '@angular/router';
import { ProductsFacade } from './data/products.facade';

export const CATALOG_ROUTES: Routes = [
  {
    path: '',
    // O Facade vive enquanto o usuário estiver na área de produtos.
    providers: [ProductsFacade],
    children: [
      {
        path: '',
        loadComponent: () => import('./pages/product-list/product-list').then((m) => m.ProductList),
        title: 'Produtos',
      },
    ],
  },
];
