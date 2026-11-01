import { Component, inject } from '@angular/core';
import { NotificationService } from './notification.service';

@Component({
  selector: 'app-toast-container',
  template: `
    <div class="toasts" aria-live="polite">
      @for (notification of service.notifications(); track notification.id) {
        <div [class]="'toast ' + notification.type" role="status">
          <span>{{ notification.message }}</span>
          <button type="button" class="link" (click)="service.dismiss(notification.id)" aria-label="Fechar">×</button>
        </div>
      }
    </div>
  `,
})
export class ToastContainer {
  protected readonly service = inject(NotificationService);
}
