import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

/** Запросы, для которых повторная попытка после refresh бессмысленна. */
const AUTH_ENDPOINTS = ['/auth/sign-in', '/auth/refresh', '/auth/register'];

export const refreshTokenInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  return next(req).pipe(
    catchError((error: unknown) => {
      const isUnauthorized = error instanceof HttpErrorResponse && error.status === 401;
      const isAuthRequest = AUTH_ENDPOINTS.some((path) => req.url.includes(path));

      if (!isUnauthorized || isAuthRequest || !auth.refreshToken) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        switchMap((response) =>
          next(
            req.clone({
              setHeaders: { Authorization: `Bearer ${response.accessToken}` },
            }),
          ),
        ),
        catchError((refreshError) => throwError(() => refreshError)),
      );
    }),
  );
};
