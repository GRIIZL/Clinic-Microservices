import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Appointment, RescheduleAppointmentRequest } from '../../../core/models/appointment.models';
import { DoctorProfile } from '../../../core/models/profile.models';
import { AppointmentService } from '../../../core/services/appointment.service';
import { ConfirmDialogService } from '../../../core/services/confirm-dialog.service';
import { NotificationService } from '../../../core/services/notification.service';
import { extractErrorMessage } from '../../../core/interceptors/error.interceptor';
import { formatRuDate, fromIsoDate, startOfToday, toIsoDate } from '../../../core/utils/date.utils';
import { SlotsGrid } from '../../../shared/ui/slots-grid/slots-grid';

export interface RescheduleDialogData {
  appointment: Appointment;
  /** Врачи, отфильтрованные по специализации и офису записи (F-2). */
  doctors: DoctorProfile[];
  /** Категория услуги — от неё зависит длительность приёма и набор слотов. */
  categoryName: string;
}

/** Сколько дней вперёд запрашиваем доступные даты. */
const AVAILABLE_DATES_HORIZON_DAYS = 60;

@Component({
  selector: 'app-reschedule-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    SlotsGrid,
  ],
  templateUrl: './reschedule-dialog.html',
  styleUrl: './reschedule-dialog.scss',
})
export class RescheduleDialog implements OnInit {
  private readonly dialogRef = inject<
    MatDialogRef<RescheduleDialog, RescheduleAppointmentRequest | undefined>
  >(MatDialogRef);
  private readonly appointmentsApi = inject(AppointmentService);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly notifications = inject(NotificationService);
  private readonly fb = inject(FormBuilder);

  readonly data = inject<RescheduleDialogData>(MAT_DIALOG_DATA);
  readonly today = startOfToday();

  /** AC-3: редактируются только врач, дата и слот — остальное показываем как есть. */
  readonly form = this.fb.group({
    doctorId: [this.data.appointment.doctorId, [Validators.required]],
    date: [fromIsoDate(this.data.appointment.date), [Validators.required]],
    timeslot: [this.data.appointment.timeslot, [Validators.required]],
  });

  readonly availableDates = signal<string[]>([]);
  readonly startTimes = signal<string[]>([]);
  readonly requiredSlotsCount = signal(0);
  readonly loadingDates = signal(false);
  readonly loadingSlots = signal(false);

  /** F-5: в календаре доступны только дни со свободными окнами. */
  readonly dateFilter = (date: Date | null): boolean => {
    if (!date) return false;

    const iso = toIsoDate(date);
    if (iso < toIsoDate(this.today)) return false;

    // Дату самой записи разрешаем всегда, иначе её нельзя оставить без изменений.
    if (iso === this.data.appointment.date.slice(0, 10)) return true;

    return this.availableDates().includes(iso);
  };

  ngOnInit(): void {
    this.loadAvailableDates();
    this.loadSlots();
  }

  onDoctorChange(): void {
    this.form.controls.timeslot.setValue('');
    this.startTimes.set([]);
    this.loadAvailableDates();
    this.loadSlots();
  }

  onDateChange(): void {
    this.form.controls.timeslot.setValue('');
    this.startTimes.set([]);
    this.loadSlots();
  }

  selectSlot(timeslot: string): void {
    this.form.controls.timeslot.setValue(timeslot);
  }

  async confirm(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const doctor = this.data.doctors.find((d) => d.id === raw.doctorId);
    const doctorName = doctor
      ? `${doctor.lastName} ${doctor.firstName}`.trim()
      : 'выбранному врачу';

    // AC-5..AC-8: подтверждение переноса через диалог Yes/No.
    const confirmed = await this.confirmDialog.confirm({
      title: 'Перенести запись?',
      message: `Перенести приём на ${formatRuDate(raw.date!)} в ${raw.timeslot} к ${doctorName}?`,
      confirmText: 'Yes',
      cancelText: 'No',
    });
    if (!confirmed) return;

    this.dialogRef.close({
      doctorId: raw.doctorId!,
      date: toIsoDate(raw.date!),
      timeslot: raw.timeslot!,
    });
  }

  cancel(): void {
    this.dialogRef.close(undefined);
  }

  doctorLabel(doctor: DoctorProfile): string {
    return `${doctor.lastName} ${doctor.firstName} ${doctor.middleName}`.trim();
  }

  currentDate(): string {
    return formatRuDate(this.data.appointment.date);
  }

  private loadAvailableDates(): void {
    const doctorId = this.form.controls.doctorId.value;
    if (!doctorId) return;

    const from = toIsoDate(this.today);
    const to = toIsoDate(
      new Date(this.today.getFullYear(), this.today.getMonth(), this.today.getDate() + AVAILABLE_DATES_HORIZON_DAYS),
    );

    this.loadingDates.set(true);
    this.appointmentsApi.getAvailableDates(doctorId, from, to, this.data.categoryName).subscribe({
      next: (dates) => {
        this.availableDates.set(dates);
        this.loadingDates.set(false);
      },
      error: (error) => {
        this.loadingDates.set(false);
        this.notifications.error(
          extractErrorMessage(error, 'Не удалось загрузить доступные даты.'),
        );
      },
    });
  }

  private loadSlots(): void {
    const { doctorId, date } = this.form.getRawValue();
    if (!doctorId || !date) {
      this.startTimes.set([]);
      return;
    }

    this.loadingSlots.set(true);
    this.appointmentsApi.getAvailableSlots(doctorId, toIsoDate(date), this.data.categoryName).subscribe({
      next: (response) => {
        this.requiredSlotsCount.set(response.requiredSlotsCount);
        this.startTimes.set(this.withCurrentTimeslot(response.availableStartTimes));
        this.loadingSlots.set(false);
      },
      error: (error) => {
        this.loadingSlots.set(false);
        this.startTimes.set([]);
        this.notifications.error(extractErrorMessage(error, 'Не удалось загрузить слоты.'));
      },
    });
  }

  /**
   * Собственный слот записи не попадает в список свободных (он занят этой же записью),
   * но при неизменных враче и дате его нужно показать как выбранный.
   */
  private withCurrentTimeslot(times: string[]): string[] {
    const appointment = this.data.appointment;
    const sameDoctor = this.form.controls.doctorId.value === appointment.doctorId;
    const sameDate = this.form.controls.date.value
      ? toIsoDate(this.form.controls.date.value!) === appointment.date.slice(0, 10)
      : false;

    if (!sameDoctor || !sameDate) return times;

    const currentStart = appointment.timeslot.split('-')[0]?.trim();
    if (!currentStart || times.includes(currentStart)) return times;

    return [...times, currentStart].sort();
  }
}
