import { Route, Routes } from '@angular/router';
import { authGuard, guestGuard, staffGuard } from './core/auth/auth.guards';

function comingSoon(path: string, titleKey: string, icon: string): Route {
  return {
    path,
    canActivate: [authGuard],
    data: { titleKey, icon },
    loadComponent: () => import('./pages/coming-soon/coming-soon').then(m => m.ComingSoon)
  };
}

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/home/home').then(m => m.Home) },
  { path: 'login', canActivate: [guestGuard], loadComponent: () => import('./pages/auth/login/login').then(m => m.Login) },
  { path: 'register', canActivate: [guestGuard], loadComponent: () => import('./pages/auth/register/register').then(m => m.Register) },
  { path: 'dashboard', canActivate: [authGuard], loadComponent: () => import('./pages/dashboard/dashboard').then(m => m.Dashboard) },
  { path: 'profile', canActivate: [authGuard], loadComponent: () => import('./pages/profile/profile').then(m => m.Profile) },
  comingSoon('book', 'nav.book', 'ti-world-search'),
  comingSoon('routes', 'nav.routes', 'ti-route'),
  comingSoon('airports', 'nav.airports', 'ti-building-airport'),
  comingSoon('fleet', 'nav.fleet', 'ti-plane'),
  comingSoon('points', 'nav.points', 'ti-star'),
  comingSoon('statistics', 'nav.statistics', 'ti-chart-bar'),
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
      }
    ]
  },
  { path: '**', redirectTo: '' }
];