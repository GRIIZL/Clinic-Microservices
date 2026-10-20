import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { firstValueFrom } from 'rxjs';
import {
  CreatePatientRequest,
  DoctorProfile,
  PatientProfile,
} from '../../core/models/profile.models';
import { AuthService } from '../../core/services/auth.service';
import { DoctorService } from '../../core/services/doctor.service';
import { PatientService } from '../../core/services/patient.service';
import { NotificationService } from '../../core/services/notification.service';
import { ConfirmDialogService } from '../../core/services/confirm-dialog.service';
import { extractErrorMessage } from '../../core/interceptors/error.interceptor';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { PageHeader } from '../../shared/ui/page-header/page-header';
import { StatusBadge } from '../../shared/ui/status-badge/status-badge';
import { PatientDialog, PatientDialogData } from './patient-dialog/patient-dialog';
import { MatchDialog, MatchDialogData, MatchDialogResult } from './match-dialog/match-dialog';
import {
  DoctorProfileDialog,
  DoctorProfileDialogData,
} from './doctor-profile-dialog/doctor-profile-dialog';

@Component({
  selector: 'app-patients-page',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    PageHeader,
    EmptyState,
    StatusBadge,
  ],
  templateUrl: './patients-page.html',
  styleUrl: './patients-page.scss',
})
export class PatientsPage implements OnInit {
  private readonly patientsApi = inject(PatientService);
  private readonly doctorsApi = inject(DoctorService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly dialog = inject(MatDialog);

  readonly patientSearch = new FormControl('', { nonNullable: true });
  readonly doctorSearch = new FormControl('', { nonNullable: true });

  readonly patients = signal<PatientProfile[]>([]);
  readonly doctors = signal<DoctorProfile[]>([]);

  readonly patientsLoading = signal(false);
  readonly doctorsLoading = signal(false);

  /** US-50: «Incorrect patient name», когда поиск не дал совпадений. */
  readonly patientSearchError = signal(false);
  readonly doctorSearchEmpty = signal(false);

  ngOnInit(): void {
    this.loadPatients();
    this.loadDoctors();
  }

  loadPatients(): void {
    const name = this.patientSearch.value.trim();
    this.patientsLoading.set(true);
    this.patientSearchError.set(false);

    this.patientsApi.getAll(name || undefined).subscribe({
      next: (patients) => {
        this.patients.set(patients);
        this.patientsLoading.set(false);
      },
      error: (error) => {
        this.patientsLoading.set(false);
        if (error?.status === 404) {
          this.patients.set([]);
          this.patientSearchError.set(true);
          return;
        }
        this.notifications.error(extractErrorMessage(error, 'Не удалось загрузить пациентов.'));
      },
    });
  }

  resetPatientSearch(): void {
    this.patientSearch.setValue('');
    this.loadPatients();
  }

  loadDoctors(): void {
    const name = this.doctorSearch.value.trim();
    this.doctorsLoading.set(true);
    this.doctorSearchEmpty.set(false);

    this.doctorsApi.getForAdmin(name ? { name } : {}).subscribe({
      next: (doctors) => {
        this.doctors.set(doctors);
        this.doctorsLoading.set(false);
      },
      error: (error) => {
        this.doctorsLoading.set(false);
        this.doctors.set([]);
        if (error?.status === 404) {
          this.doctorSearchEmpty.set(true);
          return;
        }
        this.notifications.error(extractErrorMessage(error, 'Не удалось загрузить врачей.'));
      },
    });
  }

  async registerPatient(): Promise<void> {
    const request = await this.openPatientDialog();
    if (!request) return;

    this.patientsApi.create(request).subscribe({
      next: async (response) => {
        if (response.matchFound && response.existingProfileId && response.existingProfileData) {
          await this.handleMatch(response.existingProfileId, response.existingProfileData, request);
          return;
        }

        this.notifications.success('Пациент зарегистрирован.');
        this.loadPatients();
      },
      error: (error) =>
        this.notifications.error(extractErrorMessage(error, 'Не удалось создать профиль пациента.')),
    });
  }

  async deletePatient(patient: PatientProfile): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Удалить пациента?',
      message: 'Do you really want to delete? Data will be lost',
      confirmText: 'Yes',
      cancelText: 'No',
      danger: true,
    });
    if (!confirmed) return;

    this.patientsApi.delete(patient.id).subscribe({
      next: () => {
        this.notifications.success('Профиль пациента удалён.');
        this.loadPatients();
      },
      error: (error) =>
        this.notifications.error(extractErrorMessage(error, 'Не удалось удалить пациента.')),
    });
  }

  async viewDoctor(doctor: DoctorProfile): Promise<void> {
    this.doctorsApi.getById(doctor.id).subscribe({
      next: (full) => {
        this.dialog.open<DoctorProfileDialog, DoctorProfileDialogData>(DoctorProfileDialog, {
          data: { doctor: full },
          width: '480px',
          restoreFocus: false,
        });
      },
      error: (error) =>
        this.notifications.error(extractErrorMessage(error, 'Не удалось загрузить профиль врача.')),
    });
  }

  fullName(patient: PatientProfile): string {
    return [patient.lastName, patient.firstName, patient.middleName].filter(Boolean).join(' ');
  }

  formatDate(value: string): string {
    if (!value) return '—';
    return new Date(value).toLocaleDateString('ru-RU');
  }

  private async handleMatch(
    profileId: string,
    existing: MatchDialogData['existing'],
    request: CreatePatientRequest,
  ): Promise<void> {
    const ref = this.dialog.open<MatchDialog, MatchDialogData, MatchDialogResult>(MatchDialog, {
      data: { existing },
      width: '420px',
      disableClose: true,
      restoreFocus: false,
    });

    const action = await firstValueFrom(ref.afterClosed());

    if (action === 'link') {
      const accountId = this.auth.currentUser()?.accountId;
      if (!accountId) {
        this.notifications.error('Не удалось определить аккаунт для привязки профиля.');
        return;
      }

      this.patientsApi.linkAccount(profileId, accountId).subscribe({
        next: () => {
          this.notifications.success('Профиль привязан к вашему аккаунту.');
          this.loadPatients();
        },
        error: (error) =>
          this.notifications.error(extractErrorMessage(error, 'Could not link account.')),
      });
      return;
    }

    // «No, create new» — принудительно создаём дубликат профиля.
    this.patientsApi.create({ ...request, accountId: null }).subscribe({
      next: () => {
        this.notifications.success('Создан новый профиль пациента.');
        this.loadPatients();
      },
      error: (error) =>
        this.notifications.error(
          extractErrorMessage(error, 'Could not create the duplicate profile.'),
        ),
    });
  }

  private async openPatientDialog(): Promise<CreatePatientRequest | undefined> {
    const ref = this.dialog.open<PatientDialog, PatientDialogData, CreatePatientRequest | undefined>(
      PatientDialog,
      {
        data: { patient: null, accountId: this.auth.currentUser()?.accountId ?? null },
        width: '420px',
        autoFocus: 'first-tabbable',
        restoreFocus: false,
      },
    );

    return firstValueFrom(ref.afterClosed());
  }
}
