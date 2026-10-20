import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';
import { ConfirmDialog, ConfirmDialogData } from '../../shared/ui/confirm-dialog/confirm-dialog';

@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  private readonly dialog = inject(MatDialog);

  /** Возвращает true, если пользователь подтвердил действие. */
  async confirm(data: ConfirmDialogData): Promise<boolean> {
    const ref = this.dialog.open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, {
      data,
      width: '380px',
      autoFocus: 'dialog',
      restoreFocus: false,
    });

    return (await firstValueFrom(ref.afterClosed())) === true;
  }

  /** Стандартный диалог «отменить редактирование?» — как в legacy-страницах. */
  confirmDiscard(): Promise<boolean> {
    return this.confirm({
      title: 'Отменить изменения?',
      message: 'Do you really want to cancel? Changes will not be saved.',
      confirmText: 'Yes',
      cancelText: 'No',
    });
  }
}
