import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { AppointmentResult } from '../../../core/models/appointment.models';
import { AppointmentService } from '../../../core/services/appointment.service';
import { NotificationService } from '../../../core/services/notification.service';
import { extractErrorMessage } from '../../../core/interceptors/error.interceptor';

export interface AppointmentResultViewDialogData {
  appointmentId: string;
  result: AppointmentResult;
}

/** US-60 / US-61 / US-62: просмотр заключения и скачивание PDF. */
@Component({
  selector: 'app-appointment-result-view-dialog',
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Медицинское заключение</h2>

    <mat-dialog-content>
      <dl class="result-view">
        <div>
          <dt>Жалобы</dt>
          <dd>{{ data.result.complaints }}</dd>
        </div>
        <div>
          <dt>Заключение</dt>
          <dd>{{ data.result.conclusion }}</dd>
        </div>
        <div>
          <dt>Рекомендации</dt>
          <dd>{{ data.result.recommendations }}</dd>
        </div>
      </dl>
      <p class="created-at">Создано: {{ createdAt() }}</p>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-stroked-button type="button" mat-dialog-close>Закрыть</button>
      <button
        mat-flat-button
        color="primary"
        type="button"
        [disabled]="downloading()"
        (click)="downloadPdf()"
      >
        <span class="material-symbols-outlined">download</span>
        {{ downloading() ? 'Скачивание...' : 'Скачать PDF' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .result-view {
      display: flex;
      flex-direction: column;
      gap: 14px;
      margin: 0;
      font-size: 0.9rem;
    }

    dt {
      font-weight: 600;
      color: #1e293b;
      margin-bottom: 4px;
    }

    dd {
      margin: 0;
      color: #334155;
      line-height: 1.6;
      white-space: pre-wrap;
    }

    .created-at {
      margin: 18px 0 0;
      font-size: 0.78rem;
      color: #64748b;
    }
  `,
})
export class AppointmentResultViewDialog {
  private readonly appointmentsApi = inject(AppointmentService);
  private readonly notifications = inject(NotificationService);

  readonly data = inject<AppointmentResultViewDialogData>(MAT_DIALOG_DATA);
  readonly downloading = signal(false);

  createdAt(): string {
    return this.data.result.createdAt
      ? new Date(this.data.result.createdAt).toLocaleString('ru-RU')
      : '—';
  }

  downloadPdf(): void {
    this.downloading.set(true);

    this.appointmentsApi.downloadResultPdf(this.data.appointmentId).subscribe({
      next: (blob) => {
        this.downloading.set(false);
        this.appointmentsApi.saveResultPdf(this.data.appointmentId, blob);
      },
      error: (error) => {
        this.downloading.set(false);
        this.notifications.error(
          error?.status === 404
            ? 'Документ не найден: по приёму нет заключения.'
            : extractErrorMessage(error, 'Ошибка скачивания PDF.'),
        );
      },
    });
  }
}
