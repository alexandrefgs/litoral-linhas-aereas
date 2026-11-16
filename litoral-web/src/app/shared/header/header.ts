import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { STAFF_POSITION_NAMES } from '../../core/staff/staff-positions';
import { ThemeMode, ThemeService } from '../../core/theme/theme.service';
import { ClickOutsideDirective } from '../directives/click-outside.directive';
import { LanguageSwitcher } from '../language-switcher/language-switcher';

type Menu = 'theme' | 'notams' | 'news' | 'user';

interface NavItem {
  path: string;
  icon: string;
  labelKey: string;
}

@Component({
  selector: 'app-header',
  imports: [RouterLink, RouterLinkActive, TranslatePipe, LanguageSwitcher, ClickOutsideDirective],
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
              <a routerLink="/" [fragment]="link.fragment" class="rounded-md px-3 py-2 text-sm font-medium text-ink hover:text-primary">{{ link.labelKey | t }}</a>
            }
          </nav>
        }

        <div class="flex items-center gap-1 sm:gap-2">
          <app-language-switcher class="hidden md:block" />

          <div class="relative">
            <button type="button" class="icon-btn" [attr.aria-label]="'theme.title' | t" (click)="toggle('theme')">
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
                    {{ option.labelKey | t }}
                  </button>
                }
              </div>
            }
          </div>

          @if (auth.isAuthenticated()) {
            <div class="relative">
              <button type="button" class="icon-btn" [attr.aria-label]="'header.notams' | t" (click)="toggle('notams')">
                <i class="ti ti-alert-triangle" aria-hidden="true"></i>
              </button>
              @if (openMenu() === 'notams') {
                <div class="menu-panel w-80">
                  <div class="flex items-center justify-between border-b border-line px-4 py-3">
                    <span class="font-semibold text-ink">{{ 'header.notams' | t }}</span>
                    <i class="ti ti-speakerphone text-xl text-ink-muted" aria-hidden="true"></i>
                  </div>
                  <p class="px-4 py-8 text-center text-sm text-ink-muted">{{ 'header.noNotams' | t }}</p>
                </div>
              }
            </div>

            <div class="relative">
              <button type="button" class="icon-btn" [attr.aria-label]="'header.news' | t" (click)="toggle('news')">
                <i class="ti ti-bell" aria-hidden="true"></i>
              </button>
              @if (openMenu() === 'news') {
                <div class="menu-panel w-80">
                  <div class="flex items-center justify-between border-b border-line px-4 py-3">
                    <span class="font-semibold text-ink">{{ 'header.news' | t }}</span>
                    <i class="ti ti-news text-xl text-ink-muted" aria-hidden="true"></i>
                  </div>
                  <p class="px-4 py-8 text-center text-sm text-ink-muted">{{ 'header.noNews' | t }}</p>
                </div>
              }
            </div>

            @if (auth.pilot(); as pilot) {
              <div class="relative ml-1">
                <button type="button" (click)="toggle('user')" [attr.aria-label]="'header.account' | t"
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
                        <i class="ti ti-user text-xl" aria-hidden="true"></i>{{ 'nav.profile' | t }}
                      </a>
                      <app-language-switcher class="block px-3 py-2 md:hidden" />
                    </div>
                    <div class="border-t border-line p-3">
                      <button type="button" (click)="logout()" class="flex w-full items-center justify-center gap-2 rounded-lg bg-danger px-4 py-2 font-medium text-white hover:opacity-90">
                        {{ 'nav.logout' | t }} <i class="ti ti-logout" aria-hidden="true"></i>
                      </button>
                    </div>
                  </div>
                }
              </div>
            }
          } @else {
            <a routerLink="/login" class="btn-secondary ml-1">{{ 'nav.login' | t }}</a>
            <a routerLink="/register" class="btn-primary">{{ 'nav.register' | t }}</a>
          }
        </div>
      </div>

      @if (auth.isAuthenticated()) {
        <nav class="border-t border-line">
          <div class="mx-auto flex max-w-7xl gap-1 overflow-x-auto px-6 py-2">
            @for (item of navItems; track item.path) {
              <a [routerLink]="item.path" routerLinkActive="!bg-primary !text-on-primary"
                class="flex shrink-0 items-center gap-2 rounded-lg px-4 py-2 font-medium text-ink hover:bg-surface-alt">
                <i class="ti {{ item.icon }} text-xl" aria-hidden="true"></i>
                {{ item.labelKey | t }}
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

  protected readonly navItems: NavItem[] = [
    { path: '/dashboard', icon: 'ti-home', labelKey: 'nav.dashboard' },
    { path: '/book', icon: 'ti-world-search', labelKey: 'nav.book' },
    { path: '/routes', icon: 'ti-route', labelKey: 'nav.routes' },
    { path: '/airports', icon: 'ti-building-airport', labelKey: 'nav.airports' },
    { path: '/fleet', icon: 'ti-plane', labelKey: 'nav.fleet' },
    { path: '/points', icon: 'ti-star', labelKey: 'nav.points' },
    { path: '/statistics', icon: 'ti-chart-bar', labelKey: 'nav.statistics' }
  ];

  protected readonly publicLinks = [
    { fragment: 'inicio', labelKey: 'public.home' },
    { fragment: 'sobre', labelKey: 'public.about' },
    { fragment: 'operacao', labelKey: 'public.operations' },
    { fragment: 'numeros', labelKey: 'public.numbers' },
    { fragment: 'voos', labelKey: 'public.flights' }
  ];

  protected readonly themeOptions: { mode: ThemeMode; icon: string; labelKey: string }[] = [
    { mode: 'light', icon: 'ti-sun', labelKey: 'theme.light' },
    { mode: 'dark', icon: 'ti-moon', labelKey: 'theme.dark' },
    { mode: 'system', icon: 'ti-device-desktop', labelKey: 'theme.system' }
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