import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { firstValueFrom } from 'rxjs';
import {
  Appointment,
  AppointmentResult,
  RescheduleAppointmentRequest,
} from '../../core/models/appointment.models';
import { DoctorProfile, PatientProfile } from '../../core/models/profile.models';
import { Specialization } from '../../core/models/service.models';
import { AppointmentService } from '../../core/services/appointment.service';
import { AuthService } from '../../core/services/auth.service';
import { DoctorService } from '../../core/services/doctor.service';
import { PatientService } from '../../core/services/patient.service';
import { SpecializationService } from '../../core/services/specialization.service';
import { NotificationService } from '../../core/services/notification.service';
import { ConfirmDialogService } from '../../core/services/confirm-dialog.service';
import { extractErrorMessage } from '../../core/interceptors/error.interceptor';
import { formatRuDate } from '../../core/utils/date.utils';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { PageHeader } from '../../shared/ui/page-header/page-header';
import { StatusBadge } from '../../shared/ui/status-badge/status-badge';
import {
  AppointmentResultViewDialog,
  AppointmentResultViewDialogData,
} from '../appointments/appointment-result-view-dialog/appointment-result-view-dialog';
import { RescheduleDialog, RescheduleDialogData } from './reschedule-dialog/reschedule-dialog';

@Component({
  selector: 'app-patient-cabinet',
  imports: [MatButtonModule, MatProgressSpinnerModule, PageHeader, EmptyState, StatusBadge],
  templateUrl: './patient-cabinet.html',
  styleUrl: './patient-cabinet.scss',
})
export class PatientCabinetPage implements OnInit {
  private readonly appointmentsApi = inject(AppointmentService);
  private readonly patientsApi = inject(PatientService);
  private readonly doctorsApi = inject(DoctorService);
  private readonly specializationsApi = inject(SpecializationService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly dialog = inject(MatDialog);

  readonly profile = signal<PatientProfile | null>(null);
  readonly appointments = signal<Appointment[]>([]);
  readonly loading = signal(true);
  readonly profileMissing = signal(false);

  private readonly doctors = signal<DoctorProfile[]>([]);
  private readonly specializations = signal<Specialization[]>([]);

  ngOnInit(): void {
    this.loadDoctors();
    this.loadSpecializations();
    this.loadProfile();
  }

  loadProfile(): void {
    const accountId = this.auth.currentUser()?.accountId;
    if (!accountId) {
      this.loading.set(false);
      this.profileMissing.set(true);
      return;
    }

    this.patientsApi.getByAccountId(accountId).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.loadHistory(profile.id);
      },
      error: (error) => {
        this.loading.set(false);
        if (error?.status === 404) {
          this.profileMissing.set(true);
          return;
        }
        this.notifications.error(extractErrorMessage(error, 'Не удалось загрузить профиль.'));
      },
    });
  }

  loadHistory(patientId: string): void {
    this.loading.set(true);
    this.appointmentsApi.getPatientHistory(patientId).subscribe({
      next: (appointments) => {
        this.appointments.set(appointments);
        this.loading.set(false);
      },
      error: (error) => {
        this.loading.set(false);
        this.notifications.error(extractErrorMessage(error, 'Не удалось загрузить записи.'));
      },
    });
  }

  /** AC-1: перенос доступен только для записей в статусе Pending. */
  canReschedule(appointment: Appointment): boolean {
    return appointment.status === 'Pending';
  }

  async reschedule(appointment: Appointment): Promise<void> {
    const specialization = this.specializations().find(
      (s) => s.id === appointment.specializationId,
    );
    const service = specialization?.services.find((s) => s.id === appointment.serviceId);

    if (!service) {
      this.notifications.error(
        'Не удалось определить услугу записи. Обратитесь в регистратуру.',
      );
      return;
    }

    const ref = this.dialog.open<
      RescheduleDialog,
      RescheduleDialogData,
      RescheduleAppointmentRequest | undefined
    >(RescheduleDialog, {
      data: {
        appointment,
        doctors: this.filterDoctors(appointment, specialization),
        categoryName: service.categoryName,
      },
      width: '460px',
      autoFocus: 'first-tabbable',
      restoreFocus: false,
    });

    const request = await firstValueFrom(ref.afterClosed());
    if (!request) return;

    this.appointmentsApi.reschedule(appointment.id, request).subscribe({
      next: () => {
        this.notifications.success('Запись перенесена.');
        const profile = this.profile();
        if (profile) this.loadHistory(profile.id);
      },
      error: (error) => {
        if (error?.status === 409) {
          this.notifications.error(
            extractErrorMessage(
              error,
              'Перенос невозможен: запись уже подтверждена или слот занят.',
            ),
          );
          return;
        }
        this.notifications.error(extractErrorMessage(error, 'Не удалось перенести запись.'));
      },
    });
  }

  async cancelAppointment(appointment: Appointment): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Отменить запись?',
      message: `Отменить приём ${formatRuDate(appointment.date)} в ${appointment.timeslot}?`,
      confirmText: 'Да',
      cancelText: 'Нет',
      danger: true,
    });
    if (!confirmed) return;

    this.appointmentsApi.cancel(appointment.id).subscribe({
      next: () => {
        this.notifications.success('Запись отменена.');
        const profile = this.profile();
        if (profile) this.loadHistory(profile.id);
      },
      error: (error) =>
        this.notifications.error(extractErrorMessage(error, 'Не удалось отменить запись.')),
    });
  }

  viewResult(appointmentId: string): void {
    this.appointmentsApi.getResult(appointmentId).subscribe({
      next: (result: AppointmentResult) =>
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

  formatDate(value: string): string {
    return formatRuDate(value);
  }

  doctorName(doctorId: string): string {
    const doctor = this.doctors().find((d) => d.id === doctorId);
    return doctor ? `${doctor.lastName} ${doctor.firstName}` : '—';
  }

  serviceName(specializationId: string, serviceId: string): string {
    const specialization = this.specializations().find((s) => s.id === specializationId);
    return specialization?.services.find((s) => s.id === serviceId)?.name ?? '—';
  }

  private loadDoctors(): void {
    this.doctorsApi.getForPatients().subscribe({
      next: (doctors) => this.doctors.set(doctors),
      error: () => this.doctors.set([]),
    });
  }

  private loadSpecializations(): void {
    this.specializationsApi.getAll().subscribe({
      next: (specializations) => this.specializations.set(specializations),
      error: () => this.specializations.set([]),
    });
  }

  /** F-2: врачи только по специализации и офису переносимой записи. */
  private filterDoctors(
    appointment: Appointment,
    specialization: Specialization | undefined,
  ): DoctorProfile[] {
    return this.doctors().filter((doctor) => {
      const sameSpecialization = specialization
        ? doctor.specialization.toLowerCase() === specialization.name.toLowerCase()
        : true;
      const sameOffice = appointment.officeId ? doctor.officeId === appointment.officeId : true;

      return sameSpecialization && sameOffice;
    });
  }
}
