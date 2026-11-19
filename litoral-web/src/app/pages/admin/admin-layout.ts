import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { StaffArea, hasStaffArea } from '../../core/staff/staff-positions';

interface AdminMenuItem {
  area: StaffArea;
  label: string;
  icon: string;
  path?: string;
}

const MENU: AdminMenuItem[] = [
  { area: 'routes', label: 'Airports', icon: 'ti-building-airport', path: '/admin/airports' },
  { area: 'routes', label: 'Routes', icon: 'ti-route', path: '/admin/routes' },
  { area: 'fleet', label: 'Fleet', icon: 'ti-plane', path: '/admin/fleet' },
  { area: 'fleet', label: 'MEL / CDL', icon: 'ti-tool', path: '/admin/mel' },
  { area: 'pilots', label: 'Pilots', icon: 'ti-users', path: '/admin/pilots' },
  { area: 'pilots', label: 'Applications', icon: 'ti-user-plus', path: '/admin/applications' },
  { area: 'pilots', label: 'Privacy', icon: 'ti-lock', path: '/admin/privacy' },
  { area: 'staff', label: 'Staff', icon: 'ti-shield-lock', path: '/admin/staff' },
  { area: 'operations', label: 'Operations', icon: 'ti-calendar-event', path: '/admin/operations' },
  { area: 'operations', label: 'Tours', icon: 'ti-route', path: '/admin/tours' },
  { area: 'operations', label: 'Events', icon: 'ti-flag', path: '/admin/events' },
  { area: 'operations', label: 'Awards', icon: 'ti-award', path: '/admin/awards' },
  { area: 'downloads', label: 'Downloads', icon: 'ti-download', path: '/admin/downloads' },
  { area: 'pireps', label: 'PIREPs', icon: 'ti-clipboard-check', path: '/admin/pireps' },
  { area: 'pireps', label: 'Scoring', icon: 'ti-scale', path: '/admin/scoring' },
  { area: 'reports', label: 'Logs', icon: 'ti-history', path: '/admin/logs' },
  { area: 'reports', label: 'Reports', icon: 'ti-chart-line', path: '/admin/reports' },
  { area: 'system', label: 'System', icon: 'ti-activity-heartbeat', path: '/admin/system' }
];

@Component({
  selector: 'app-admin-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <section class="mx-auto grid max-w-7xl gap-6 px-6 py-8 lg:grid-cols-[240px_1fr]">
      <aside class="panel h-fit p-3">
        <a routerLink="/admin" class="eyebrow block px-3 pb-3 pt-1 hover:text-primary">Administration</a>
        <nav class="flex flex-col gap-1">
          @for (item of items(); track item.label) {
            @if (item.path) {
              <a [routerLink]="item.path" routerLinkActive="!bg-primary !text-on-primary"
                class="flex items-center gap-3 rounded-lg px-3 py-2 font-medium text-ink hover:bg-surface-alt">
                <i class="ti {{ item.icon }} text-xl" aria-hidden="true"></i>
                {{ item.label }}
              </a>
            } @else {
              <span class="flex items-center gap-3 rounded-lg px-3 py-2 text-ink-muted">
                <i class="ti {{ item.icon }} text-xl" aria-hidden="true"></i>
                {{ item.label }}
                <span class="ml-auto whitespace-nowrap rounded-full bg-surface-alt px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide">Soon</span>
              </span>
            }
          }
        </nav>
      </aside>

      <div class="min-w-0">
        <router-outlet />
      </div>
    </section>
  `
})
export class AdminLayout {
  private readonly auth = inject(AuthService);

  protected readonly items = computed(() => {
    const positions = this.auth.pilot()?.staffPositions ?? [];
    return MENU.filter(item => hasStaffArea(positions, item.area));
  });
}