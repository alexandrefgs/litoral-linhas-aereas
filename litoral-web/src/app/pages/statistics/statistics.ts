import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Statistics as StatisticsData, RankedPilot } from '../../core/statistics/statistics.models';
import { StatisticsService } from '../../core/statistics/statistics.service';
import { BarChart } from '../../shared/charts/bar-chart';
import { BarList } from '../../shared/charts/bar-list';
import { formatMinutes } from '../../shared/utils/flight';
import { PilotPodium } from './pilot-podium';

type RankingTab = 'hours' | 'flights' | 'points';

const MONTHS = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

@Component({
  selector: 'app-statistics',
  imports: [DecimalPipe, RouterLink, BarChart, BarList, PilotPodium],
  template: `
    <section class="mx-auto flex max-w-7xl flex-col gap-6 px-6 py-8">
      <div>
        <h1 class="text-2xl font-bold text-ink">Statistics</h1>
        <p class="text-ink-muted">How the Litoral is flying, and where you stand.</p>
      </div>

      @if (loading()) {
        <p class="panel px-6 py-16 text-center text-ink-muted">Loading...</p>
      } @else if (!data()) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-16 text-center">
          <p class="text-danger">Something went wrong. Try again.</p>
          <button type="button" class="btn-secondary" (click)="load()">Try again</button>
        </div>
      } @else {
        @let s = data()!;

        <div class="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-6">
          @for (kpi of totals(); track kpi.label) {
            <div class="panel card-hover flex items-center gap-3 p-4">
              <span class="stat-icon"><i class="ti {{ kpi.icon }}" aria-hidden="true"></i></span>
              <div class="min-w-0">
                <p class="truncate text-xl font-bold text-ink">{{ kpi.value }}</p>
                <p class="text-xs text-ink-muted">{{ kpi.label }}</p>
              </div>
            </div>
          }
        </div>

        <div class="grid gap-6 lg:grid-cols-2">
          <div class="panel card-hover flex flex-col gap-4 p-5">
            <h2 class="flex items-center gap-2 font-semibold text-ink"><i class="ti ti-user text-lg text-primary" aria-hidden="true"></i>Your statistics</h2>
            <div class="grid grid-cols-2 gap-4 sm:grid-cols-4">
              <div><p class="text-xl font-bold text-ink">{{ s.me.flights }}</p><p class="text-xs text-ink-muted">Flights</p></div>
              <div><p class="text-xl font-bold text-ink">{{ format(s.me.minutes) }}</p><p class="text-xs text-ink-muted">Hours</p></div>
              <div><p class="text-xl font-bold text-ink">{{ s.me.points | number }}</p><p class="text-xs text-ink-muted">Points</p></div>
              <div><p class="text-xl font-bold text-ink">{{ s.me.averageLandingRate !== null ? s.me.averageLandingRate + ' fpm' : '—' }}</p><p class="text-xs text-ink-muted">Avg landing</p></div>
            </div>
            <div class="grid grid-cols-3 gap-3 border-t border-line pt-4">
              @for (pos of myPositions(); track pos.label) {
                <div class="rounded-lg bg-surface-alt px-3 py-2 text-center">
                  <p class="font-mono text-lg font-bold" [class]="pos.value ? 'text-primary' : 'text-ink-muted'">{{ pos.value ? '#' + pos.value : '—' }}</p>
                  <p class="text-[11px] text-ink-muted">{{ pos.label }}</p>
                </div>
              }
            </div>
          </div>

          <div class="panel card-hover flex flex-col gap-4 p-5">
            <h2 class="flex items-center gap-2 font-semibold text-ink"><i class="ti ti-calendar text-lg text-primary" aria-hidden="true"></i>{{ monthName(s.month.month) }} {{ s.month.year }}</h2>
            <div class="grid grid-cols-3 gap-4">
              <div><p class="text-xl font-bold text-ink">{{ s.month.flights }}</p><p class="text-xs text-ink-muted">VA flights</p></div>
              <div><p class="text-xl font-bold text-ink">{{ format(s.month.minutes) }}</p><p class="text-xs text-ink-muted">VA hours</p></div>
              <div><p class="text-xl font-bold text-ink">{{ s.month.activePilots }}</p><p class="text-xs text-ink-muted">Active pilots</p></div>
            </div>
            <div class="grid grid-cols-2 gap-3 border-t border-line pt-4 sm:grid-cols-4">
              <div><p class="font-bold text-ink">{{ s.me.monthFlights }}</p><p class="text-[11px] text-ink-muted">Your flights</p></div>
              <div><p class="font-bold text-ink">{{ format(s.me.monthMinutes) }}</p><p class="text-[11px] text-ink-muted">Your hours</p></div>
              <div><p class="font-mono font-bold" [class]="s.me.monthHoursPosition ? 'text-primary' : 'text-ink-muted'">{{ s.me.monthHoursPosition ? '#' + s.me.monthHoursPosition : '—' }}</p><p class="text-[11px] text-ink-muted">Hours podium</p></div>
              <div><p class="font-mono font-bold" [class]="s.me.monthFlightsPosition ? 'text-primary' : 'text-ink-muted'">{{ s.me.monthFlightsPosition ? '#' + s.me.monthFlightsPosition : '—' }}</p><p class="text-[11px] text-ink-muted">Flights podium</p></div>
            </div>
          </div>
        </div>

        <div class="flex flex-col gap-3">
          <div class="flex flex-wrap items-end justify-between gap-2">
            <h2 class="text-lg font-bold text-ink">Pilots of the month</h2>
            <p class="text-xs text-ink-muted">Approved flights only. The top 3 of each podium earn +{{ s.bonus.join(' / +') }} points when the month closes.</p>
          </div>
          <div class="grid gap-6 lg:grid-cols-2">
            <app-pilot-podium [pilots]="s.monthHours" title="Most hours" icon="ti-clock" unit="minutes" [bonus]="s.bonus" [subtitle]="monthName(s.month.month)" />
            <app-pilot-podium [pilots]="s.monthFlights" title="Most flights" icon="ti-plane-departure" unit="flights" [bonus]="s.bonus" [subtitle]="monthName(s.month.month)" />
          </div>

          @if (s.lastAwards; as awards) {
            <div class="panel flex flex-col gap-3 p-5">
              <h3 class="flex items-center gap-2 font-semibold text-ink">
                <i class="ti ti-award text-lg text-sol-400" aria-hidden="true"></i>Winners of {{ monthName(awards.month) }} {{ awards.year }}
              </h3>
              <div class="grid gap-4 md:grid-cols-2">
                @for (group of awardGroups(); track group.category) {
                  <div>
                    <p class="mb-2 text-xs font-semibold uppercase tracking-wide text-ink-muted">{{ group.category === 'Hours' ? 'Most hours' : 'Most flights' }}</p>
                    <ul class="flex flex-col gap-1.5">
                      @for (w of group.winners; track w.position) {
                        <li class="flex items-center gap-3 text-sm">
                          <span class="flex h-6 w-6 items-center justify-center rounded-full text-xs font-bold" [class]="medal(w.position)">{{ w.position }}</span>
                          <a [routerLink]="['/profile', w.pilotNumber]" class="min-w-0 flex-1 truncate font-medium text-ink hover:underline">{{ w.name }}</a>
                          <span class="font-mono text-xs text-ink-muted">{{ w.category === 'Hours' ? format(w.value) : w.value + ' flights' }}</span>
                          <span class="rounded-full bg-onda-500/15 px-2 py-0.5 text-[11px] font-semibold text-onda-600">+{{ w.points }}</span>
                        </li>
                      }
                    </ul>
                  </div>
                }
              </div>
            </div>
          }
        </div>

        <div class="panel card-hover p-5">
          <app-bar-chart [points]="s.monthly" title="Last 12 months" />
        </div>

        <div class="panel flex flex-col">
          <div class="flex flex-wrap items-center justify-between gap-3 border-b border-line px-5 py-4">
            <h2 class="flex items-center gap-2 font-semibold text-ink"><i class="ti ti-trophy text-lg text-primary" aria-hidden="true"></i>All-time ranking</h2>
            <div class="flex rounded-lg border border-line p-0.5 text-xs font-semibold">
              @for (t of rankingTabs; track t.value) {
                <button type="button" class="rounded-md px-3 py-1" (click)="ranking.set(t.value)"
                  [class]="ranking() === t.value ? 'bg-primary text-on-primary' : 'text-ink-muted hover:text-ink'">{{ t.label }}</button>
              }
            </div>
          </div>
          @if (rankingRows().length === 0) {
            <p class="px-5 py-10 text-center text-sm text-ink-muted">No pilots ranked yet.</p>
          } @else {
            <ul class="flex flex-col divide-y divide-line">
              @for (p of rankingRows(); track p.pilotNumber) {
                <li class="flex items-center gap-4 px-5 py-3" [class.bg-primary/10]="p.isMe">
                  <span class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full font-mono text-sm font-bold" [class]="medal(p.position)">{{ p.position }}</span>
                  <span class="flex h-9 w-9 shrink-0 items-center justify-center overflow-hidden rounded-full bg-primary text-xs font-bold text-on-primary">
                    @if (p.photoUrl) {
                      <img [src]="p.photoUrl" alt="" class="h-full w-full object-cover" />
                    } @else {
                      {{ initials(p.name) }}
                    }
                  </span>
                  <div class="min-w-0 flex-1">
                    <a [routerLink]="['/profile', p.pilotNumber]" class="block truncate font-semibold text-ink hover:text-primary hover:underline">{{ p.name }}</a>
                    <p class="font-mono text-xs text-ink-muted">{{ p.displayId }}</p>
                  </div>
                  <span class="font-mono font-semibold text-ink">{{ rankingValue(p) }}</span>
                </li>
              }
            </ul>
            @if (myRankingPosition(); as pos) {
              <p class="border-t border-line px-5 py-3 text-sm text-ink-muted">Your position: <span class="font-mono font-semibold text-primary">#{{ pos }}</span></p>
            }
          }
        </div>

        <div class="grid gap-6 md:grid-cols-2 xl:grid-cols-4">
          <app-bar-list [items]="s.topRoutes" title="Top routes" icon="ti-route" [mono]="true" />
          <app-bar-list [items]="s.topAirports" title="Top airports" icon="ti-building-airport" [mono]="true" />
          <app-bar-list [items]="s.fleet" title="Fleet usage" icon="ti-plane" />
          <app-bar-list [items]="s.networks" title="Networks" icon="ti-broadcast" />
        </div>
      }
    </section>
  `
})
export class Statistics implements OnInit {
  private readonly api = inject(StatisticsService);

  protected readonly format = formatMinutes;
  protected readonly rankingTabs: { value: RankingTab; label: string }[] = [
    { value: 'hours', label: 'Hours' },
    { value: 'flights', label: 'Flights' },
    { value: 'points', label: 'Points' }
  ];

  protected readonly data = signal<StatisticsData | null>(null);
  protected readonly loading = signal(true);
  protected readonly ranking = signal<RankingTab>('hours');

  protected readonly totals = computed(() => {
    const t = this.data()?.totals;
    if (!t) return [];
    return [
      { icon: 'ti-users', label: 'Active pilots', value: t.pilots.toLocaleString('en-US') },
      { icon: 'ti-plane-departure', label: 'Flights', value: t.flights.toLocaleString('en-US') },
      { icon: 'ti-clock', label: 'Hours', value: Math.floor(t.minutes / 60).toLocaleString('en-US') },
      { icon: 'ti-route', label: 'Distance (NM)', value: t.distanceNm.toLocaleString('en-US') },
      { icon: 'ti-plane-arrival', label: 'Avg landing', value: t.averageLandingRate !== null ? `${t.averageLandingRate} fpm` : '—' },
      { icon: 'ti-building-airport', label: 'Aircraft · Airports', value: `${t.aircraft} · ${t.airports}` }
    ];
  });

  protected readonly myPositions = computed(() => {
    const me = this.data()?.me;
    return [
      { label: 'Hours rank', value: me?.hoursPosition ?? null },
      { label: 'Flights rank', value: me?.flightsPosition ?? null },
      { label: 'Points rank', value: me?.pointsPosition ?? null }
    ];
  });

  protected readonly rankingRows = computed(() => {
    const s = this.data();
    if (!s) return [];
    return this.ranking() === 'hours' ? s.topHours : this.ranking() === 'flights' ? s.topFlights : s.topPoints;
  });

  protected readonly myRankingPosition = computed(() => {
    const me = this.data()?.me;
    if (!me || this.rankingRows().some(r => r.isMe)) return null;
    return this.ranking() === 'hours' ? me.hoursPosition : this.ranking() === 'flights' ? me.flightsPosition : me.pointsPosition;
  });

  protected readonly awardGroups = computed(() => {
    const winners = this.data()?.lastAwards?.winners ?? [];
    return (['Hours', 'Flights'] as const).map(category => ({ category, winners: winners.filter(w => w.category === category) }));
  });

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    try {
      this.data.set(await this.api.get());
    } catch {
      this.data.set(null);
    } finally {
      this.loading.set(false);
    }
  }

  protected rankingValue(p: RankedPilot): string {
    if (this.ranking() === 'hours') return formatMinutes(p.value);
    if (this.ranking() === 'flights') return `${p.value}`;
    return p.value.toLocaleString('en-US');
  }

  protected monthName(month: number): string {
    return MONTHS[month - 1] ?? '';
  }

  protected medal(position: number): string {
    return position === 1 ? 'bg-sol-400 text-atlantico-900'
      : position === 2 ? 'bg-slate-300 text-slate-800'
      : position === 3 ? 'bg-orange-300 text-orange-950'
      : 'bg-surface-alt text-ink-muted';
  }

  protected initials(name: string): string {
    return name.split(/[\s.]+/).filter(Boolean).map(p => p[0]).slice(0, 2).join('').toUpperCase();
  }
}