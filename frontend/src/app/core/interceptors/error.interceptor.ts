import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';

/**
 * Достаёт человекочитаемое сообщение из ответа ASP.NET:
 * - { message: "..." }
 * - ValidationProblemDetails { errors: { Field: ["..."] } }
 */
export function extractErrorMessage(error: unknown, fallback = 'Произошла ошибка. Попробуйте ещё раз.'): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  if (error.status === 0) {
    return 'Нет связи с сервером. Проверьте, запущен ли API Gateway.';
  }

  const body = error.error as
    | { message?: string; errors?: Record<string, string[]>; title?: string }
    | string
    | null;

  if (typeof body === 'string' && body.trim()) {
    return body;
  }

  if (body && typeof body === 'object') {
    if (body.errors) {
      const messages = Object.values(body.errors).flat().filter(Boolean);
      if (messages.length) return messages.join(' ');
    }
    if (body.message) return body.message;
    if (body.title) return body.title;
  }

  return fallback;
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(NotificationService);

  return next(req).pipe(
    catchError((error: unknown) => {
      // 4xx обрабатывают сами страницы (у них есть контекст и inline-сообщения).
      // Глобально показываем только аварии сервера и обрыв связи.
      if (
        error instanceof HttpErrorResponse &&
        (error.status === 0 || error.status >= 500)
      ) {
        notifications.error(extractErrorMessage(error));
      }
      return throwError(() => error);
    }),
  );
};
