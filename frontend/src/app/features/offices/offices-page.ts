import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { firstValueFrom } from 'rxjs';
import { Office, OfficeFormValue } from '../../core/models/office.models';
import { OfficeService } from '../../core/services/office.service';
import { NotificationService } from '../../core/services/notification.service';
import { extractErrorMessage } from '../../core/interceptors/error.interceptor';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { PageHeader } from '../../shared/ui/page-header/page-header';
import { StatusBadge } from '../../shared/ui/status-badge/status-badge';
import { OfficeDialog, OfficeDialogData } from './office-dialog/office-dialog';

@Component({
  selector: 'app-offices-page',
  imports: [MatButtonModule, MatProgressSpinnerModule, PageHeader, EmptyState, StatusBadge],
  templateUrl: './offices-page.html',
  styleUrl: './offices-page.scss',
})
export class OfficesPage implements OnInit {
  private readonly officesApi = inject(OfficeService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  readonly offices = signal<Office[]>([]);
  readonly loading = signal(false);
  readonly togglingId = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.officesApi.getAll().subscribe({
      next: (offices) => {
        this.offices.set(offices);
        this.loading.set(false);
      },
      error: (error) => {
        this.loading.set(false);
        this.notifications.error(extractErrorMessage(error, 'Не удалось загрузить офисы.'));
      },
    });
  }

  address(office: Office): string {
    return `${office.city}, ${office.street} str., h. ${office.houseNumber}, off. ${office.officeNumber}`;
  }

  async create(): Promise<void> {
    const value = await this.openDialog(null);
    if (!value) return;

    this.officesApi.create(value).subscribe({
      next: () => {
        this.notifications.success('Офис создан.');
        this.load();
      },
      error: (error) =>
        this.notifications.error(extractErrorMessage(error, 'Не удалось создать офис.')),
    });
  }

  async edit(office: Office): Promise<void> {
    const value = await this.openDialog(office);
    if (!value) return;

    this.officesApi.update(office.id, value).subscribe({
      next: () => {
        this.notifications.success('Офис обновлён.');
        this.load();
      },
      error: (error) =>
        this.notifications.error(extractErrorMessage(error, 'Не удалось обновить офис.')),
    });
  }

  toggleStatus(office: Office): void {
    const status = office.status === 'Active' ? 'Inactive' : 'Active';
    this.togglingId.set(office.id);

    this.officesApi.changeStatus(office.id, status).subscribe({
      next: () => {
        this.togglingId.set(null);
        this.load();
      },
      error: (error) => {
        this.togglingId.set(null);
        this.notifications.error(extractErrorMessage(error, 'Не удалось изменить статус офиса.'));
      },
    });
  }

  private async openDialog(office: Office | null): Promise<OfficeFormValue | undefined> {
    const ref = this.dialog.open<OfficeDialog, OfficeDialogData, OfficeFormValue | undefined>(
      OfficeDialog,
      {
        data: { office },
        width: '420px',
        autoFocus: 'first-tabbable',
        restoreFocus: false,
      },
    );

    return firstValueFrom(ref.afterClosed());
  }
}
