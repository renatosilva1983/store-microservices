import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ToastContainer } from './core/notifications/toast-container';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastContainer],
  template: `
    <header class="topbar">
      <a class="brand" routerLink="/">Store</a>
      <nav>
        <a routerLink="/products" routerLinkActive="active">Produtos</a>
      </nav>
    </header>

    <main class="container">
      <router-outlet />
    </main>

    <app-toast-container />
  `,
})
export class App {}
