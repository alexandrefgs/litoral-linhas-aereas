import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { StaffArea, hasStaffArea } from '../../core/staff/staff-positions';

interface AdminMenuItem {
  area: StaffArea;
  labelKey: string;
  icon: string;
  path?: string;
}

const MENU: AdminMenuItem[] = [
  { area: 'routes', labelKey: 'admin.areas.airports', icon: 'ti-building-airport', path: '/admin/airports' },
  { area: 'routes', labelKey: 'admin.areas.routes', icon: 'ti-route' },
  { area: 'fleet', labelKey: 'admin.areas.fleet', icon: 'ti-plane' },
  { area: 'pilots', labelKey: 'admin.areas.pilots', icon: 'ti-users' },
  { area: 'staff', labelKey: 'admin.areas.staff', icon: 'ti-shield-lock' },
  { area: 'operations', labelKey: 'admin.areas.operations', icon: 'ti-calendar-event' },
  { area: 'pireps', labelKey: 'admin.areas.pireps', icon: 'ti-clipboard-check' },
  { area: 'reports', labelKey: 'admin.areas.reports', icon: 'ti-chart-line' }
];

@Component({
  selector: 'app-admin-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, TranslatePipe],
  template: `
    <section class="mx-auto grid max-w-7xl gap-6 px-6 py-8 lg:grid-cols-[240px_1fr]">
      <aside class="panel h-fit p-3">
        <a routerLink="/admin" class="eyebrow block px-3 pb-3 pt-1 hover:text-primary">{{ 'admin.title' | t }}</a>
        <nav class="flex flex-col gap-1">
          @for (item of items(); track item.labelKey) {
            @if (item.path) {
              <a [routerLink]="item.path" routerLinkActive="!bg-primary !text-on-primary"
                class="flex items-center gap-3 rounded-lg px-3 py-2 font-medium text-ink hover:bg-surface-alt">
                <i class="ti {{ item.icon }} text-xl" aria-hidden="true"></i>
                {{ item.labelKey | t }}
              </a>
            } @else {
              <span class="flex items-center gap-3 rounded-lg px-3 py-2 text-ink-muted">
                <i class="ti {{ item.icon }} text-xl" aria-hidden="true"></i>
                {{ item.labelKey | t }}
                <span class="ml-auto whitespace-nowrap rounded-full bg-surface-alt px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide">{{ 'admin.soon' | t }}</span>
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