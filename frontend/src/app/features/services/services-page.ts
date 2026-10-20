import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { firstValueFrom } from 'rxjs';
import {
  MedicalService,
  MedicalServiceFormValue,
  ServiceStatus,
  Specialization,
  SpecializationFormValue,
} from '../../core/models/service.models';
import { SpecializationService } from '../../core/services/specialization.service';
import { NotificationService } from '../../core/services/notification.service';
import { ConfirmDialogService } from '../../core/services/confirm-dialog.service';
import { extractErrorMessage } from '../../core/interceptors/error.interceptor';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { PageHeader } from '../../shared/ui/page-header/page-header';
import { StatusBadge } from '../../shared/ui/status-badge/status-badge';
import {
  SpecializationDialog,
  SpecializationDialogData,
} from './specialization-dialog/specialization-dialog';
import {
  MedicalServiceDialog,
  MedicalServiceDialogData,
} from './medical-service-dialog/medical-service-dialog';

@Component({
  selector: 'app-services-page',
  imports: [MatButtonModule, MatProgressSpinnerModule, PageHeader, EmptyState, StatusBadge],
  templateUrl: './services-page.html',
  styleUrl: './services-page.scss',
})
export class ServicesPage implements OnInit {
  private readonly servicesApi = inject(SpecializationService);
  private readonly notifications = inject(NotificationService);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly dialog = inject(MatDialog);

  readonly specializations = signal<Specialization[]>([]);
  readonly loading = signal(false);
  readonly servicesLoading = signal(false);

  /** Выбранная специализация: показываем её услуги отдельным блоком (как в legacy). */
  readonly selected = signal<Specialization | null>(null);

  ngOnInit(): void {
    this.loadSpecializations();
  }

  loadSpecializations(): void {
    this.loading.set(true);
    this.servicesApi.getAll().subscribe({
      next: (specializations) => {
        this.specializations.set(specializations);
        this.loading.set(false);
      },
      error: (error) => {
        this.loading.set(false);
        this.notifications.error(extractErrorMessage(error, 'Ошибка загрузки специализаций.'));
      },
    });
  }

  servicesCount(specialization: Specialization): number {
    return specialization.services?.length ?? 0;
  }

  async createSpecialization(): Promise<void> {
    const value = await this.openSpecializationDialog(null);
    if (!value) return;

    this.servicesApi.create(value).subscribe({
      next: () => {
        this.notifications.success('Специализация создана.');
        this.loadSpecializations();
      },
      error: (error) =>
        this.notifications.error(extractErrorMessage(error, 'Ошибка при сохранении специализации.')),
    });
  }

  async editSpecialization(specialization: Specialization): Promise<void> {
    const value = await this.openSpecializationDialog(specialization);
    if (!value) return;

    this.servicesApi.update(specialization.id, value).subscribe({
      next: () => {
        this.notifications.success('Специализация обновлена.');
        this.loadSpecializations();
        if (this.selected()?.id === specialization.id) {
          this.openServices(specialization);
        }
      },
      error: (error) =>
        this.notifications.error(extractErrorMessage(error, 'Ошибка при сохранении специализации.')),
    });
  }

  async toggleStatus(specialization: Specialization): Promise<void> {
    const status: ServiceStatus = specialization.status === 'Active' ? 'Inactive' : 'Active';

    const confirmed = await this.confirmDialog.confirm({
      title: 'Изменить статус специализации?',
      message: `Изменить статус специализации на '${status}'?`,
      confirmText: 'Да',
      cancelText: 'Нет',
    });
    if (!confirmed) return;

    this.servicesApi.changeStatus(specialization.id, status).subscribe({
      next: () => {
        this.loadSpecializations();
        if (this.selected()?.id === specialization.id) {
          this.openServices(specialization);
        }
      },
      error: (error) =>
        this.notifications.error(extractErrorMessage(error, 'Не удалось изменить статус.')),
    });
  }

  openServices(specialization: Specialization): void {
    this.servicesLoading.set(true);
    this.servicesApi.getById(specialization.id).subscribe({
      next: (full) => {
        this.selected.set(full);
        this.servicesLoading.set(false);
      },
      error: (error) => {
        this.servicesLoading.set(false);
        this.notifications.error(extractErrorMessage(error, 'Ошибка загрузки услуг.'));
      },
    });
  }

  closeServices(): void {
    this.selected.set(null);
  }

  async createService(): Promise<void> {
    const specialization = this.selected();
    if (!specialization) return;

    const value = await this.openServiceDialog(null);
    if (!value) return;

    this.servicesApi.createService(specialization.id, value).subscribe({
      next: () => {
        this.notifications.success('Услуга добавлена.');
        this.refreshSelected();
      },
      error: (error) =>
        this.notifications.error(extractErrorMessage(error, 'Ошибка при сохранении услуги.')),
    });
  }

  async editService(service: MedicalService): Promise<void> {
    const specialization = this.selected();
    if (!specialization) return;

    const value = await this.openServiceDialog(service);
    if (!value) return;

    this.servicesApi.updateService(specialization.id, service.id, value).subscribe({
      next: () => {
        this.notifications.success('Услуга обновлена.');
        this.refreshSelected();
      },
      error: (error) =>
        this.notifications.error(extractErrorMessage(error, 'Ошибка при сохранении услуги.')),
    });
  }

  private refreshSelected(): void {
    const specialization = this.selected();
    if (specialization) this.openServices(specialization);
    this.loadSpecializations();
  }

  private async openSpecializationDialog(
    specialization: Specialization | null,
  ): Promise<SpecializationFormValue | undefined> {
    const ref = this.dialog.open<
      SpecializationDialog,
      SpecializationDialogData,
      SpecializationFormValue | undefined
    >(SpecializationDialog, {
      data: { specialization },
      width: '400px',
      autoFocus: 'first-tabbable',
      restoreFocus: false,
    });

    return firstValueFrom(ref.afterClosed());
  }

  private async openServiceDialog(
    service: MedicalService | null,
  ): Promise<MedicalServiceFormValue | undefined> {
    const ref = this.dialog.open<
      MedicalServiceDialog,
      MedicalServiceDialogData,
      MedicalServiceFormValue | undefined
    >(MedicalServiceDialog, {
      data: { service },
      width: '420px',
      autoFocus: 'first-tabbable',
      restoreFocus: false,
    });

    return firstValueFrom(ref.afterClosed());
  }
}
