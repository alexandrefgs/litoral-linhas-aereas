import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { STAFF_POSITION_NAMES } from '../../core/staff/staff-positions';
import { ThemeMode, ThemeService } from '../../core/theme/theme.service';
import { ClickOutsideDirective } from '../directives/click-outside.directive';

type Menu = 'theme' | 'notams' | 'news' | 'user';

interface NavItem {
  path: string;
  icon: string;
  label: string;
}

@Component({
  selector: 'app-header',
  imports: [RouterLink, RouterLinkActive, ClickOutsideDirective],
  template: `
    <header class="sticky top-0 z-[1100] border-b border-line bg-surface/95 backdrop-blur" (appClickOutside)="openMenu.set(null)">
      <div class="mx-auto flex max-w-7xl items-center justify-between gap-4 px-6 py-3">
        <a routerLink="/" class="flex items-center gap-3">
          <img src="favicon.svg" alt="" class="h-10 w-10" />
          <span class="leading-tight">
            <span class="block text-xl font-extrabold tracking-wide text-primary">LITORAL</span>
            <span class="block text-[11px] font-medium tracking-[0.2em] text-ink-muted">LINHAS AÉREAS</span>
          </span>
        </a>

        @if (!auth.isAuthenticated()) {
          <nav class="hidden items-center gap-1 lg:flex">
            @for (link of publicLinks; track link.fragment) {
              <a routerLink="/" [fragment]="link.fragment" class="rounded-md px-3 py-2 text-sm font-medium text-ink hover:text-primary">{{ link.label }}</a>
            }
          </nav>
        }

        <div class="flex items-center gap-1 sm:gap-2">
          <div class="relative">
            <button type="button" class="icon-btn" aria-label="Theme" (click)="toggle('theme')">
              <i class="ti {{ themeIcon() }}" aria-hidden="true"></i>
            </button>
            @if (openMenu() === 'theme') {
              <div class="menu-panel w-48 p-2">
                @for (option of themeOptions; track option.mode) {
                  <button type="button" (click)="setTheme(option.mode)"
                    class="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-left hover:bg-surface-alt"
                    [class.text-primary]="theme.mode() === option.mode"
                    [class.text-ink]="theme.mode() !== option.mode">
                    <i class="ti {{ option.icon }} text-xl" aria-hidden="true"></i>
                    {{ option.label }}
                  </button>
                }
              </div>
            }
          </div>

          @if (auth.isAuthenticated()) {
            <div class="relative">
              <button type="button" class="icon-btn" aria-label="NOTAMs" (click)="toggle('notams')">
                <i class="ti ti-alert-triangle" aria-hidden="true"></i>
              </button>
              @if (openMenu() === 'notams') {
                <div class="menu-panel w-80">
                  <div class="flex items-center justify-between border-b border-line px-4 py-3">
                    <span class="font-semibold text-ink">NOTAMs</span>
                    <i class="ti ti-speakerphone text-xl text-ink-muted" aria-hidden="true"></i>
                  </div>
                  <p class="px-4 py-8 text-center text-sm text-ink-muted">No NOTAMs published.</p>
                </div>
              }
            </div>

            <div class="relative">
              <button type="button" class="icon-btn" aria-label="News" (click)="toggle('news')">
                <i class="ti ti-bell" aria-hidden="true"></i>
              </button>
              @if (openMenu() === 'news') {
                <div class="menu-panel w-80">
                  <div class="flex items-center justify-between border-b border-line px-4 py-3">
                    <span class="font-semibold text-ink">News</span>
                    <i class="ti ti-news text-xl text-ink-muted" aria-hidden="true"></i>
                  </div>
                  <p class="px-4 py-8 text-center text-sm text-ink-muted">No news yet.</p>
                </div>
              }
            </div>

            @if (auth.pilot(); as pilot) {
              <div class="relative ml-1">
                <button type="button" (click)="toggle('user')" aria-label="My account"
                  class="flex h-10 w-10 items-center justify-center rounded-full bg-primary text-sm font-bold text-on-primary ring-2 ring-accent ring-offset-2 ring-offset-surface">
                  {{ initials() }}
                </button>
                @if (openMenu() === 'user') {
                  <div class="menu-panel w-64">
                    <div class="flex items-center gap-3 border-b border-line px-4 py-3">
                      <span class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-primary text-sm font-bold text-on-primary">{{ initials() }}</span>
                      <div class="min-w-0">
                        <p class="truncate font-semibold text-ink">{{ pilot.firstName.charAt(0) }}. {{ pilot.lastName }}</p>
                        <p class="truncate text-sm text-ink-muted">{{ pilot.displayId }} · {{ roleLabel() }}</p>
                      </div>
                    </div>
                    <div class="p-2">
                      <a routerLink="/profile" (click)="openMenu.set(null)" class="flex items-center gap-3 rounded-lg px-3 py-2 text-ink hover:bg-surface-alt">
                        <i class="ti ti-user text-xl" aria-hidden="true"></i>Profile
                      </a>
                    </div>
                    <div class="border-t border-line p-3">
                      <button type="button" (click)="logout()" class="flex w-full items-center justify-center gap-2 rounded-lg bg-danger px-4 py-2 font-medium text-white hover:opacity-90">
                        Sign out <i class="ti ti-logout" aria-hidden="true"></i>
                      </button>
                    </div>
                  </div>
                }
              </div>
            }
          } @else {
            <a routerLink="/login" class="btn-secondary ml-1">Sign in</a>
            <a routerLink="/register" class="btn-primary">Create account</a>
          }
        </div>
      </div>

      @if (auth.isAuthenticated()) {
        <nav class="border-t border-line">
          <div class="mx-auto flex max-w-7xl gap-1 overflow-x-auto px-6 py-2">
            @for (item of navItems(); track item.path) {
              <a [routerLink]="item.path" routerLinkActive="!bg-primary !text-on-primary"
                class="flex shrink-0 items-center gap-2 rounded-lg px-4 py-2 font-medium text-ink hover:bg-surface-alt">
                <i class="ti {{ item.icon }} text-xl" aria-hidden="true"></i>
                {{ item.label }}
              </a>
            }
          </div>
        </nav>
      }
    </header>
  `
})
export class Header {
  protected readonly auth = inject(AuthService);
  protected readonly theme = inject(ThemeService);
  protected readonly openMenu = signal<Menu | null>(null);

  private readonly pilotNavItems: NavItem[] = [
    { path: '/dashboard', icon: 'ti-home', label: 'Dashboard' },
    { path: '/book', icon: 'ti-world-search', label: 'Book flight' },
    { path: '/routes', icon: 'ti-route', label: 'Routes' },
    { path: '/airports', icon: 'ti-building-airport', label: 'Airports' },
    { path: '/fleet', icon: 'ti-plane', label: 'Fleet' },
    { path: '/points', icon: 'ti-star', label: 'Points' },
    { path: '/statistics', icon: 'ti-chart-bar', label: 'Statistics' }
  ];

  protected readonly navItems = computed(() =>
    this.auth.isStaff()
      ? [...this.pilotNavItems, { path: '/admin', icon: 'ti-shield-lock', label: 'Admin' }]
      : this.pilotNavItems
  );

  protected readonly publicLinks = [
    { fragment: 'inicio', label: 'Home' },
    { fragment: 'sobre', label: 'About' },
    { fragment: 'operacao', label: 'Operations' },
    { fragment: 'numeros', label: 'Numbers' },
    { fragment: 'voos', label: 'Flights' }
  ];

  protected readonly themeOptions: { mode: ThemeMode; icon: string; label: string }[] = [
    { mode: 'light', icon: 'ti-sun', label: 'Light' },
    { mode: 'dark', icon: 'ti-moon', label: 'Dark' },
    { mode: 'system', icon: 'ti-device-desktop', label: 'System' }
  ];

  protected readonly themeIcon = computed(
    () => this.themeOptions.find(o => o.mode === this.theme.mode())?.icon ?? 'ti-sun'
  );

  protected readonly initials = computed(() => {
    const pilot = this.auth.pilot();
    return pilot ? `${pilot.firstName.charAt(0)}${pilot.lastName.charAt(0)}`.toUpperCase() : '';
  });

  protected readonly roleLabel = computed(() => {
    const positions = this.auth.pilot()?.staffPositions ?? [];
    return positions.length ? STAFF_POSITION_NAMES[positions[0]] ?? positions[0] : 'Pilot';
  });

  protected toggle(menu: Menu): void {
    this.openMenu.update(current => (current === menu ? null : menu));
  }

  protected setTheme(mode: ThemeMode): void {
    this.theme.setMode(mode);
    this.openMenu.set(null);
  }

  protected logout(): void {
    this.openMenu.set(null);
    this.auth.logout();
  }
}