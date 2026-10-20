import { Component, inject } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ConfirmDialogService } from '../../../core/services/confirm-dialog.service';
import { CreatePatientRequest, PatientProfile } from '../../../core/models/profile.models';

export interface PatientDialogData {
  patient: PatientProfile | null;
  /** ID аккаунта текущего пользователя — привязываем новый профиль сразу. */
  accountId?: string | null;
}

function notInFuture(control: AbstractControl): ValidationErrors | null {
  const value = control.value as string;
  if (!value) return null;
  return new Date(value) > new Date() ? { futureDate: true } : null;
}

@Component({
  selector: 'app-patient-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  templateUrl: './patient-dialog.html',
  styleUrl: './patient-dialog.scss',
})
export class PatientDialog {
  private readonly dialogRef = inject<MatDialogRef<PatientDialog, CreatePatientRequest | undefined>>(
    MatDialogRef,
  );
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly fb = inject(FormBuilder);

  readonly data = inject<PatientDialogData>(MAT_DIALOG_DATA);

  readonly form = this.fb.nonNullable.group({
    firstName: [this.data.patient?.firstName ?? '', [Validators.required]],
    lastName: [this.data.patient?.lastName ?? '', [Validators.required]],
    middleName: [this.data.patient?.middleName ?? ''],
    phoneNumber: [this.data.patient?.phoneNumber ?? '', [Validators.required]],
    dateOfBirth: [this.toDateInput(this.data.patient?.dateOfBirth), [Validators.required, notInFuture]],
  });

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.dialogRef.close({
      accountId: this.data.patient ? undefined : (this.data.accountId ?? null),
      firstName: raw.firstName.trim(),
      lastName: raw.lastName.trim(),
      middleName: raw.middleName.trim() || null,
      phoneNumber: raw.phoneNumber.trim(),
      dateOfBirth: raw.dateOfBirth,
    });
  }

  async cancel(): Promise<void> {
    if (this.form.dirty) {
      const discard = await this.confirmDialog.confirmDiscard();
      if (!discard) return;
    }

    this.dialogRef.close(undefined);
  }

  private toDateInput(value?: string): string {
    if (!value) return '';
    return value.slice(0, 10);
  }
}
