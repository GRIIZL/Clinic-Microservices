import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { ConfirmDialogService } from '../../../core/services/confirm-dialog.service';
import { Office, OfficeFormValue } from '../../../core/models/office.models';

export interface OfficeDialogData {
  office: Office | null;
}

@Component({
  selector: 'app-office-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatRadioModule,
  ],
  templateUrl: './office-dialog.html',
  styleUrl: './office-dialog.scss',
})
export class OfficeDialog {
  private readonly dialogRef = inject<MatDialogRef<OfficeDialog, OfficeFormValue | undefined>>(
    MatDialogRef,
  );
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly fb = inject(FormBuilder);

  readonly data = inject<OfficeDialogData>(MAT_DIALOG_DATA);

  readonly form = this.fb.nonNullable.group({
    city: [this.data.office?.city ?? '', [Validators.required]],
    street: [this.data.office?.street ?? '', [Validators.required]],
    houseNumber: [this.data.office?.houseNumber ?? '', [Validators.required]],
    officeNumber: [this.data.office?.officeNumber ?? '', [Validators.required]],
    registryPhoneNumber: [this.data.office?.registryPhoneNumber ?? '', [Validators.required]],
    status: [this.data.office?.status ?? ('Active' as OfficeFormValue['status'])],
    photoUrl: [this.data.office?.photoUrl ?? ''],
  });

  readonly saving = signal(false);

  get isEditMode(): boolean {
    return this.data.office !== null;
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.dialogRef.close(this.form.getRawValue());
  }

  async cancel(): Promise<void> {
    if (this.form.dirty) {
      const discard = await this.confirmDialog.confirmDiscard();
      if (!discard) return;
    }

    this.dialogRef.close(undefined);
  }
}
