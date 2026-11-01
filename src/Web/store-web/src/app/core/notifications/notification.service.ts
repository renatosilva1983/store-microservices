import { Injectable, signal } from '@angular/core';

export type NotificationType = 'success' | 'error' | 'info';

export interface Notification {
  id: number;
  type: NotificationType;
  message: string;
}

/**
 * Estado global de mensagens com signals: qualquer parte da aplicação publica,
 * o ToastContainer exibe. Sem RxJS, sem zone.js.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private static readonly DurationMs = 4000;
  private nextId = 1;

  private readonly _notifications = signal<Notification[]>([]);
  readonly notifications = this._notifications.asReadonly();

  success(message: string): void {
    this.show('success', message);
  }

  error(message: string): void {
    this.show('error', message);
  }

  info(message: string): void {
    this.show('info', message);
  }

  dismiss(id: number): void {
    this._notifications.update((list) => list.filter((n) => n.id !== id));
  }

  private show(type: NotificationType, message: string): void {
    const id = this.nextId++;
    this._notifications.update((list) => [...list, { id, type, message }]);
    setTimeout(() => this.dismiss(id), NotificationService.DurationMs);
  }
}
