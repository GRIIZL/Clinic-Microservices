import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../services/auth.service';

/** Пускает только авторизованных; при истёкшем access-токене пробует refresh. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth
    .ensureSession()
    .pipe(map((ok) => (ok ? true : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } }))));
};

/** Пускает только пользователей с одной из указанных ролей. */
export const roleGuard = (...roles: Parameters<AuthService['hasRole']>) => {
  const guard: CanActivateFn = () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    return auth.hasRole(...roles) ? true : router.createUrlTree(['/']);
  };

  return guard;
};

/** Не пускает уже авторизованного пользователя на страницы входа/регистрации. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.isAuthenticated() ? router.createUrlTree(['/']) : true;
};
