import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { Component, computed, input, signal } from '@angular/core';
import { NETWORK_LOGOS } from '../../core/pirep/pirep.models';
import { PilotProfile, ProfileAirport, countryFlagByCode, ordinal, timeSince } from '../../core/profile/profile.models';
import { countryFlagUrl } from '../../shared/utils/country-flag';
import { ActivityChart } from './activity-chart';
import { PilotAvatar } from './pilot-avatar';

@Component({
  selector: 'app-profile-overview',
  imports: [DatePipe, NgTemplateOutlet, ActivityChart, PilotAvatar],
  template: `
    @let p = profile();

    <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
      <div class="panel p-5 lg:col-span-1">
        <p class="text-2xl font-bold text-ink">{{ p.joinedAt | date: 'yyyy-MM-dd' }}</p>
        <p class="mt-1 flex flex-wrap items-center gap-1.5 text-sm text-ink-muted">
          <i class="ti ti-calendar" aria-hidden="true"></i> Joined <span class="text-xs">({{ since() }})</span>
        </p>
      </div>
      <div class="panel p-5">
        <p class="font-mono text-2xl font-bold text-ink">{{ p.displayId }}</p>
        <p class="mt-1 flex items-center gap-1.5 text-sm text-ink-muted"><i class="ti ti-id-badge-2" aria-hidden="true"></i> {{ p.rankName }}</p>
      </div>
      <div class="panel p-5">
        <p class="font-mono text-2xl font-bold text-ink">{{ p.baseIcao }}</p>
        <p class="mt-1 flex items-center gap-1.5 text-sm text-ink-muted"><i class="ti ti-building-airport" aria-hidden="true"></i> Base</p>
      </div>
      <div class="panel p-5">
        <p class="font-mono text-2xl font-bold" [class]="p.vatsimId ? 'text-ink' : 'text-ink-muted'">{{ p.vatsimId ?? '—' }}</p>
        <p class="mt-1 flex items-center gap-1.5 text-sm text-ink-muted">
          <img [src]="logos.Vatsim" alt="" class="h-4 w-auto" /> VATSIM ID
        </p>
      </div>
      <div class="panel p-5">
        <p class="font-mono text-2xl font-bold" [class]="p.ivaoId ? 'text-ink' : 'text-ink-muted'">{{ p.ivaoId ?? '—' }}</p>
        <p class="mt-1 flex items-center gap-1.5 text-sm text-ink-muted">
          <img [src]="logos.Ivao" alt="" class="h-4 w-auto invert dark:invert-0" /> IVAO ID
        </p>
      </div>
    </div>

    <div class="mt-6 grid gap-6 lg:grid-cols-3">
      <div class="flex flex-col gap-6">
        <div class="panel p-6">
          <p class="eyebrow text-xs">About</p>
          <ul class="mt-3 flex flex-col gap-3 text-sm">
            <li class="flex items-start gap-2.5">
              <i class="ti ti-user mt-0.5 text-lg text-ink-muted" aria-hidden="true"></i>
              <span class="text-ink-muted">Full name: <span class="text-ink">{{ p.firstName }} {{ p.lastName }}</span></span>
            </li>
            @if (p.age !== null) {
              <li class="flex items-start gap-2.5">
                <i class="ti ti-cake mt-0.5 text-lg text-ink-muted" aria-hidden="true"></i>
                <span class="text-ink-muted">
                  Age: <span class="text-ink">{{ p.age }} years</span>
                  @if (p.private?.birthDate; as birth) { <span class="text-xs">({{ birth | date: 'yyyy-MM-dd' }})</span> }
                </span>
              </li>
            }
            @if (location()) {
              <li class="flex items-start gap-2.5">
                <i class="ti ti-map-pin mt-0.5 text-lg text-ink-muted" aria-hidden="true"></i>
                <span class="flex items-center gap-1.5 text-ink">
                  @if (pilotFlag(); as flag) { <img [src]="flag" alt="" class="h-3 w-auto rounded-sm" /> }
                  {{ location() }}
                </span>
              </li>
            }
          </ul>

          @if (p.private; as priv) {
            <p class="eyebrow mt-6 text-xs">Contacts</p>
            <ul class="mt-3 flex flex-col gap-3 text-sm">
              <li class="flex items-start gap-2.5">
                <i class="ti ti-brand-discord mt-0.5 text-lg text-ink-muted" aria-hidden="true"></i>
                <span class="text-ink-muted">Discord: <span class="text-ink">{{ priv.discordId ?? 'Not informed' }}</span></span>
              </li>
              <li class="flex items-start gap-2.5">
                <i class="ti ti-mail mt-0.5 text-lg text-ink-muted" aria-hidden="true"></i>
                <span class="min-w-0 break-all text-ink-muted">Email: <span class="text-ink">{{ priv.email }}</span></span>
              </li>
            </ul>
            <p class="mt-2 flex items-center gap-1 text-xs text-ink-muted"><i class="ti ti-lock" aria-hidden="true"></i> Only you and the staff can see this.</p>
          }

          <p class="eyebrow mt-6 text-xs">Aircraft in the simulator</p>
          @if (p.simAircraft.length) {
            <ul class="mt-3 flex flex-col gap-2.5 text-sm">
              @for (a of p.simAircraft; track a.icaoCode) {
                <li class="flex items-center gap-2.5">
                  <i class="ti ti-plane text-lg text-primary" aria-hidden="true"></i>
                  <span class="text-ink">{{ a.name }}</span>
                  <span class="text-xs text-ink-muted">({{ a.fleetCount }} in the fleet)</span>
                </li>
              }
            </ul>
          } @else {
            <p class="mt-3 text-sm text-ink-muted">{{ p.isOwner ? 'Select your aircraft in Settings.' : 'Not informed.' }}</p>
          }
        </div>

        <div class="panel overflow-hidden">
          <h2 class="border-b border-line px-6 py-4 text-lg font-semibold text-ink">Best years with us</h2>
          @if (bestYears().length) {
            <table class="w-full text-left text-sm">
              <thead class="border-b border-line text-xs uppercase tracking-wide text-ink-muted">
                <tr>
                  <th class="px-6 py-3 font-semibold">Rank</th>
                  <th class="px-6 py-3 font-semibold">Year</th>
                  <th class="px-6 py-3 font-semibold">Flights</th>
                  <th class="px-6 py-3 font-semibold">Hours</th>
                </tr>
              </thead>
              <tbody>
                @for (y of bestYears(); track y.year; let i = $index) {
                  <tr class="border-b border-line last:border-0">
                    <td class="px-6 py-3 font-bold text-ink">{{ ordinal(i + 1) }}</td>
                    <td class="px-6 py-3 text-ink">{{ y.year }}</td>
                    <td class="px-6 py-3 font-semibold text-ink">{{ y.flights }}</td>
                    <td class="px-6 py-3 text-ink-muted">{{ hours(y.minutes) }}</td>
                  </tr>
                }
              </tbody>
            </table>
          } @else {
            <p class="px-6 py-8 text-center text-sm text-ink-muted">No flights yet.</p>
          }
        </div>

        <div class="panel overflow-hidden">
          <button type="button" class="flex w-full items-center justify-between px-6 py-4 text-left font-semibold text-ink hover:bg-surface-alt/50" (click)="toggle('badge')">
            Badge <i class="ti text-ink-muted" [class]="open().has('badge') ? 'ti-chevron-down' : 'ti-chevron-right'" aria-hidden="true"></i>
          </button>
          @if (open().has('badge')) {
            <div class="border-t border-line p-6">
              <div class="relative mx-auto w-60 overflow-hidden rounded-2xl bg-gradient-to-b from-atlantico-500 to-atlantico-800 px-5 pb-5 pt-4 text-center text-white shadow-lg">
                <div class="absolute -right-16 -top-16 h-40 w-40 rounded-full border border-white/20"></div>
                <div class="absolute -bottom-20 -left-16 h-44 w-44 rounded-full border border-onda-300/30"></div>
                <div class="relative">
                  <div class="mx-auto mb-3 h-2 w-14 rounded-full bg-white/80"></div>
                  <p class="text-xl font-extrabold tracking-[0.25em]">LITORAL</p>
                  <p class="text-[9px] tracking-[0.35em] text-white/70">LINHAS AÉREAS</p>
                  <app-pilot-avatar [photoUrl]="p.photoUrl" [firstName]="p.firstName" [lastName]="p.lastName" fontSize="2rem"
                    class="mx-auto mt-4 h-28 w-28 rounded-full ring-4 ring-white/30" />
                  <p class="mt-3 font-mono text-lg font-bold">{{ p.displayId }}</p>
                  <p class="text-xl font-bold leading-tight">{{ p.firstName }}<br />{{ p.lastName }}</p>
                  <p class="mt-1 text-sm font-medium text-onda-200">{{ p.rankName }}</p>
                  <p class="mt-2 text-xs text-white/75">Admitted: {{ p.joinedAt | date: 'dd/MM/yyyy' }}</p>
                  <p class="mx-auto mt-3 w-fit rounded-full bg-white px-3 py-0.5 text-[10px] font-bold tracking-wider text-atlantico-700">LITORAL LINHAS AÉREAS</p>
                </div>
              </div>
            </div>
          }
        </div>

        <div class="panel overflow-hidden">
          <button type="button" class="flex w-full items-center justify-between px-6 py-4 text-left font-semibold text-ink hover:bg-surface-alt/50" (click)="toggle('signature')">
            Signature <i class="ti text-ink-muted" [class]="open().has('signature') ? 'ti-chevron-down' : 'ti-chevron-right'" aria-hidden="true"></i>
          </button>
          @if (open().has('signature')) {
            <div class="border-t border-line p-6">
              <div class="relative mx-auto flex h-28 max-w-md items-stretch overflow-hidden rounded-lg bg-gradient-to-r from-atlantico-800 via-atlantico-600 to-onda-500 text-white shadow-lg">
                <div class="flex w-28 shrink-0 flex-col items-center justify-center gap-1 bg-black/20">
                  <img src="favicon.svg" alt="" class="h-10 w-10" />
                  <span class="text-[10px] font-extrabold tracking-[0.2em]">LITORAL</span>
                </div>
                <div class="flex min-w-0 flex-1 flex-col justify-center px-4">
                  <p class="truncate text-sm font-bold">{{ p.displayId }} · {{ p.displayName }}</p>
                  <p class="text-xs text-onda-100">{{ p.rankName }}</p>
                  <div class="mt-2 grid grid-cols-3 gap-2 text-[11px]">
                    <span><span class="block font-bold">{{ hours(p.totalFlightMinutes) }}</span><span class="text-white/70">hours</span></span>
                    <span><span class="block font-bold">{{ p.totalFlights }}</span><span class="text-white/70">flights</span></span>
                    <span><span class="block font-bold">{{ p.joinedAt | date: 'dd/MM/yyyy' }}</span><span class="text-white/70">since</span></span>
                  </div>
                </div>
              </div>
            </div>
          }
        </div>
      </div>

      <div class="flex flex-col gap-6 lg:col-span-2">
        <div class="panel p-6">
          <p class="eyebrow text-xs">Annual activity</p>
          <div class="mt-3 flex gap-8">
            <div>
              <p class="text-sm text-ink-muted">{{ currentYear - 1 }}</p>
              <p class="text-2xl font-bold text-ink">{{ previous()?.flights ?? 0 }} <span class="text-sm font-normal text-ink-muted">flights</span></p>
            </div>
            <div>
              <p class="text-sm text-ink-muted">{{ currentYear }}</p>
              <p class="text-2xl font-bold text-primary">{{ current()?.flights ?? 0 }} <span class="text-sm font-normal text-ink-muted">flights</span></p>
            </div>
          </div>
          <app-activity-chart class="mt-4" [current]="current()" [previous]="previous()" [currentYear]="currentYear" />
        </div>

        <div class="panel overflow-hidden">
          <div class="flex items-baseline gap-2 border-b border-line px-6 py-4">
            <h2 class="text-lg font-semibold text-ink">Top routes</h2>
            <span class="text-xs text-ink-muted">All flights performed</span>
          </div>
          @if (p.topRoutes.length) {
            <div class="overflow-x-auto">
              <table class="w-full text-left text-sm">
                <thead class="border-b border-line text-xs uppercase tracking-wide text-ink-muted">
                  <tr>
                    <th class="px-6 py-3 font-semibold">Rank</th>
                    <th class="px-6 py-3 font-semibold">Dep</th>
                    <th class="px-6 py-3 font-semibold">Arr</th>
                    <th class="px-6 py-3 text-right font-semibold">Flights</th>
                  </tr>
                </thead>
                <tbody>
                  @for (r of p.topRoutes; track $index; let i = $index) {
                    <tr class="border-b border-line last:border-0" [class.opacity-70]="i >= 3">
                      <td class="px-6 py-3 font-bold text-ink">{{ ordinal(i + 1) }}</td>
                      <td class="px-6 py-3">
                        <ng-container *ngTemplateOutlet="airportCell; context: { $implicit: r.departure }" />
                      </td>
                      <td class="px-6 py-3">
                        <ng-container *ngTemplateOutlet="airportCell; context: { $implicit: r.arrival }" />
                      </td>
                      <td class="px-6 py-3 text-right font-semibold text-ink">{{ r.flights }} {{ r.flights === 1 ? 'flight' : 'flights' }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          } @else {
            <p class="px-6 py-8 text-center text-sm text-ink-muted">No approved flights yet.</p>
          }
        </div>
      </div>
    </div>

    <ng-template #airportCell let-airport>
      <p class="flex items-center gap-1.5 font-mono font-bold text-ink">
        @if (flag(airport); as url) { <img [src]="url" alt="" class="h-3 w-auto rounded-sm" /> }
        {{ airport.icao }}
      </p>
      <p class="text-xs text-ink-muted">{{ airport.city || airport.name }}</p>
    </ng-template>
  `
})
export class ProfileOverview {
  readonly profile = input.required<PilotProfile>();

  protected readonly logos = NETWORK_LOGOS;
  protected readonly ordinal = ordinal;
  protected readonly currentYear = new Date().getUTCFullYear();
  protected readonly open = signal(new Set<string>());

  protected readonly since = computed(() => timeSince(this.profile().joinedAt));
  protected readonly current = computed(() => this.profile().activity.find(a => a.year === this.currentYear) ?? null);
  protected readonly previous = computed(() => this.profile().activity.find(a => a.year === this.currentYear - 1) ?? null);
  protected readonly bestYears = computed(() => [...this.profile().activity].sort((a, b) => b.flights - a.flights || b.minutes - a.minutes).slice(0, 5));
  protected readonly pilotFlag = computed(() => countryFlagByCode(this.profile().countryCode));

  protected readonly location = computed(() => {
    const p = this.profile();
    return [p.city, p.state, p.countryCode].filter(Boolean).join(', ');
  });

  protected toggle(key: string): void {
    this.open.update(set => {
      const next = new Set(set);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  }

  protected flag(airport: ProfileAirport): string | null {
    return countryFlagUrl(airport.country);
  }

  protected hours(minutes: number): string {
    return `${Math.floor(minutes / 60)}:${(minutes % 60).toString().padStart(2, '0')}`;
  }
}