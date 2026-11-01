import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../notifications/notification.service';
import { describeError } from './problem-details';

/**
 * Interceptor funcional: trata erro HTTP num ponto só (ProblemDetails → mensagem).
 * Repassa o erro adiante para que a tela também possa reagir (ex.: marcar campos).
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(NotificationService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        notifications.error(describeError(error));
      }
      return throwError(() => error);
    }),
  );
};
