import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(message, 'Закрыть', {
      duration: 4000,
      panelClass: 'snackbar-success',
    });
  }

  info(message: string): void {
    this.snackBar.open(message, 'Закрыть', {
      duration: 5000,
      panelClass: 'snackbar-info',
    });
  }

  error(message: string): void {
    this.snackBar.open(message, 'Закрыть', {
      duration: 7000,
      panelClass: 'snackbar-error',
    });
  }
}
