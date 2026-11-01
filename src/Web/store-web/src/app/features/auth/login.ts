import { Component, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { NotificationService } from '../../core/notifications/notification.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  /** Vem da query string (?returnUrl=...) graças ao withComponentInputBinding(). */
  readonly returnUrl = input<string>('/products');

  protected readonly loading = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['admin@store.dev', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password } = this.form.getRawValue();
    this.loading.set(true);
    this.auth.login(email, password).subscribe({
      next: (response) => {
        this.notifications.success(`Bem-vindo, ${response.user.name}.`);
        this.router.navigateByUrl(this.returnUrl() || '/products');
      },
      error: () => this.loading.set(false),
    });
  }
}
