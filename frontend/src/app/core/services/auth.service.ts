import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, map, of, shareReplay, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthResponse,
  CurrentUser,
  LoginRequest,
  RegisterRequest,
  UserRole,
} from '../models/auth.models';

const ACCESS_TOKEN_KEY = 'accessToken';
const REFRESH_TOKEN_KEY = 'refreshToken';

interface JwtPayload {
  nameid?: string;
  email?: string;
  role?: UserRole;
  exp?: number;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/auth`;

  private readonly user = signal<CurrentUser | null>(this.readUserFromToken());
  private refreshInFlight: Observable<AuthResponse> | null = null;

  readonly currentUser = this.user.asReadonly();
  readonly isAuthenticated = computed(() => this.user() !== null);
  readonly role = computed<UserRole | null>(() => this.user()?.role ?? null);
  readonly isStaff = computed(() => {
    const role = this.role();
    return role === 'Admin' || role === 'Receptionist';
  });

  get accessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  get refreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  hasRole(...roles: UserRole[]): boolean {
    const role = this.role();
    return role !== null && roles.includes(role);
  }

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/sign-in`, credentials)
      .pipe(tap((response) => this.applyAuthResponse(response)));
  }

  register(request: RegisterRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/register`, request);
  }

  checkEmail(email: string): Observable<{ exists: boolean }> {
    return this.http.get<{ exists: boolean }>(`${this.apiUrl}/check-email`, {
      params: { email },
    });
  }

  verifyEmail(token: string): Observable<{ message: string }> {
    return this.http.get<{ message: string }>(`${this.apiUrl}/verify`, {
      params: { token },
    });
  }

  /**
   * Обновляет пару токенов. Параллельные вызовы переиспользуют один HTTP-запрос,
   * чтобы не плодить refresh-запросы при пачке 401-х.
   */
  refresh(): Observable<AuthResponse> {
    const refreshToken = this.refreshToken;
    if (!refreshToken) {
      return throwError(() => new Error('Refresh token is missing'));
    }

    if (!this.refreshInFlight) {
      this.refreshInFlight = this.http
        .post<AuthResponse>(`${this.apiUrl}/refresh`, { refreshToken })
        .pipe(
          tap((response) => this.applyAuthResponse(response)),
          catchError((error) => {
            this.clearSession();
            return throwError(() => error);
          }),
          finalize(() => {
            this.refreshInFlight = null;
          }),
          shareReplay({ bufferSize: 1, refCount: true }),
        );
    }

    return this.refreshInFlight;
  }

  /**
   * Проверяет, что сессия жива. Если access-токен истёк, но есть refresh-токен —
   * пытается молча обновить пару.
   */
  ensureSession(): Observable<boolean> {
    const token = this.accessToken;
    if (token && !this.isExpired(token)) {
      return of(true);
    }

    if (!this.refreshToken) {
      this.clearSession();
      return of(false);
    }

    return this.refresh().pipe(
      map(() => true),
      catchError(() => of(false)),
    );
  }

  logout(): Observable<unknown> {
    const token = this.accessToken;
    if (!token) {
      this.clearSession();
      return of(null);
    }

    return this.http.post(`${this.apiUrl}/sign-out`, {}).pipe(
      catchError(() => of(null)),
      tap(() => this.clearSession()),
    );
  }

  clearSession(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    this.user.set(null);
  }

  private applyAuthResponse(response: AuthResponse): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, response.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, response.refreshToken);
    this.user.set(this.readUserFromToken());
  }

  private readUserFromToken(): CurrentUser | null {
    const token = localStorage.getItem(ACCESS_TOKEN_KEY);
    if (!token) return null;

    const payload = this.decodeToken(token);
    if (!payload || this.isExpired(token)) return null;

    if (!payload.nameid || !payload.email || !payload.role) return null;

    return {
      accountId: payload.nameid,
      email: payload.email,
      role: payload.role,
    };
  }

  private decodeToken(token: string): JwtPayload | null {
    try {
      const payload = token.split('.')[1];
      if (!payload) return null;

      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const decoded = decodeURIComponent(
        atob(normalized)
          .split('')
          .map((char) => `%${char.charCodeAt(0).toString(16).padStart(2, '0')}`)
          .join(''),
      );

      return JSON.parse(decoded) as JwtPayload;
    } catch {
      return null;
    }
  }

  private isExpired(token: string): boolean {
    const payload = this.decodeToken(token);
    if (!payload?.exp) return true;

    // exp хранится в секундах; 5 секунд запаса на рассинхрон часов.
    return payload.exp * 1000 <= Date.now() + 5_000;
  }
}
