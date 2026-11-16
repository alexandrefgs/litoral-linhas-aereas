import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { combineLatest } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { DEFAULT_COVER_URL, PilotProfile, countryFlagByCode } from '../../core/profile/profile.models';
import { ProfileService } from '../../core/profile/profile.service';
import { PilotAvatar } from './pilot-avatar';
import { ProfileFlights } from './profile-flights';
import { ProfileOverview } from './profile-overview';
import { ProfileSettings } from './profile-settings';

type ProfileTab = 'profile' | 'flights' | 'awards' | 'downloads' | 'settings';

interface TabItem {
  key: ProfileTab;
  label: string;
  icon: string;
  ownerOnly?: boolean;
}

const TABS: TabItem[] = [
  { key: 'profile', label: 'Profile', icon: 'ti-user' },
  { key: 'flights', label: 'All Flights', icon: 'ti-map-2' },
  { key: 'awards', label: 'Awards', icon: 'ti-award' },
  { key: 'downloads', label: 'Downloads', icon: 'ti-download', ownerOnly: true },
  { key: 'settings', label: 'Settings', icon: 'ti-settings', ownerOnly: true }
];

@Component({
  selector: 'app-profile',
  imports: [PilotAvatar, ProfileOverview, ProfileFlights, ProfileSettings],
  template: `
    <section class="mx-auto flex max-w-7xl flex-col gap-6 px-6 py-8">
      @if (loading() && !profile()) {
        <p class="py-24 text-center text-ink-muted">Loading...</p>
      } @else if (notFound()) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-16 text-center">
          <i class="ti ti-user-off text-4xl text-ink-muted" aria-hidden="true"></i>
          <p class="text-ink-muted">Pilot not found.</p>
        </div>
      } @else if (loadError()) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-16 text-center">
          <p class="text-danger">Something went wrong. Try again.</p>
          <button type="button" class="btn-secondary" (click)="load()">Try again</button>
        </div>
      } @else if (profile(); as p) {
        <div class="panel overflow-hidden">
          <div class="h-40 bg-cover bg-center sm:h-64" [style.background-image]="coverStyle()"></div>
          <div class="flex flex-col gap-4 px-6 pb-5 sm:flex-row sm:items-end sm:justify-between">
            <div class="flex items-end gap-4">
              <app-pilot-avatar [photoUrl]="p.photoUrl" [firstName]="p.firstName" [lastName]="p.lastName" fontSize="2.25rem"
                class="-mt-12 h-24 w-24 rounded-xl border-4 border-surface shadow-lg sm:-mt-16 sm:h-32 sm:w-32" />
              <div class="pb-1">
                <h1 class="text-xl font-semibold text-ink sm:text-2xl">{{ p.displayId }} - {{ p.displayName }}</h1>
                <p class="mt-1 flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-ink-muted">
                  <span class="flex items-center gap-1.5">
                    <i class="ti ti-circle-dot" aria-hidden="true"></i> Status:
                    <span class="font-semibold" [class]="p.isActive ? 'text-onda-500' : 'text-danger'">{{ p.isActive ? 'Active Pilot' : 'Inactive' }}</span>
                  </span>
                  @if (location()) {
                    <span class="flex items-center gap-1.5">
                      <i class="ti ti-map-pin" aria-hidden="true"></i>
                      {{ location() }}
                      @if (flag(); as url) { <img [src]="url" alt="" class="h-3 w-auto rounded-sm" /> }
                    </span>
                  }
                </p>
              </div>
            </div>
            <div class="flex gap-6 pb-1 text-center">
              <div>
                <p class="text-xl font-bold text-ink">{{ hours(p.totalFlightMinutes) }}</p>
                <p class="text-xs uppercase tracking-wide text-ink-muted">Hours</p>
              </div>
              <div>
                <p class="text-xl font-bold text-ink">{{ p.totalFlights }}</p>
                <p class="text-xs uppercase tracking-wide text-ink-muted">Flights</p>
              </div>
              <div>
                <p class="text-xl font-bold" [class]="p.points < 0 ? 'text-danger' : 'text-primary'">{{ p.points.toLocaleString('en-US') }}</p>
                <p class="text-xs uppercase tracking-wide text-ink-muted">Points</p>
              </div>
            </div>
          </div>
        </div>

        <nav class="flex flex-wrap gap-2">
          @for (t of tabs(); track t.key) {
            <button type="button" (click)="select(t.key)"
              class="flex items-center gap-1.5 rounded-md px-4 py-2 text-sm font-medium transition-colors"
              [class]="tab() === t.key ? 'bg-primary text-on-primary' : 'border border-line bg-surface text-ink hover:bg-surface-alt'">
              <i class="ti {{ t.icon }}" aria-hidden="true"></i> {{ t.label }}
            </button>
          }
        </nav>

        @switch (tab()) {
          @case ('profile') { <app-profile-overview [profile]="p" /> }
          @case ('flights') { <app-profile-flights [pilotNumber]="p.pilotNumber" [isOwner]="p.isOwner" /> }
          @case ('awards') {
            <div class="panel flex flex-col items-center gap-3 px-6 py-16 text-center">
              <i class="ti ti-award text-4xl text-ink-muted" aria-hidden="true"></i>
              <p class="text-ink-muted">No awards yet.</p>
            </div>
          }
          @case ('downloads') {
            <div class="panel flex flex-col items-center gap-3 px-6 py-16 text-center">
              <i class="ti ti-download text-4xl text-ink-muted" aria-hidden="true"></i>
              <p class="text-ink-muted">Documents, liveries and manuals will be available here soon.</p>
            </div>
          }
          @case ('settings') { <app-profile-settings (changed)="refresh()" /> }
        }
      }
    </section>
  `
})
export class Profile {
  private readonly api = inject(ProfileService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly profile = signal<PilotProfile | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly notFound = signal(false);
  protected readonly tab = signal<ProfileTab>('profile');

  private pilotNumber: number | null = null;

  protected readonly tabs = computed(() => TABS.filter(t => !t.ownerOnly || this.profile()?.isOwner));
  protected readonly flag = computed(() => countryFlagByCode(this.profile()?.countryCode));

  protected readonly location = computed(() => {
    const p = this.profile();
    return p ? [p.state, p.city].filter(Boolean).join(', ') : '';
  });

  protected readonly coverStyle = computed(() => {
    const url = this.profile()?.coverUrl ?? DEFAULT_COVER_URL;
    return `url('${url}'), linear-gradient(135deg, #0F3768 0%, #1D6FD8 55%, #2EC4B6 100%)`;
  });

  constructor() {
    combineLatest([this.route.paramMap, this.route.queryParamMap])
      .pipe(takeUntilDestroyed())
      .subscribe(([params, query]) => {
        const number = Number(params.get('number') ?? this.auth.pilot()?.pilotNumber);
        this.tab.set(this.parseTab(query.get('tab')));

        if (number !== this.pilotNumber) {
          this.pilotNumber = number;
          this.profile.set(null);
          this.load();
        }
      });
  }

  protected async load(): Promise<void> {
    if (!this.pilotNumber || Number.isNaN(this.pilotNumber)) {
      this.notFound.set(true);
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.loadError.set(false);
    this.notFound.set(false);

    try {
      const profile = await this.api.get(this.pilotNumber);
      this.profile.set(profile);
      if (!this.tabs().some(t => t.key === this.tab())) this.select('profile');
    } catch (error) {
      if ((error as { status?: number }).status === 404) this.notFound.set(true);
      else this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

    protected async refresh(): Promise<void> {
    if (!this.pilotNumber) return;
    try {
      this.profile.set(await this.api.get(this.pilotNumber));
    } catch {}
  }

  protected select(tab: ProfileTab): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab: tab === 'profile' ? null : tab },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  protected hours(minutes: number): string {
    return `${Math.floor(minutes / 60)}:${(minutes % 60).toString().padStart(2, '0')}`;
  }

  private parseTab(value: string | null): ProfileTab {
    return TABS.some(t => t.key === value) ? (value as ProfileTab) : 'profile';
  }
}