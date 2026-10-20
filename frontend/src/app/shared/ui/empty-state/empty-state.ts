import { Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  template: `
    <div class="empty-state">
      <span class="material-symbols-outlined">{{ icon() }}</span>
      <p>{{ message() }}</p>
    </div>
  `,
  styles: `
    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 8px;
      padding: 28px 16px;
      color: #64748b;
      text-align: center;
    }

    .material-symbols-outlined {
      font-size: 32px;
      opacity: 0.7;
    }

    p {
      margin: 0;
      font-size: 0.9rem;
    }
  `,
})
export class EmptyState {
  readonly message = input.required<string>();
  readonly icon = input('inbox');
}
