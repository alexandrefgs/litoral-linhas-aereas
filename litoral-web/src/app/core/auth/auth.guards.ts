import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { StaffArea, hasStaffArea } from '../staff/staff-positions';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.isAuthenticated()
    ? true
    : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.isAuthenticated() ? router.createUrlTree(['/dashboard']) : true;
};

export function staffGuard(area?: StaffArea): CanActivateFn {
  return (_route, state) => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isAuthenticated())
      return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });

    if (auth.needsTwoFactor())
      return router.createUrlTree(['/profile'], { queryParams: { tab: 'settings' } });

    const positions = auth.pilot()?.staffPositions ?? [];
    const allowed = area ? hasStaffArea(positions, area) : positions.length > 0;

    return allowed ? true : router.createUrlTree(['/dashboard']);
  };
}