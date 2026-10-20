import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { firstValueFrom } from 'rxjs';
import {
  Appointment,
  AppointmentResult,
  AppointmentScheduleItem,
  AvailableSlotsResponse,
} from '../../core/models/appointment.models';
import { Office } from '../../core/models/office.models';
import { DoctorProfile } from '../../core/models/profile.models';
import { MedicalService, Specialization } from '../../core/models/service.models';
import { AppointmentService } from '../../core/services/appointment.service';
import { AuthService } from '../../core/services/auth.service';
import { DoctorService } from '../../core/services/doctor.service';
import { OfficeService } from '../../core/services/office.service';
import { PatientService } from '../../core/services/patient.service';
import { SpecializationService } from '../../core/services/specialization.service';
import { NotificationService } from '../../core/services/notification.service';
import { ConfirmDialogService } from '../../core/services/confirm-dialog.service';
import { extractErrorMessage } from '../../core/interceptors/error.interceptor';
import { formatRuDate, startOfToday, toIsoDate } from '../../core/utils/date.utils';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { PageHeader } from '../../shared/ui/page-header/page-header';
import { SlotsGrid } from '../../shared/ui/slots-grid/slots-grid';
import { StatusBadge } from '../../shared/ui/status-badge/status-badge';
import {
  AppointmentResultDialog,
  AppointmentResultDialogData,
  AppointmentResultFormValue,
} from './appointment-result-dialog/appointment-result-dialog';
import {
  AppointmentResultViewDialog,
  AppointmentResultViewDialogData,
} from './appointment-result-view-dialog/appointment-result-view-dialog';

@Component({
  selector: 'app-appointments-page',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    PageHeader,
    EmptyState,
    SlotsGrid,
    StatusBadge,
  ],
  templateUrl: './appointments-page.html',
  styleUrl: './appointments-page.scss',
})
export class AppointmentsPage implements OnInit {
  private readonly appointmentsApi = inject(AppointmentService);
  private readonly specializationsApi = inject(SpecializationService);
  private readonly doctorsApi = inject(DoctorService);
  private readonly officesApi = inject(OfficeService);
  private readonly patientsApi = inject(PatientService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly dialog = inject(MatDialog);
  private readonly fb = inject(FormBuilder);

  readonly isPatient = computed(() => this.auth.role() === 'Patient');
  readonly today = startOfToday();

  readonly specializations = signal<Specialization[]>([]);
  readonly doctors = signal<DoctorProfile[]>([]);
  readonly offices = signal<Office[]>([]);

  readonly slots = signal<AvailableSlotsResponse | null>(null);
  readonly slotsLoading = signal(false);
  readonly creating = signal(false);
  readonly patientProfileMissing = signal(false);

  // --- US-6: форма записи ---
  readonly bookingForm = this.fb.group({
    patientId: ['', [Validators.required]],
    specializationId: ['', [Validators.required]],
    serviceId: ['', [Validators.required]],
    doctorId: ['', [Validators.required]],
    officeId: ['', [Validators.required]],
    date: [null as Date | null, [Validators.required]],
    timeslot: ['', [Validators.required]],
  });

  readonly selectedSpecialization = signal<Specialization | null>(null);

  readonly activeServices = computed<MedicalService[]>(
    () => this.selectedSpecialization()?.services.filter((s) => s.status === 'Active') ?? [],
  );

  /** Врачи под выбранную специализацию (US-6 F-2). */
  readonly doctorsForBooking = computed<DoctorProfile[]>(() => {
    const specialization = this.selectedSpecialization();
    if (!specialization) return [];
    const name = specialization.name.toLowerCase();
    return this.doctors().filter((doctor) => doctor.specialization.toLowerCase() === name);
  });

  readonly activeOffices = computed(() => this.offices().filter((o) => o.status === 'Active'));

  // --- История записей ---
  readonly historyForm = this.fb.nonNullable.group({
    patientId: ['', [Validators.required]],
  });
  readonly history = signal<Appointment[] | null>(null);
  readonly historyLoading = signal(false);

  // --- US-10: расписание врача ---
  readonly scheduleForm = this.fb.group({
    doctorId: ['', [Validators.required]],
    date: [startOfToday(), [Validators.required]],
  });
  readonly schedule = signal<AppointmentScheduleItem[] | null>(null);
  readonly scheduleLoading = signal(false);

  ngOnInit(): void {
    this.loadSpecializations();
    this.loadDoctors();
    this.loadOffices();
    this.initPatientContext();
  }

  // ==================== Загрузка справочников ====================

  private loadSpecializations(): void {
    this.specializationsApi.getAll().subscribe({
      next: (specializations) =>
        this.specializations.set(specializations.filter((s) => s.status === 'Active')),
      error: (error) =>
        this.notifications.error(extractErrorMessage(error, 'Services API недоступен.')),
    });
  }

  private loadDoctors(): void {
    this.doctorsApi.getForPatients().subscribe({
      next: (doctors) => this.doctors.set(doctors),
      error: (error) => {
        if (error?.status !== 404) {
          this.notifications.error(extractErrorMessage(error, 'Не удалось загрузить врачей.'));
        }
      },
    });
  }

  private loadOffices(): void {
    this.officesApi.getAll().subscribe({
      next: (offices) => this.offices.set(offices),
      error: (error) =>
        this.notifications.error(extractErrorMessage(error, 'Не удалось загрузить офисы.')),
    });
  }

  /** Пациенту подставляем его профиль автоматически (AC-4 US-6). */
  private initPatientContext(): void {
    const accountId = this.auth.currentUser()?.accountId;
    if (!this.isPatient() || !accountId) return;

    this.patientsApi.getByAccountId(accountId).subscribe({
      next: (profile) => {
        this.bookingForm.controls.patientId.setValue(profile.id);
        this.historyForm.controls.patientId.setValue(profile.id);
      },
      error: (error) => {
        if (error?.status === 404) {
          this.patientProfileMissing.set(true);
          return;
        }
        this.notifications.error(
          extractErrorMessage(error, 'Не удалось загрузить профиль пациента.'),
        );
      },
    });
  }

  // ==================== US-6: запись на приём ====================

  onSpecializationChange(specializationId: string): void {
    const specialization = this.specializations().find((s) => s.id === specializationId) ?? null;
    this.selectedSpecialization.set(specialization);

    this.bookingForm.patchValue({ serviceId: '', doctorId: '', timeslot: '' });
    this.slots.set(null);
  }

  onSlotInputsChange(): void {
    const { doctorId, date, serviceId } = this.bookingForm.getRawValue();
    const service = this.activeServices().find((s) => s.id === serviceId);

    this.bookingForm.controls.timeslot.setValue('');
    this.slots.set(null);

    if (!doctorId || !date || !service) return;

    this.slotsLoading.set(true);
    this.appointmentsApi
      .getAvailableSlots(doctorId, toIsoDate(date), service.categoryName)
      .subscribe({
        next: (response) => {
          this.slots.set(response);
          this.slotsLoading.set(false);
        },
        error: (error) => {
          this.slotsLoading.set(false);
          this.notifications.error(extractErrorMessage(error, 'Не удалось загрузить слоты.'));
        },
      });
  }

  selectSlot(timeslot: string): void {
    this.bookingForm.controls.timeslot.setValue(timeslot);
  }

  createAppointment(): void {
    if (this.bookingForm.invalid) {
      this.bookingForm.markAllAsTouched();
      return;
    }

    const raw = this.bookingForm.getRawValue();
    this.creating.set(true);

    this.appointmentsApi
      .create({
        patientId: raw.patientId!,
        patientEmail: this.auth.currentUser()?.email ?? null,
        specializationId: raw.specializationId!,
        doctorId: raw.doctorId!,
        serviceId: raw.serviceId!,
        officeId: raw.officeId!,
        date: toIsoDate(raw.date!),
        timeslot: raw.timeslot!,
      })
      .subscribe({
        next: (response) => {
          this.creating.set(false);
          this.notifications.success(`Запись создана (ID: ${response.appointmentId}).`);
          this.bookingForm.patchValue({ timeslot: '' });
          this.slots.set(null);

          if (this.historyForm.controls.patientId.value === raw.patientId) {
            this.loadHistory();
          }
        },
        error: (error) => {
          this.creating.set(false);
          this.notifications.error(
            error?.status === 401
              ? 'Sign in to make an appointment'
              : extractErrorMessage(error, 'Ошибка при создании записи.'),
          );
        },
      });
  }

  // ==================== История записей пациента ====================

  loadHistory(): void {
    if (this.historyForm.invalid) {
      this.historyForm.markAllAsTouched();
      return;
    }

    this.historyLoading.set(true);
    this.appointmentsApi.getPatientHistory(this.historyForm.controls.patientId.value).subscribe({
      next: (history) => {
        this.history.set(history);
        this.historyLoading.set(false);
      },
      error: (error) => {
        this.historyLoading.set(false);
        this.notifications.error(extractErrorMessage(error, 'Ошибка загрузки истории записей.'));
      },
    });
  }

  async cancelAppointment(appointment: Appointment): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Отменить запись?',
      message: 'Отменить эту запись?',
      confirmText: 'Да',
      cancelText: 'Нет',
      danger: true,
    });
    if (!confirmed) return;

    this.appointmentsApi.cancel(appointment.id).subscribe({
      next: () => {
        this.notifications.success('Запись отменена.');
        this.loadHistory();
      },
      error: (error) =>
        this.notifications.error(extractErrorMessage(error, 'Не удалось отменить запись.')),
    });
  }

  // ==================== US-10: расписание врача ====================

  loadSchedule(): void {
    if (this.scheduleForm.invalid) {
      this.scheduleForm.markAllAsTouched();
      return;
    }

    const { doctorId, date } = this.scheduleForm.getRawValue();
    this.scheduleLoading.set(true);

    this.appointmentsApi.getDoctorSchedule(doctorId!, toIsoDate(date!)).subscribe({
      next: (schedule) => {
        this.schedule.set(schedule);
        this.scheduleLoading.set(false);
      },
      error: (error) => {
        this.scheduleLoading.set(false);
        this.notifications.error(extractErrorMessage(error, 'Ошибка загрузки расписания.'));
      },
    });
  }

  // ==================== US-58..US-62: заключения ====================

  viewResult(appointmentId: string): void {
    this.appointmentsApi.getResult(appointmentId).subscribe({
      next: (result) =>
        this.dialog.open<AppointmentResultViewDialog, AppointmentResultViewDialogData>(
          AppointmentResultViewDialog,
          { data: { appointmentId, result }, width: '520px', restoreFocus: false },
        ),
      error: (error) =>
        this.notifications.error(
          error?.status === 404
            ? 'По этому приёму ещё нет медицинского заключения.'
            : extractErrorMessage(error, 'Ошибка загрузки заключения.'),
        ),
    });
  }

  async createResult(appointmentId: string): Promise<void> {
    await this.saveResult(appointmentId, null);
  }

  editResult(appointmentId: string): void {
    this.appointmentsApi.getResult(appointmentId).subscribe({
      next: (result) => void this.saveResult(appointmentId, result),
      error: (error) =>
        this.notifications.error(extractErrorMessage(error, 'Ошибка загрузки заключения.')),
    });
  }

  private async saveResult(
    appointmentId: string,
    result: AppointmentResult | null,
  ): Promise<void> {
    const ref = this.dialog.open<
      AppointmentResultDialog,
      AppointmentResultDialogData,
      AppointmentResultFormValue | undefined
    >(AppointmentResultDialog, {
      data: { appointmentId, result },
      width: '480px',
      autoFocus: 'first-tabbable',
      restoreFocus: false,
    });

    const value = await firstValueFrom(ref.afterClosed());
    if (!value) return;

    const request$ = result
      ? this.appointmentsApi.updateResult(appointmentId, value)
      : this.appointmentsApi.createResult({ appointmentId, ...value });

    request$.subscribe({
      next: () => {
        this.notifications.success(result ? 'Заключение обновлено.' : 'Заключение создано.');
        this.loadSchedule();
      },
      error: (error) =>
        this.notifications.error(extractErrorMessage(error, 'Ошибка при сохранении заключения.')),
    });
  }

  // ==================== Хелперы для шаблона ====================

  formatDate(value: string | Date): string {
    return formatRuDate(value);
  }

  doctorLabel(doctor: DoctorProfile): string {
    return `${doctor.lastName} ${doctor.firstName} ${doctor.middleName}`.trim();
  }

  officeLabel(office: Office): string {
    return `${office.city}, ${office.street} ${office.houseNumber} (оф. ${office.officeNumber})`;
  }

  serviceLabel(service: MedicalService): string {
    return `${service.name} (${service.price} BYN)`;
  }
}
