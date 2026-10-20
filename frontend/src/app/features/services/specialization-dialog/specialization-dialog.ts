import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { ConfirmDialogService } from '../../../core/services/confirm-dialog.service';
import { Specialization, SpecializationFormValue } from '../../../core/models/service.models';

export interface SpecializationDialogData {
  specialization: Specialization | null;
}

@Component({
  selector: 'app-specialization-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatRadioModule,
  ],
  templateUrl: './specialization-dialog.html',
  styleUrl: './specialization-dialog.scss',
})
export class SpecializationDialog {
  private readonly dialogRef = inject<
    MatDialogRef<SpecializationDialog, SpecializationFormValue | undefined>
  >(MatDialogRef);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly fb = inject(FormBuilder);

  readonly data = inject<SpecializationDialogData>(MAT_DIALOG_DATA);

  readonly form = this.fb.nonNullable.group({
    name: [this.data.specialization?.name ?? '', [Validators.required]],
    status: [this.data.specialization?.status ?? ('Active' as SpecializationFormValue['status'])],
  });

  get isEditMode(): boolean {
    return this.data.specialization !== null;
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
