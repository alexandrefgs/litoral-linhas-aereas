import { Route, Routes } from '@angular/router';
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
  { path: 'dashboard', canActivate: [authGuard], loadComponent: () => import('./pages/dashboard/dashboard').then(m => m.Dashboard) },
  { path: 'profile', canActivate: [authGuard], loadComponent: () => import('./pages/profile/profile').then(m => m.Profile) },
  { path: 'profile/:number', canActivate: [authGuard], loadComponent: () => import('./pages/profile/profile').then(m => m.Profile) },
  { path: 'book', canActivate: [authGuard], loadComponent: () => import('./pages/book/book').then(m => m.Book) },
  { path: 'briefing', canActivate: [authGuard], loadComponent: () => import('./pages/briefing/briefing').then(m => m.Briefing) },
  { path: 'fleet', canActivate: [authGuard], loadComponent: () => import('./pages/fleet/fleet').then(m => m.Fleet) },
  { path: 'pireps', canActivate: [authGuard], loadComponent: () => import('./pages/pireps/pireps').then(m => m.Pireps) },
  { path: 'pireps/:id', canActivate: [authGuard], loadComponent: () => import('./pages/pireps/pirep-detail').then(m => m.PirepDetail) },
    { path: 'special-flights', canActivate: [authGuard], loadComponent: () => import('./pages/special-flights/special-flights').then(m => m.SpecialFlights) },
  { path: 'airports', canActivate: [authGuard], loadComponent: () => import('./pages/airports/airports').then(m => m.Airports) },
  { path: 'points', canActivate: [authGuard], loadComponent: () => import('./pages/points/points').then(m => m.Points) },
  comingSoon('statistics', 'Statistics', 'ti-chart-bar'),
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