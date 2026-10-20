import { Component, computed, input, output } from '@angular/core';

interface SlotView {
  start: string;
  label: string;
}

@Component({
  selector: 'app-slots-grid',
  template: `
    <div class="slots-grid">
      @if (loading()) {
        <span class="hint">Загрузка слотов...</span>
      } @else if (slots().length === 0) {
        <span class="hint">{{ emptyText() }}</span>
      } @else {
        @for (slot of slots(); track slot.start) {
          <button
            type="button"
            class="slot-btn"
            [class.selected]="selected() === slot.label"
            (click)="selectedChange.emit(slot.label)"
          >
            {{ slot.label }}
          </button>
        }
      }
    </div>
  `,
  styles: `
    .slots-grid {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
      min-height: 34px;
      align-items: center;
    }

    .hint {
      font-size: 13px;
      color: #64748b;
    }

    .slot-btn {
      padding: 6px 10px;
      font-size: 12px;
      font-weight: 600;
      border: 2px solid #cbd5e1;
      background: #fff;
      color: #334155;
      border-radius: 6px;
      cursor: pointer;
      transition: all 0.15s;
    }

    .slot-btn:hover {
      border-color: #1a73e8;
      color: #1a73e8;
    }

    .slot-btn.selected {
      background: #1a73e8;
      border-color: #1a73e8;
      color: #fff;
    }
  `,
})
export class SlotsGrid {
  /** Время начала слотов в формате "HH:mm". */
  readonly startTimes = input<string[]>([]);
  /** Сколько 10-минутных слотов занимает приём (приходит с бэкенда). */
  readonly requiredSlotsCount = input(0);
  readonly selected = input<string | null>(null);
  readonly loading = input(false);
  readonly emptyText = input('Укажите врача, услугу и дату');

  readonly selectedChange = output<string>();

  readonly slots = computed<SlotView[]>(() =>
    this.startTimes().map((start) => ({
      start,
      label: `${start} - ${this.calculateEnd(start, this.requiredSlotsCount())}`,
    })),
  );

  private calculateEnd(start: string, slotsCount: number): string {
    const [hours, minutes] = start.split(':').map(Number);
    const end = new Date(2000, 0, 1, hours, minutes + slotsCount * 10);
    return `${String(end.getHours()).padStart(2, '0')}:${String(end.getMinutes()).padStart(2, '0')}`;
  }
}
