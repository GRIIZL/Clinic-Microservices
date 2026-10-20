import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/guards/auth.guards';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Вход — Медицинский портал',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.LoginPage),
  },
  {
    path: 'register',
    title: 'Регистрация — Медицинский портал',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register/register').then((m) => m.RegisterPage),
  },
  {
    path: 'verify-email',
    title: 'Подтверждение email',
    loadComponent: () =>
      import('./features/auth/verify-email/verify-email').then((m) => m.VerifyEmailPage),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/main-layout/main-layout').then((m) => m.MainLayout),
    children: [
      {
        path: '',
        title: 'Главная — Медицинский портал',
        loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.DashboardPage),
      },
      {
        path: 'my',
        title: 'Мои записи',
        canActivate: [roleGuard('Patient')],
        loadComponent: () =>
          import('./features/patient-cabinet/patient-cabinet').then((m) => m.PatientCabinetPage),
      },
      {
        path: 'appointments',
        title: 'Записи на приём',
        loadComponent: () =>
          import('./features/appointments/appointments-page').then((m) => m.AppointmentsPage),
      },
      {
        path: 'patients',
        title: 'Пациенты',
        canActivate: [roleGuard('Receptionist', 'Admin')],
        loadComponent: () =>
          import('./features/profiles/patients-page').then((m) => m.PatientsPage),
      },
      {
        path: 'offices',
        title: 'Офисы',
        canActivate: [roleGuard('Receptionist', 'Admin')],
        loadComponent: () => import('./features/offices/offices-page').then((m) => m.OfficesPage),
      },
      {
        path: 'services',
        title: 'Услуги',
        canActivate: [roleGuard('Receptionist', 'Admin')],
        loadComponent: () => import('./features/services/services-page').then((m) => m.ServicesPage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];

