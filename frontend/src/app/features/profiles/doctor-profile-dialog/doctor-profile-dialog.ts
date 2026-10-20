import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { DoctorProfile } from '../../../core/models/profile.models';
import { StatusBadge } from '../../../shared/ui/status-badge/status-badge';

export interface DoctorProfileDialogData {
  doctor: DoctorProfile;
}

/** US-17: детальный просмотр профиля врача. */
@Component({
  selector: 'app-doctor-profile-dialog',
  imports: [MatDialogModule, MatButtonModule, StatusBadge],
  template: `
    <h2 mat-dialog-title>{{ data.doctor.lastName }} {{ data.doctor.firstName }} {{ data.doctor.middleName }}</h2>

    <mat-dialog-content>
      <dl class="profile-list">
        <div><dt>Специализация</dt><dd>{{ data.doctor.specialization }}</dd></div>
        <div><dt>Стаж</dt><dd>{{ data.doctor.experience }} лет</dd></div>
        <div><dt>Статус</dt><dd><app-status-badge [status]="data.doctor.status" /></dd></div>
        <div><dt>Email</dt><dd>{{ data.doctor.email }}</dd></div>
        <div><dt>Дата рождения</dt><dd>{{ dateOnly(data.doctor.dateOfBirth) }}</dd></div>
        <div><dt>ID офиса</dt><dd class="mono">{{ data.doctor.officeId || '—' }}</dd></div>
      </dl>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-flat-button color="primary" mat-dialog-close type="button">Закрыть</button>
    </mat-dialog-actions>
  `,
  styles: `
    .profile-list {
      display: flex;
      flex-direction: column;
      gap: 10px;
      margin: 0;
      font-size: 0.875rem;
    }

    .profile-list div {
      display: grid;
      grid-template-columns: 140px 1fr;
      gap: 12px;
      align-items: center;
    }

    dt {
      color: #64748b;
    }

    dd {
      margin: 0;
      color: #1e293b;
    }

    .mono {
      font-family: monospace;
      font-size: 0.8rem;
      word-break: break-all;
    }
  `,
})
export class DoctorProfileDialog {
  readonly data = inject<DoctorProfileDialogData>(MAT_DIALOG_DATA);

  dateOnly(value: string): string {
    return value ? value.slice(0, 10) : '—';
  }
}
