import { inject } from '@angular/core';
import { Route, Router, Routes } from '@angular/router';
import { authGuard, guestGuard, staffGuard } from './core/auth/auth.guards';

function comingSoon(path: string, title: string, icon: string): Route {
  return {
    path,
    canActivate: [authGuard],
    data: { title, icon },
    loadComponent: () => import('./pages/coming-soon/coming-soon').then(m => m.ComingSoon)
  };
}

export const routes: Routes = [
  { path: '', canActivate: [guestGuard], loadComponent: () => import('./pages/home/home').then(m => m.Home) },
  { path: 'login', canActivate: [guestGuard], loadComponent: () => import('./pages/auth/login/login').then(m => m.Login) },
  { path: 'register', canActivate: [guestGuard], loadComponent: () => import('./pages/auth/register/register').then(m => m.Register) },
  { path: 'regulations', data: { kind: 'regulations' }, loadComponent: () => import('./pages/legal/legal-page').then(m => m.LegalPage) },
  { path: 'privacy', data: { kind: 'privacy' }, loadComponent: () => import('./pages/legal/legal-page').then(m => m.LegalPage) },
  { path: 'dashboard', canActivate: [authGuard], loadComponent: () => import('./pages/dashboard/dashboard').then(m => m.Dashboard) },
  { path: 'profile', canActivate: [authGuard], loadComponent: () => import('./pages/profile/profile').then(m => m.Profile) },
  { path: 'profile/:number', canActivate: [authGuard], loadComponent: () => import('./pages/profile/profile').then(m => m.Profile) },
  { path: 'book', canActivate: [authGuard], loadComponent: () => import('./pages/book/book').then(m => m.Book) },
  { path: 'briefing', canActivate: [authGuard], loadComponent: () => import('./pages/briefing/briefing').then(m => m.Briefing) },
  { path: 'fleet', canActivate: [authGuard], loadComponent: () => import('./pages/fleet/fleet').then(m => m.Fleet) },
  { path: 'flights/:id', canActivate: [authGuard], loadComponent: () => import('./pages/flight/flight-detail').then(m => m.FlightDetail) },
  { path: 'pireps/:id', redirectTo: 'flights/:id' },
  { path: 'pireps', redirectTo: () => inject(Router).parseUrl('/profile?tab=flights') },
  { path: 'special-flights', canActivate: [authGuard], loadComponent: () => import('./pages/special-flights/special-flights').then(m => m.SpecialFlights) },
  { path: 'airports', canActivate: [authGuard], loadComponent: () => import('./pages/airports/airports').then(m => m.Airports) },
  { path: 'points', canActivate: [authGuard], loadComponent: () => import('./pages/points/points').then(m => m.Points) },
  { path: 'statistics', canActivate: [authGuard], loadComponent: () => import('./pages/statistics/statistics').then(m => m.Statistics) },
  {
    path: 'admin',
    canActivate: [staffGuard()],
    loadComponent: () => import('./pages/admin/admin-layout').then(m => m.AdminLayout),
    children: [
      { path: '', loadComponent: () => import('./pages/admin/admin-home').then(m => m.AdminHome) },
      {
        path: 'airports',
        canActivate: [staffGuard('routes')],
        loadComponent: () => import('./pages/admin/airports/admin-airports').then(m => m.AdminAirports)
      },
      {
        path: 'routes',
        canActivate: [staffGuard('routes')],
        loadComponent: () => import('./pages/admin/routes/admin-routes').then(m => m.AdminRoutes)
      },
      {
        path: 'fleet',
        canActivate: [staffGuard('fleet')],
        loadComponent: () => import('./pages/admin/fleet/admin-fleet').then(m => m.AdminFleet)
      },
      {
        path: 'pireps',
        canActivate: [staffGuard('pireps')],
        loadComponent: () => import('./pages/admin/pireps/admin-pireps').then(m => m.AdminPireps)
      },
      {
        path: 'pireps/:id',
        canActivate: [staffGuard('pireps')],
        loadComponent: () => import('./pages/admin/pireps/admin-pirep-review').then(m => m.AdminPirepReview)
      },
      {
        path: 'pilots',
        canActivate: [staffGuard('pilots')],
        loadComponent: () => import('./pages/admin/pilots/admin-pilots').then(m => m.AdminPilots)
      },
      {
        path: 'privacy',
        canActivate: [staffGuard('pilots')],
        loadComponent: () => import('./pages/admin/privacy/admin-privacy').then(m => m.AdminPrivacy)
      },
      {
        path: 'staff',
        canActivate: [staffGuard('staff')],
        loadComponent: () => import('./pages/admin/staff/admin-staff').then(m => m.AdminStaff)
      },
      {
        path: 'reports',
        canActivate: [staffGuard('reports')],
        loadComponent: () => import('./pages/admin/reports/admin-reports').then(m => m.AdminReports)
      },
      {
        path: 'logs',
        canActivate: [staffGuard('reports')],
        loadComponent: () => import('./pages/admin/logs/admin-logs').then(m => m.AdminLogs)
      },
      {
        path: 'operations',
        canActivate: [staffGuard('operations')],
        loadComponent: () => import('./pages/admin/operations/admin-operations').then(m => m.AdminOperations)
      },
      {
        path: 'scoring',
        canActivate: [staffGuard('pireps')],
        loadComponent: () => import('./pages/admin/scoring/admin-scoring').then(m => m.AdminScoring)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];