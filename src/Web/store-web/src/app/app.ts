import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth/auth.service';
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
      <div class="session">
        @if (auth.user(); as user) {
          <span>{{ user.name }}</span>
          <button type="button" class="link" (click)="logout()">Sair</button>
        } @else {
          <a routerLink="/login">Entrar</a>
        }
      </div>
    </header>

    <main class="container">
      <router-outlet />
    </main>

    <app-toast-container />
  `,
})
export class App {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected logout(): void {
    this.auth.logout();
    this.router.navigate(['/products']);
  }
}
