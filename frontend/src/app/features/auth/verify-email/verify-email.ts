import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { extractErrorMessage } from '../../../core/interceptors/error.interceptor';

type VerifyState = 'loading' | 'success' | 'error';

@Component({
  selector: 'app-verify-email',
  imports: [MatButtonModule, MatProgressSpinnerModule, RouterLink],
  template: `
    <div class="auth-page">
      <div class="auth-card verify-card">
        <div class="auth-brand">
          <span class="material-symbols-outlined">local_hospital</span>
          <span>Медицинский портал</span>
        </div>

        @switch (state()) {
          @case ('loading') {
            <h1>Подтверждение email</h1>
            <p class="auth-subtitle">Проверяем ссылку подтверждения...</p>
            <mat-spinner diameter="36" />
          }
          @case ('success') {
            <h1>Email подтверждён</h1>
            <p class="auth-success">{{ message() }}</p>
            <a mat-flat-button color="primary" class="auth-submit" routerLink="/login">
              Войти в систему
            </a>
          }
          @case ('error') {
            <h1>Не удалось подтвердить email</h1>
            <p class="auth-error">{{ message() }}</p>
            <a mat-stroked-button class="auth-submit" routerLink="/login">На страницу входа</a>
          }
        }
      </div>
    </div>
  `,
  styleUrl: '../auth-shell.scss',
  styles: `
    .verify-card {
      display: flex;
      flex-direction: column;
      align-items: stretch;
      gap: 8px;
    }

    .verify-card mat-spinner {
      align-self: center;
      margin: 12px 0;
    }

    a.auth-submit {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      text-decoration: none;
    }
  `,
})
export class VerifyEmailPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  readonly state = signal<VerifyState>('loading');
  readonly message = signal('');

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');

    if (!token) {
      this.state.set('error');
      this.message.set('Токен подтверждения отсутствует в ссылке.');
      return;
    }

    this.auth.verifyEmail(token).subscribe({
      next: (response) => {
        this.state.set('success');
        this.message.set(response.message || 'Email успешно подтвержден!');
      },
      error: (error) => {
        this.state.set('error');
        this.message.set(extractErrorMessage(error, 'Невалидный или просроченный токен.'));
      },
    });
  }
}
