import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AppointmentResult } from '../../../core/models/appointment.models';

export interface AppointmentResultDialogData {
  appointmentId: string;
  result: AppointmentResult | null;
}

export interface AppointmentResultFormValue {
  complaints: string;
  conclusion: string;
  recommendations: string;
}

/** US-58 / US-59: создание и редактирование медицинского заключения. */
@Component({
  selector: 'app-appointment-result-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  templateUrl: './appointment-result-dialog.html',
  styleUrl: './appointment-result-dialog.scss',
})
export class AppointmentResultDialog {
  private readonly dialogRef = inject<
    MatDialogRef<AppointmentResultDialog, AppointmentResultFormValue | undefined>
  >(MatDialogRef);
  private readonly fb = inject(FormBuilder);

  readonly data = inject<AppointmentResultDialogData>(MAT_DIALOG_DATA);

  readonly form = this.fb.nonNullable.group({
    complaints: [this.data.result?.complaints ?? '', [Validators.required]],
    conclusion: [this.data.result?.conclusion ?? '', [Validators.required]],
    recommendations: [this.data.result?.recommendations ?? '', [Validators.required]],
  });

  get isEditMode(): boolean {
    return this.data.result !== null;
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.dialogRef.close({
      complaints: raw.complaints.trim(),
      conclusion: raw.conclusion.trim(),
      recommendations: raw.recommendations.trim(),
    });
  }

  cancel(): void {
    this.dialogRef.close(undefined);
  }
}
