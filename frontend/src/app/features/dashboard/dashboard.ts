import { Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { UserRole } from '../../core/models/auth.models';

interface QuickLink {
  title: string;
  description: string;
  path: string;
  icon: string;
  roles: UserRole[];
}

const QUICK_LINKS: QuickLink[] = [
  {
    title: 'Мои записи',
    description: 'История визитов, отмена и перенос записи.',
    path: '/my',
    icon: 'event_note',
    roles: ['Patient'],
  },
  {
    title: 'Записаться на приём',
    description: 'Выбрать специализацию, услугу, врача и свободный слот.',
    path: '/appointments',
    icon: 'add_circle',
    roles: ['Patient'],
  },
  {
    title: 'Записи на приём',
    description: 'История пациента, расписание врача и медицинские заключения.',
    path: '/appointments',
    icon: 'calendar_month',
    roles: ['Doctor', 'Receptionist', 'Admin'],
  },
  {
    title: 'Картотека пациентов',
    description: 'Поиск пациентов, регистрация и реестр врачей.',
    path: '/patients',
    icon: 'group',
    roles: ['Receptionist', 'Admin'],
  },
  {
    title: 'Офисы',
    description: 'Адреса, телефоны регистратуры и статусы офисов.',
    path: '/offices',
    icon: 'apartment',
    roles: ['Receptionist', 'Admin'],
  },
  {
    title: 'Услуги',
    description: 'Специализации, услуги, цены и категории.',
    path: '/services',
    icon: 'medical_services',
    roles: ['Receptionist', 'Admin'],
  },
];

@Component({
  selector: 'app-dashboard',
  imports: [MatButtonModule, RouterLink],
  template: `
    <section class="dashboard">
      <div class="hero">
        <div>
          <h1>Здравствуйте!</h1>
          <p>Вы вошли как <strong>{{ user()?.email }}</strong> ({{ roleLabel() }}).</p>
        </div>
        <span class="material-symbols-outlined hero-icon">health_and_safety</span>
      </div>

      <div class="cards">
        @for (link of links(); track link.path + link.title) {
          <a class="card" [routerLink]="link.path">
            <span class="material-symbols-outlined">{{ link.icon }}</span>
            <h2>{{ link.title }}</h2>
            <p>{{ link.description }}</p>
            <span class="card-link">Перейти →</span>
          </a>
        }
      </div>
    </section>
  `,
  styles: `
    .dashboard {
      display: flex;
      flex-direction: column;
      gap: 28px;
    }

    .hero {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      padding: 28px;
      border-radius: 16px;
      background: linear-gradient(135deg, #e0f2fe 0%, #f0fdf4 100%);
    }

    .hero h1 {
      margin: 0 0 6px;
      font-size: 1.6rem;
      color: #0f172a;
    }

    .hero p {
      margin: 0;
      color: #475569;
    }

    .hero-icon {
      font-size: 64px;
      color: #1a73e8;
      opacity: 0.75;
    }

    .cards {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
      gap: 18px;
    }

    .card {
      display: flex;
      flex-direction: column;
      gap: 8px;
      padding: 22px;
      border-radius: 14px;
      background: var(--mat-sys-surface-container-low);
      border: 1px solid var(--mat-sys-outline-variant);
      text-decoration: none;
      color: inherit;
      transition: transform 0.15s, box-shadow 0.15s;
    }

    .card:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 20px rgba(15, 23, 42, 0.08);
    }

    .card .material-symbols-outlined {
      color: #1a73e8;
      font-size: 30px;
    }

    .card h2 {
      margin: 0;
      font-size: 1.05rem;
      color: #0f172a;
    }

    .card p {
      margin: 0;
      font-size: 0.85rem;
      color: #64748b;
      line-height: 1.5;
      flex: 1;
    }

    .card-link {
      font-size: 0.82rem;
      font-weight: 600;
      color: #1a73e8;
    }

    @media (max-width: 720px) {
      .hero-icon {
        display: none;
      }
    }
  `,
})
export class DashboardPage {
  private readonly auth = inject(AuthService);

  readonly user = this.auth.currentUser;

  readonly roleLabel = computed(() => {
    switch (this.auth.role()) {
      case 'Patient':
        return 'пациент';
      case 'Doctor':
        return 'врач';
      case 'Receptionist':
        return 'ресепшн';
      case 'Admin':
        return 'администратор';
      default:
        return '';
    }
  });

  readonly links = computed(() => {
    const role = this.auth.role();
    if (!role) return [];
    return QUICK_LINKS.filter((link) => link.roles.includes(role));
  });
}
