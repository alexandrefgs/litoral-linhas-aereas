import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { apiErrorMessage } from '../../core/api/api-error';
import {
  BOOKING_TYPE_LABEL,
  FlightCrewMember,
  FlightDetail as FlightDetailModel,
  hhmm
} from '../../core/flights/flight.models';
import { FlightService } from '../../core/flights/flight.service';
import { NETWORK_LABELS, PIREP_STATUS_CLASS, PirepScoreItem } from '../../core/pirep/pirep.models';
import { CrewCard } from '../../shared/crew-card/crew-card';
import { TrackMap } from '../../shared/track-map/track-map';
import { countryFlagUrl } from '../../shared/utils/country-flag';
import { FlightChart } from './flight-chart';

interface TimelineEntry {
  at: string;
  icon: string;
  tone: string;
  title: string;
  detail: string | null;
}

const EVENT_STYLE: Record<string, { icon: string; title: string }> = {
  Overspeed: { icon: 'ti-gauge', title: 'Overspeed' },
  Slew: { icon: 'ti-arrows-move', title: 'Slew mode' },
  Stall: { icon: 'ti-alert-triangle', title: 'Stall' },
  Pause: { icon: 'ti-player-pause', title: 'Simulator paused' },
  SimulationRate: { icon: 'ti-clock-bolt', title: 'Simulation rate changed' },
  Refuel: { icon: 'ti-gas-station', title: 'Refuel in flight' },
  Crash: { icon: 'ti-flame', title: 'Crash' },
  Other: { icon: 'ti-info-circle', title: 'Event' }
};

@Component({
  selector: 'app-flight-detail',
  imports: [DatePipe, DecimalPipe, CrewCard, TrackMap, FlightChart],
  template: `
    <section class="mx-auto flex max-w-7xl flex-col gap-6 px-6 py-8">
      @if (loading()) {
        <p class="panel px-6 py-16 text-center text-ink-muted">Loading...</p>
      } @else if (error()) {
        <p class="panel px-6 py-16 text-center text-danger">{{ error() }}</p>
      } @else if (flight(); as f) {
        <div class="panel flex flex-wrap items-center justify-between gap-4 border-t-4 border-t-primary px-8 py-6">
          <p class="font-mono text-4xl font-extrabold text-primary">{{ f.callsign }}</p>
          <div class="flex items-center gap-4 font-mono text-4xl font-extrabold text-ink">
            <span [title]="f.departure.name">{{ f.departure.icao }}</span>
            <i class="ti ti-plane text-3xl text-primary" aria-hidden="true"></i>
            <span [title]="f.arrival.name">{{ f.arrival.icao }}</span>
          </div>
          <div class="flex flex-col items-end gap-1">
            <span class="rounded-full px-3 py-1 text-sm font-semibold" [class]="statusClass[f.status]">{{ f.status }}</span>
            @if (f.arrival.icao !== f.plannedArrivalIcao) {
              <span class="text-xs text-danger">Diverted from {{ f.plannedArrivalIcao }}</span>
            }
          </div>
        </div>

        <div class="grid gap-6 lg:grid-cols-2 xl:grid-cols-[1fr_1fr_2fr]">
          @for (member of crew(); track member.role) {
            <app-crew-card [member]="member.data" [role]="member.role" />
          }

          <div class="relative overflow-hidden rounded-xl border border-line border-t-4 border-t-primary bg-atlantico-900 bg-cover bg-center text-white card-hover lg:col-span-2 xl:col-span-1"
            [style.background-image]="aircraftCover()">
            <div class="flex h-full flex-col gap-4 bg-gradient-to-r from-black/85 via-black/65 to-black/30 p-6">
              <div class="flex items-start justify-between gap-4">
                <div>
                  <p class="text-sm font-semibold text-white/80">{{ f.blockOffAt | date: 'MMM d, y · HH:mm' : 'UTC' }} UTC</p>
                  <p class="flex items-center gap-3 font-mono text-4xl font-extrabold">
                    {{ f.aircraft.registration }}
                    @if (flag(f.departure.country); as url) {
                      <img [src]="url" alt="" class="h-5 w-auto rounded-sm" />
                    }
                  </p>
                  <p class="text-white/70">{{ f.aircraft.typeName }}{{ f.aircraft.variant ? ' · ' + f.aircraft.variant : '' }}</p>
                </div>
                <div class="rounded-lg bg-black/40 px-4 py-2 text-center">
                  <p class="text-[10px] uppercase tracking-wide text-white/70">Conformity</p>
                  <p class="text-3xl font-extrabold" [class]="f.hasRejection ? 'text-danger' : 'text-onda-300'">{{ f.conformity }}</p>
                  <p class="text-xs tracking-tight text-sol-300">
                    @for (star of stars; track star) {
                      <span [class.opacity-30]="star > starCount()">★</span>
                    }
                  </p>
                </div>
              </div>

              <div class="flex flex-wrap gap-2 text-xs font-semibold">
                <span class="rounded-md bg-white/15 px-2 py-1">{{ bookingLabel[f.bookingType] }}</span>
                <span class="rounded-md bg-white/15 px-2 py-1">{{ f.aircraft.isCargo ? 'Cargo' : 'Passengers' }}</span>
                <span class="rounded-md bg-white/15 px-2 py-1">{{ f.source === 'Manual' ? 'Manual PIREP' : 'ACARS' }}</span>
                @if (f.simulator) {
                  <span class="rounded-md bg-white/15 px-2 py-1">{{ f.simulator }}</span>
                }
              </div>

              <div class="mt-auto grid grid-cols-2 gap-x-4 gap-y-3 rounded-lg bg-black/35 p-4 sm:grid-cols-3">
                @for (stat of stats(); track stat.label) {
                  <div class="flex items-center gap-3">
                    <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-white/10 text-lg text-sol-300"><i class="ti {{ stat.icon }}" aria-hidden="true"></i></span>
                    <div class="min-w-0">
                      <p class="text-xs text-white/70">{{ stat.label }}</p>
                      <p class="truncate font-semibold">{{ stat.value }}</p>
                    </div>
                  </div>
                }
              </div>
              @if (f.aircraft.photoCredit) {
                <p class="text-right text-[10px] text-white/60">© {{ f.aircraft.photoCredit }}</p>
              }
            </div>
          </div>
        </div>

        @if (f.reviewNote || f.comments) {
          <div class="grid gap-4 md:grid-cols-2">
            @if (f.comments) {
              <div class="panel p-5 text-sm"><p class="mb-1 font-semibold text-ink">Pilot comments</p><p class="whitespace-pre-line text-ink-muted">{{ f.comments }}</p></div>
            }
            @if (f.reviewNote) {
              <div class="panel p-5 text-sm"><p class="mb-1 font-semibold text-ink">Safety review</p><p class="whitespace-pre-line text-ink-muted">{{ f.reviewNote }}</p></div>
            }
          </div>
        }

        <div class="grid gap-6 lg:grid-cols-2">
          <div class="panel overflow-hidden">
            <app-track-map [track]="f.track" [departure]="f.departure" [arrival]="f.arrival" class="h-[460px]" />
          </div>

          <div class="panel flex flex-col">
            <div class="border-b border-line px-6 py-4">
              <h2 class="text-lg font-bold text-ink">Flight log</h2>
            </div>
            <ol class="flex max-h-[400px] flex-col gap-1 overflow-y-auto px-4 py-3">
              @for (entry of timeline(); track $index) {
                <li class="flex items-start gap-3 rounded-lg px-2 py-2 hover:bg-surface-alt">
                  <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full border text-lg" [class]="entry.tone">
                    <i class="ti {{ entry.icon }}" aria-hidden="true"></i>
                  </span>
                  <div class="min-w-0 flex-1">
                    <p class="font-medium text-ink">{{ entry.title }}</p>
                    @if (entry.detail) {
                      <p class="text-sm text-ink-muted">{{ entry.detail }}</p>
                    }
                  </div>
                  <span class="whitespace-nowrap font-mono text-xs text-ink-muted">{{ entry.at | date: 'd MMM HH:mm' : 'UTC' }}Z</span>
                </li>
              }
            </ol>
          </div>
        </div>

        <div class="panel p-6">
          <div class="mb-4 flex flex-wrap items-end justify-between gap-2">
            <div>
              <h2 class="text-lg font-bold text-ink">Flight profile</h2>
              <p class="text-sm" [class]="(f.onlinePercent ?? 0) >= 80 ? 'text-onda-500' : 'text-ink-muted'">
                {{ f.network === 'Offline' ? 'Flown offline' : (f.onlinePercent ?? 0 | number: '1.0-0') + '% online on ' + networks[f.network] }}
              </p>
            </div>
            @if (f.flightRoute) {
              <p class="max-w-2xl truncate rounded-lg bg-surface-alt px-3 py-1.5 font-mono text-xs text-ink" [title]="f.flightRoute">{{ f.flightRoute }}</p>
            }
          </div>
          <app-flight-chart [track]="f.track" />
        </div>

        <div class="panel p-6">
          <div class="mb-5 flex flex-wrap items-center justify-between gap-4">
            <div class="flex items-center gap-3">
              <span class="flex h-12 w-12 items-center justify-center rounded-full bg-primary text-2xl text-on-primary"><i class="ti ti-robot" aria-hidden="true"></i></span>
              <div>
                <p class="font-bold text-ink">Scoring</p>
                <p class="text-sm text-ink-muted">Automatic and Safety review items</p>
              </div>
            </div>
            <div class="flex items-center gap-4">
              <div class="flex h-16 w-16 flex-col items-center justify-center rounded-full bg-surface-alt">
                <span class="text-lg font-extrabold" [class]="f.hasRejection ? 'text-danger' : 'text-onda-500'">{{ f.hasRejection ? '✕' : f.scorePoints }}</span>
                <span class="text-[10px] text-ink-muted">Points</span>
              </div>
              <div class="text-xs text-ink-muted">
                <p>Highest: {{ scoreSummary().highest }} pts</p>
                <p>Lowest: {{ scoreSummary().lowest }} pts</p>
                <p>Items: {{ f.items.length }}</p>
              </div>
            </div>
          </div>

          @if (rejections().length) {
            <div class="mb-4 flex flex-col gap-2">
              @for (item of rejections(); track $index) {
                <p class="flex items-center gap-2 rounded-lg bg-danger/10 px-3 py-2 text-sm font-medium text-danger">
                  <i class="ti ti-circle-x" aria-hidden="true"></i>{{ item.description }}
                </p>
              }
            </div>
          }

          @if (pointItems().length) {
            <div class="flex flex-col gap-2">
              @for (item of pointItems(); track $index) {
                <div class="grid grid-cols-[minmax(0,220px)_1fr] items-center gap-3 text-sm">
                  <p class="truncate text-ink-muted" [title]="item.description">{{ item.description }}{{ item.quantity > 1 ? ' ×' + item.quantity : '' }}</p>
                  <div class="h-6 rounded bg-surface-alt">
                    <div class="flex h-full items-center justify-center rounded text-xs font-bold text-white"
                      [class]="item.points >= 0 ? 'bg-onda-500' : 'bg-danger'"
                      [style.width.%]="barWidth(item)">
                      {{ item.points > 0 ? '+' : '' }}{{ item.points }} pts
                    </div>
                  </div>
                </div>
              }
            </div>
          } @else if (!rejections().length) {
            <p class="py-6 text-center text-sm text-ink-muted">No scoring items for this flight.</p>
          }

          <p class="mt-5 rounded-lg bg-surface-alt px-4 py-2 text-sm text-ink">
            Total {{ f.hasRejection ? 'rejected' : f.scorePoints + ' points' }} for {{ f.callsign }} · {{ f.departure.icao }} → {{ f.arrival.icao }}
            <span class="text-ink-muted">· filed {{ f.submittedAt | date: 'd MMM y, HH:mm' : 'UTC' }}Z</span>
          </p>
        </div>
      }
    </section>
  `
})
export class FlightDetail {
  private readonly api = inject(FlightService);
  private readonly route = inject(ActivatedRoute);

  protected readonly statusClass = PIREP_STATUS_CLASS;
  protected readonly networks = NETWORK_LABELS;
  protected readonly bookingLabel = BOOKING_TYPE_LABEL;
  protected readonly flag = countryFlagUrl;
  protected readonly stars = [1, 2, 3, 4, 5];

  protected readonly flight = signal<FlightDetailModel | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly starCount = computed(() => Math.round((this.flight()?.conformity ?? 0) / 20));

  protected readonly crew = computed(() => {
    const f = this.flight();
    if (!f) return [];
    return [
      { role: 'Captain', data: f.captain },
      { role: 'First Officer', data: f.firstOfficer }
    ] as { role: string; data: FlightCrewMember }[];
  });

  protected readonly aircraftCover = computed(() => {
    const url = this.flight()?.aircraft.photoUrl;
    return url ? `url('${url}')` : 'linear-gradient(135deg, #0F3768 0%, #1D6FD8 60%, #2EC4B6 100%)';
  });

  protected readonly stats = computed(() => {
    const f = this.flight();
    if (!f) return [];
    return [
      { icon: 'ti-clock', label: 'Block time', value: hhmm(f.blockMinutes) },
      { icon: 'ti-ruler-2', label: 'Distance', value: f.distanceNm !== null ? `${f.distanceNm} NM` : '—' },
      { icon: 'ti-star', label: 'Points', value: f.hasRejection ? 'Rejected' : `${f.scorePoints} pts` },
      { icon: 'ti-gas-station', label: 'Fuel used', value: f.fuelUsedKg !== null ? `${Math.round(f.fuelUsedKg).toLocaleString('en-US')} kg` : '—' },
      { icon: 'ti-arrow-bar-to-up', label: 'Cruise', value: f.cruiseAltitudeFt ? `FL${Math.round(f.cruiseAltitudeFt / 100)}` : '—' },
      { icon: 'ti-world', label: 'Network', value: f.network === 'Offline' ? 'Offline' : `${this.networks[f.network]} ${Math.round(f.onlinePercent ?? 0)}%` }
    ];
  });

  protected readonly rejections = computed(() => (this.flight()?.items ?? []).filter(i => i.outcome === 'Reject'));
  protected readonly pointItems = computed(() => (this.flight()?.items ?? []).filter(i => i.outcome !== 'Reject'));

  protected readonly scoreSummary = computed(() => {
    const values = this.pointItems().map(i => i.points);
    return values.length
      ? { highest: Math.max(...values), lowest: Math.min(...values) }
      : { highest: 0, lowest: 0 };
  });

  private readonly maxPoints = computed(() => Math.max(1, ...this.pointItems().map(i => Math.abs(i.points))));

  protected readonly timeline = computed<TimelineEntry[]>(() => {
    const f = this.flight();
    if (!f) return [];

    const ok = 'border-onda-500/40 bg-onda-500/10 text-onda-500';
    const info = 'border-primary/40 bg-primary/10 text-primary';
    const warn = 'border-danger/40 bg-danger/10 text-danger';

    const entries: TimelineEntry[] = [
      { at: f.blockOffAt, icon: 'ti-arrow-back-up', tone: info, title: 'Block off', detail: f.fuelAtPushbackKg !== null ? `Pushback with ${Math.round(f.fuelAtPushbackKg).toLocaleString('en-US')} kg of fuel` : null },
      { at: f.blockOnAt, icon: 'ti-parking', tone: info, title: 'Block on', detail: `Block time ${hhmm(f.blockMinutes)}` },
      { at: f.submittedAt, icon: 'ti-send', tone: info, title: 'PIREP filed', detail: f.source === 'Manual' ? 'Manual PIREP' : 'Sent by ACARS' }
    ];

    if (f.takeoffAt) entries.push({ at: f.takeoffAt, icon: 'ti-plane-departure', tone: ok, title: `Takeoff from ${f.departure.icao}`, detail: null });
    if (f.landingAt) {
      entries.push({
        at: f.landingAt,
        icon: 'ti-plane-arrival',
        tone: ok,
        title: `Landing at ${f.arrival.icao}`,
        detail: f.landingRateFpm !== null ? `${f.landingRateFpm} fpm${f.airMinutes !== null ? ' · air time ' + hhmm(f.airMinutes) : ''}` : null
      });
    }
    if (f.reviewedAt) {
      entries.push({
        at: f.reviewedAt,
        icon: f.status === 'Approved' ? 'ti-circle-check' : 'ti-circle-x',
        tone: f.status === 'Approved' ? ok : warn,
        title: `PIREP ${f.status.toLowerCase()} by Safety`,
        detail: null
      });
    }

    for (const event of f.events) {
      const style = EVENT_STYLE[event.type] ?? EVENT_STYLE['Other'];
      const parts = [event.detail, event.durationSeconds ? `${event.durationSeconds} s` : null].filter(Boolean);
      entries.push({ at: event.at, icon: style.icon, tone: warn, title: style.title, detail: parts.join(' · ') || null });
    }

    return entries.sort((a, b) => new Date(a.at).getTime() - new Date(b.at).getTime());
  });

  constructor() {
    this.route.paramMap
      .pipe(takeUntilDestroyed(inject(DestroyRef)))
      .subscribe(params => this.load(params.get('id') ?? ''));
  }

  protected barWidth(item: PirepScoreItem): number {
    return Math.max(12, (Math.abs(item.points) / this.maxPoints()) * 100);
  }

  private async load(id: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.flight.set(await this.api.get(id));
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Could not load the flight.');
    } finally {
      this.loading.set(false);
    }
  }
}