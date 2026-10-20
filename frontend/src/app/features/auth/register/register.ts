import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { extractErrorMessage } from '../../../core/interceptors/error.interceptor';
import { RegisterRequest } from '../../../core/models/auth.models';

type EmailState = 'unknown' | 'checking' | 'unique' | 'taken';

function passwordsMatch(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;
  return password === confirmPassword ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: '../auth-shell.scss',
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.nonNullable.group(
    {
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(15)]],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: passwordsMatch },
  );

  readonly showPassword = signal(false);
  readonly loading = signal(false);
  readonly emailState = signal<EmailState>('unknown');
  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  togglePassword(): void {
    this.showPassword.update((value) => !value);
  }

  checkEmail(): void {
    const control = this.form.controls.email;
    const email = control.value.trim();

    if (!email || control.invalid) {
      this.emailState.set('unknown');
      return;
    }

    this.emailState.set('checking');
    this.auth.checkEmail(email).subscribe({
      next: (result) => this.emailState.set(result.exists ? 'taken' : 'unique'),
      error: () => this.emailState.set('unknown'),
    });
  }

  submit(): void {
    if (this.loading()) return;

    this.errorMessage.set('');

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.emailState() === 'taken') {
      this.errorMessage.set('Невозможно зарегистрироваться. Этот Email занят.');
      return;
    }

    this.loading.set(true);

    this.auth.register(this.form.getRawValue() as RegisterRequest).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.successMessage.set(response.message || 'Регистрация успешна. Проверьте почту.');
      },
      error: (error) => {
        this.loading.set(false);
        this.errorMessage.set(extractErrorMessage(error, 'Ошибка регистрации.'));
      },
    });
  }
}
