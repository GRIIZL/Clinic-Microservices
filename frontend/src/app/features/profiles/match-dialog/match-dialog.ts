import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { ExistingProfileData } from '../../../core/models/profile.models';

export interface MatchDialogData {
  existing: ExistingProfileData;
}

export type MatchDialogResult = 'link' | 'create-new';

/** AC-6 / AC-7: найден похожий профиль — связать с аккаунтом или создать новый. */
@Component({
  selector: 'app-match-dialog',
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title class="warning-title">
      <span class="material-symbols-outlined">warning</span>
      Внимание! Найден похожий профиль!
    </h2>

    <mat-dialog-content>
      <div class="match-info">
        <p><strong>ФИО:</strong> {{ data.existing.lastName }} {{ data.existing.firstName }}</p>
        <p><strong>Телефон:</strong> {{ data.existing.phoneNumber }}</p>
      </div>
      <p class="question">Вы уже посещали нашу клинику ранее?</p>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-stroked-button type="button" (click)="close('create-new')">
        No, create new
      </button>
      <button mat-flat-button color="primary" type="button" (click)="close('link')">
        Yes, it's me
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .warning-title {
      display: flex;
      align-items: center;
      gap: 8px;
      color: #b45309;
      font-size: 1.05rem;
    }

    .match-info {
      padding: 12px;
      border-radius: 8px;
      background: #fffbeb;
      border: 1px solid #fef3c7;
      font-size: 0.85rem;
      color: #475569;
    }

    .match-info p {
      margin: 0 0 4px;
    }

    .match-info p:last-child {
      margin-bottom: 0;
    }

    .question {
      margin: 16px 0 0;
      font-size: 0.9rem;
    }
  `,
})
export class MatchDialog {
  private readonly dialogRef = inject<MatDialogRef<MatchDialog, MatchDialogResult>>(MatDialogRef);

  readonly data = inject<MatchDialogData>(MAT_DIALOG_DATA);

  close(result: MatchDialogResult): void {
    this.dialogRef.close(result);
  }
}
