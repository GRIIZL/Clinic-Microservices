import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import {
  MedicalService,
  MedicalServiceFormValue,
  SERVICE_CATEGORIES,
} from '../../../core/models/service.models';

export interface MedicalServiceDialogData {
  service: MedicalService | null;
}

@Component({
  selector: 'app-medical-service-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatRadioModule,
  ],
  templateUrl: './medical-service-dialog.html',
  styleUrl: './medical-service-dialog.scss',
})
export class MedicalServiceDialog {
  private readonly dialogRef = inject<
    MatDialogRef<MedicalServiceDialog, MedicalServiceFormValue | undefined>
  >(MatDialogRef);
  private readonly fb = inject(FormBuilder);

  readonly data = inject<MedicalServiceDialogData>(MAT_DIALOG_DATA);
  readonly categories = SERVICE_CATEGORIES;

  readonly form = this.fb.nonNullable.group({
    name: [this.data.service?.name ?? '', [Validators.required]],
    price: [this.data.service?.price ?? (null as number | null), [Validators.required, Validators.min(0.01)]],
    categoryName: [
      this.data.service?.categoryName ?? ('Consultations' as MedicalServiceFormValue['categoryName']),
      [Validators.required],
    ],
    status: [this.data.service?.status ?? ('Active' as MedicalServiceFormValue['status'])],
  });

  get isEditMode(): boolean {
    return this.data.service !== null;
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.dialogRef.close({
      name: raw.name.trim(),
      price: Number(raw.price),
      categoryName: raw.categoryName,
      status: raw.status,
    });
  }

  cancel(): void {
    this.dialogRef.close(undefined);
  }
}
