import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { apiErrorMessage } from '../../../core/api/api-error';
import { Report } from '../../../core/statistics/statistics.models';
import { StatisticsService } from '../../../core/statistics/statistics.service';
import { BarChart } from '../../../shared/charts/bar-chart';
import { BarList } from '../../../shared/charts/bar-list';
import { formatMinutes } from '../../../shared/utils/flight';

type Preset = 'month' | 'lastMonth' | '90days' | 'year' | 'custom';

const BOOKING_LABELS: Record<string, string> = { Random: 'Random', Custom: 'Custom', Monthly: 'Monthly roster', Special: 'Special flight', Tour: 'Tour leg' };

@Component({
  selector: 'app-admin-reports',
  imports: [DatePipe, RouterLink, BarChart, BarList],
  template: `
    <div class="flex flex-col gap-6">
      <div>
        <h1 class="text-2xl font-bold text-ink">Reports</h1>
        <p class="text-ink-muted">Flights, PIREP reviews and pilots for the selected period (UTC).</p>
      </div>

      <div class="flex flex-wrap items-end gap-2">
        @for (p of presets; track p.value) {
          <button type="button" class="rounded-full border px-3 py-1 text-sm font-medium" (click)="choose(p.value)"
            [class]="preset() === p.value ? 'border-primary bg-primary text-on-primary' : 'border-line text-ink hover:bg-surface-alt'">{{ p.label }}</button>
        }
        @if (preset() === 'custom') {
          <div class="flex flex-wrap items-end gap-2">
            <div>
              <label for="reportFrom" class="field-label">From</label>
              <input id="reportFrom" type="date" class="field-input py-1" [value]="customFrom()" (change)="customFrom.set($any($event.target).value)" />
            </div>
            <div>
              <label for="reportTo" class="field-label">To</label>
              <input id="reportTo" type="date" class="field-input py-1" [value]="customTo()" (change)="customTo.set($any($event.target).value)" />
            </div>
            <button type="button" class="btn-primary py-1.5" [disabled]="!customFrom() || !customTo()" (click)="load()">Apply</button>
          </div>
        }
      </div>

      @if (error(); as message) {
        <p class="rounded-lg bg-danger/10 px-4 py-3 text-sm text-danger">{{ message }}</p>
      }

      @if (loading() && !report()) {
        <p class="panel px-6 py-16 text-center text-ink-muted">Loading...</p>
      } @else if (report(); as r) {
        <p class="text-sm text-ink-muted" [class.opacity-60]="loading()">
          {{ r.from | date: 'd MMM y' : 'UTC' }} – {{ lastDay(r.to) | date: 'd MMM y' : 'UTC' }}
        </p>

        @for (group of groups(); track group.title) {
          <section class="flex flex-col gap-3" [class.opacity-60]="loading()">
            <h2 class="flex items-center gap-2 text-sm font-semibold uppercase tracking-wide text-ink-muted">
              <i class="ti {{ group.icon }} text-lg" aria-hidden="true"></i>{{ group.title }}
            </h2>
            <div class="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-4">
              @for (kpi of group.items; track kpi.label) {
                <div class="panel card-hover p-4">
                  <p class="text-xl font-bold" [class]="kpi.tone ?? 'text-ink'">{{ kpi.value }}</p>
                  <p class="text-xs text-ink-muted">{{ kpi.label }}</p>
                </div>
              }
            </div>
          </section>
        }

        <div class="panel card-hover p-5" [class.opacity-60]="loading()">
          <app-bar-chart [points]="r.timeline" title="Flights in the period" />
        </div>

        <div class="grid gap-6 xl:grid-cols-2" [class.opacity-60]="loading()">
          <div class="panel flex flex-col">
            <h3 class="flex items-center gap-2 border-b border-line px-5 py-4 font-semibold text-ink">
              <i class="ti ti-trophy text-lg text-primary" aria-hidden="true"></i>Top pilots by hours
            </h3>
            @if (r.topPilots.length === 0) {
              <p class="px-5 py-10 text-center text-sm text-ink-muted">No flights in this period.</p>
            } @else {
              <ul class="flex flex-col divide-y divide-line">
                @for (p of r.topPilots; track p.pilotNumber) {
                  <li class="flex items-center gap-3 px-5 py-2.5 text-sm">
                    <span class="w-6 text-center font-mono text-ink-muted">{{ p.position }}</span>
                    <a [routerLink]="['/profile', p.pilotNumber]" class="min-w-0 flex-1 truncate font-medium text-ink hover:underline">{{ p.name }}</a>
                    <span class="font-mono text-xs text-ink-muted">{{ p.displayId }}</span>
                    <span class="w-16 text-right font-mono font-semibold text-ink">{{ format(p.value) }}</span>
                  </li>
                }
              </ul>
            }
          </div>
          <div class="grid gap-6 md:grid-cols-2">
            <app-bar-list [items]="r.networks" title="Networks" icon="ti-broadcast" />
            <app-bar-list [items]="bookingTypes()" title="Booking types" icon="ti-ticket" />
            <app-bar-list class="md:col-span-2" [items]="r.topRoutes" title="Top routes" icon="ti-route" [mono]="true" />
          </div>
        </div>
      }
    </div>
  `
})
export class AdminReports implements OnInit {
  private readonly api = inject(StatisticsService);

  protected readonly format = formatMinutes;
  protected readonly presets: { value: Preset; label: string }[] = [
    { value: 'month', label: 'This month' },
    { value: 'lastMonth', label: 'Last month' },
    { value: '90days', label: 'Last 90 days' },
    { value: 'year', label: 'This year' },
    { value: 'custom', label: 'Custom' }
  ];

  protected readonly preset = signal<Preset>('month');
  protected readonly customFrom = signal('');
  protected readonly customTo = signal('');
  protected readonly report = signal<Report | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly bookingTypes = computed(() =>
    (this.report()?.bookingTypes ?? []).map(b => ({ ...b, label: BOOKING_LABELS[b.key] ?? b.label })));

  protected readonly groups = computed(() => {
    const r = this.report();
    if (!r) return [];
    const hours = (h: number | null) => (h === null ? '—' : h >= 48 ? `${Math.round(h / 24)} days` : `${h} h`);
    return [
      {
        title: 'Flights',
        icon: 'ti-plane-departure',
        items: [
          { label: 'Flights', value: r.flights.flights.toLocaleString('en-US') },
          { label: 'Hours', value: formatMinutes(r.flights.minutes) },
          { label: 'Distance (NM)', value: r.flights.distanceNm.toLocaleString('en-US') },
          { label: 'Pilots who flew', value: `${r.flights.activePilots}` },
          { label: 'Avg landing rate', value: r.flights.averageLandingRate !== null ? `${r.flights.averageLandingRate} fpm` : '—' }
        ]
      },
      {
        title: 'PIREP reviews',
        icon: 'ti-clipboard-check',
        items: [
          { label: 'Pending now', value: `${r.pireps.pendingNow}`, tone: r.pireps.pendingNow > 0 ? 'text-highlight' : undefined },
          { label: 'Oldest pending', value: hours(r.pireps.oldestPendingHours), tone: (r.pireps.oldestPendingHours ?? 0) > 72 ? 'text-danger' : undefined },
          { label: 'Approved', value: `${r.pireps.approved}`, tone: 'text-onda-600' },
          { label: 'Rejected', value: `${r.pireps.rejected}`, tone: r.pireps.rejected > 0 ? 'text-danger' : undefined },
          { label: 'Rejection rate', value: `${r.pireps.rejectionPercent}%` },
          { label: 'Avg conformity', value: r.pireps.averageConformity !== null ? `${r.pireps.averageConformity}%` : '—' },
          { label: 'Avg review time', value: hours(r.pireps.averageReviewHours) },
          { label: 'Manual PIREPs', value: `${r.pireps.manual}` }
        ]
      },
      {
        title: 'Pilots',
        icon: 'ti-users',
        items: [
          { label: 'Total pilots', value: `${r.pilots.total}` },
          { label: 'Active', value: `${r.pilots.active}`, tone: 'text-onda-600' },
          { label: 'Inactive', value: `${r.pilots.inactive}` },
          { label: 'New sign-ups', value: `${r.pilots.newSignups}` },
          { label: 'No flight in 30+ days', value: `${r.pilots.idle30Days}`, tone: r.pilots.idle30Days > 0 ? 'text-highlight' : undefined },
          { label: 'Never flown', value: `${r.pilots.neverFlown}` }
        ]
      }
    ];
  });

  ngOnInit(): void {
    this.load();
  }

  protected choose(value: Preset): void {
    this.preset.set(value);
    if (value !== 'custom') this.load();
  }

  protected async load(): Promise<void> {
    const range = this.range();
    if (!range) return;

    this.loading.set(true);
    this.error.set(null);
    try {
      this.report.set(await this.api.report(range.from, range.to));
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.loading.set(false);
    }
  }

  protected lastDay(to: string): Date {
    return new Date(new Date(to).getTime() - 1);
  }

  private range(): { from: Date; to: Date } | null {
    const now = new Date();
    const y = now.getUTCFullYear();
    const m = now.getUTCMonth();
    const tomorrow = new Date(Date.UTC(y, m, now.getUTCDate() + 1));

    switch (this.preset()) {
      case 'month': return { from: new Date(Date.UTC(y, m, 1)), to: tomorrow };
      case 'lastMonth': return { from: new Date(Date.UTC(y, m - 1, 1)), to: new Date(Date.UTC(y, m, 1)) };
      case '90days': return { from: new Date(Date.UTC(y, m, now.getUTCDate() - 89)), to: tomorrow };
      case 'year': return { from: new Date(Date.UTC(y, 0, 1)), to: tomorrow };
      case 'custom': {
        if (!this.customFrom() || !this.customTo()) return null;
        const from = new Date(`${this.customFrom()}T00:00:00Z`);
        const to = new Date(new Date(`${this.customTo()}T00:00:00Z`).getTime() + 86_400_000);
        return { from, to };
      }
    }
  }
}