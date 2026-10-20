import { Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { UserRole } from '../../core/models/auth.models';

interface NavItem {
  label: string;
  path: string;
  roles: UserRole[];
}

const NAV_ITEMS: NavItem[] = [
  { label: 'Главная', path: '/', roles: ['Patient', 'Doctor', 'Receptionist', 'Admin'] },
  { label: 'Мои записи', path: '/my', roles: ['Patient'] },
  { label: 'Записи', path: '/appointments', roles: ['Doctor', 'Receptionist', 'Admin'] },
  { label: 'Пациенты', path: '/patients', roles: ['Receptionist', 'Admin'] },
  { label: 'Офисы', path: '/offices', roles: ['Receptionist', 'Admin'] },
  { label: 'Услуги', path: '/services', roles: ['Receptionist', 'Admin'] },
];

const ROLE_LABELS: Record<UserRole, string> = {
  Patient: 'Пациент',
  Doctor: 'Врач',
  Receptionist: 'Ресепшн',
  Admin: 'Администратор',
};

@Component({
  selector: 'app-main-layout',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatMenuModule,
    MatDividerModule,
  ],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
})
export class MainLayout {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly user = this.auth.currentUser;
  readonly roleLabel = computed(() => {
    const role = this.auth.role();
    return role ? ROLE_LABELS[role] : '';
  });

  readonly navItems = computed(() => {
    const role = this.auth.role();
    if (!role) return [];
    return NAV_ITEMS.filter((item) => item.roles.includes(role));
  });

  logout(): void {
    this.auth.logout().subscribe(() => this.router.navigate(['/login']));
  }
}
