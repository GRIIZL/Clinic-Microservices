import { Component, computed, input } from '@angular/core';

type BadgeTone = 'success' | 'warning' | 'info' | 'danger' | 'muted';

const TONES: Record<string, BadgeTone> = {
  Active: 'success',
  'At work': 'success',
  Completed: 'success',
  Pending: 'warning',
  'On vacation': 'warning',
  'Sick leave': 'warning',
  Approved: 'info',
  Canceled: 'danger',
  Inactive: 'muted',
};

@Component({
  selector: 'app-status-badge',
  template: `<span class="badge" [class]="tone()">{{ status() }}</span>`,
  styles: `
    .badge {
      display: inline-block;
      padding: 3px 8px;
      border-radius: 4px;
      font-size: 12px;
      font-weight: 700;
      line-height: 1.4;
      white-space: nowrap;
    }

    .success {
      background: #dcfce7;
      color: #15803d;
    }

    .warning {
      background: #fef9c3;
      color: #a16207;
    }

    .info {
      background: #dbeafe;
      color: #1d4ed8;
    }

    .danger {
      background: #fee2e2;
      color: #b91c1c;
    }

    .muted {
      background: #f1f5f9;
      color: #475569;
    }
  `,
})
export class StatusBadge {
  readonly status = input.required<string>();

  readonly tone = computed<BadgeTone>(() => TONES[this.status()] ?? 'muted');
}
