import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { Airport, RankInfo } from '../../core/reference/reference.models';
import { ReferenceService } from '../../core/reference/reference.service';
import { STAFF_POSITION_NAMES } from '../../core/staff/staff-positions';
import { AirportMap } from '../../shared/airport-map/airport-map';
import { LiveFlights } from '../../shared/live-flights/live-flights';
import { FlightTimePipe } from '../../shared/pipes/flight-time.pipe';
import { LocaleNumberPipe } from '../../shared/pipes/locale-number.pipe';
import { RecentFlights } from '../../shared/recent-flights/recent-flights';
import { EventsBanner } from './events-banner';
import { MyLastFlights } from './my-last-flights';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, FlightTimePipe, LocaleNumberPipe, AirportMap, LiveFlights, RecentFlights, MyLastFlights, EventsBanner],
  template: `
    @if (auth.pilot(); as pilot) {
      <section class="mx-auto flex max-w-7xl flex-col gap-6 px-6 py-8">
        @if (!pilot.simBriefUsername) {
          <div class="flex flex-wrap items-center justify-between gap-3 rounded-xl bg-highlight px-5 py-3 text-on-highlight">
            <p class="flex items-center gap-2 font-medium">
              <i class="ti ti-alert-triangle text-xl" aria-hidden="true"></i>Add your SimBrief username in your profile to book flights.
            </p>
            <a routerLink="/profile" class="rounded-md bg-surface px-4 py-2 text-sm font-medium text-ink hover:bg-surface-alt">Go to profile</a>
          </div>
        }
        
        <app-events-banner />

        <div class="grid gap-6 lg:grid-cols-[360px_minmax(0,1fr)]">
          <aside class="panel card-hover overflow-hidden">
            @if (airport(); as ap) {
              <app-airport-map class="h-56" [latitude]="ap.latitude" [longitude]="ap.longitude" [label]="ap.icao" />
            } @else {
              <div class="flex h-56 items-center justify-center bg-surface-alt text-ink-muted">
                <i class="ti ti-map-pin text-4xl" aria-hidden="true"></i>
              </div>
            }
            <div class="flex flex-col gap-4 p-5">
              <div class="flex items-center justify-between">
                <span class="rounded-md bg-surface-alt px-2 py-1 text-sm text-ink-muted">Current location</span>
                @if (airport()?.isHub) {
                  <span class="rounded-md bg-primary px-2 py-0.5 text-xs font-bold text-on-primary">HUB</span>
                }
              </div>
              <div>
                <p class="text-4xl font-extrabold text-ink">{{ pilot.currentLocationIcao }}</p>
                @if (airport(); as ap) {
                  <p class="mt-1 text-sm font-medium text-ink">{{ ap.name }}</p>
                  <p class="text-sm text-ink-muted">{{ ap.city }}, {{ ap.country }}</p>
                }
              </div>
              <div class="grid grid-cols-2 gap-3 border-t border-line pt-4">
                <div class="flex items-center gap-3">
                  <span class="stat-icon"><i class="ti ti-plane" aria-hidden="true"></i></span>
                  <div>
                    <p class="text-sm text-ink">Aircraft</p>
                    <p class="text-sm font-semibold text-ink-muted">{{ airport()?.aircraftOnGround ?? 0 }}</p>
                  </div>
                </div>
                <div class="flex items-center gap-3">
                  <span class="stat-icon"><i class="ti ti-plane-departure" aria-hidden="true"></i></span>
                  <div>
                    <p class="text-sm text-ink">Departures</p>
                    <p class="text-sm font-semibold text-ink-muted">{{ airport()?.departures ?? 0 }}</p>
                  </div>
                </div>
              </div>
            </div>
          </aside>

          <div class="flex min-w-0 flex-col gap-6">
            <div class="grid gap-4 md:grid-cols-3">
              <div class="panel card-hover stat-card">
                <div class="flex items-start justify-between">
                  <div>
                    <p class="text-2xl font-extrabold text-ink">{{ pilot.rankName }}</p>
                    <p class="text-sm text-ink-muted">
                      Next rank:
                      <span class="font-semibold text-ink">{{ nextRank()?.name ?? 'Max level' }}</span>
                    </p>
                  </div>
                  <img [src]="'images/ranks/' + pilot.rankCode.toLowerCase() + '.png'" [alt]="pilot.rankName" class="h-10 w-auto object-contain" />
                </div>
                <div class="progress"><div class="progress-bar bg-highlight" [style.width.%]="rankProgress()"></div></div>
                <div class="flex justify-between text-xs text-ink-muted">
                  <span>Progress</span><span class="font-semibold">{{ rankProgress() }}%</span>
                </div>
              </div>

              <div class="panel card-hover stat-card">
                <div class="flex items-start justify-between">
                  <div>
                    <p class="text-2xl font-extrabold text-ink">{{ pilot.totalFlightMinutes | flightTime }}</p>
                    <p class="text-sm text-ink-muted">Flight hours</p>
                  </div>
                  <span class="text-sm font-semibold text-ink">{{ hoursProgress() }}%</span>
                </div>
                <div class="progress"><div class="progress-bar bg-primary" [style.width.%]="hoursProgress()"></div></div>
                <div class="flex justify-between text-xs text-ink-muted">
                  <span>Goal</span><span>{{ nextRank()?.minFlightHours ?? '—' }}h</span>
                </div>
              </div>

              <div class="panel card-hover stat-card">
                <div class="flex items-start justify-between">
                  <div>
                    <p class="text-2xl font-extrabold text-ink">{{ pilot.points | localeNumber }}</p>
                    <p class="text-sm text-ink-muted">Points</p>
                  </div>
                  <span class="text-sm font-semibold text-ink">{{ pointsProgress() }}%</span>
                </div>
                <div class="progress"><div class="progress-bar bg-accent" [style.width.%]="pointsProgress()"></div></div>
                <div class="flex justify-between text-xs text-ink-muted">
                  <span>Goal</span><span>{{ nextRank()?.minPoints | localeNumber }}</span>
                </div>
              </div>
            </div>

            <app-my-last-flights class="flex-1" [pilotNumber]="pilot.pilotNumber" />
          </div>
        </div>

        <app-recent-flights />

        <app-live-flights />

        <div class="grid gap-6 lg:grid-cols-3">
          @for (block of rankingBlocks; track block.title) {
            <div class="panel card-hover">
              <div class="flex items-center gap-2 border-b border-line px-6 py-4">
                <i class="ti {{ block.icon }} text-xl text-primary" aria-hidden="true"></i>
                <h2 class="text-lg font-bold text-ink">{{ block.title }}</h2>
              </div>
              <p class="px-6 py-10 text-center text-sm text-ink-muted">Data shows up after the first flights.</p>
            </div>
          }
        </div>

        @if (pilot.staffPositions.length) {
          <div class="flex flex-wrap items-center gap-2">
            <span class="text-sm text-ink-muted">Staff positions:</span>
            @for (position of pilot.staffPositions; track position) {
              <span class="rounded-full bg-accent px-3 py-1 text-sm font-medium text-on-accent">{{ staffNames[position] ?? position }}</span>
            }
          </div>
        }
      </section>
    }
  `
})
export class Dashboard implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly reference = inject(ReferenceService);

  protected readonly staffNames = STAFF_POSITION_NAMES;
  protected readonly ranks = signal<RankInfo[]>([]);
  protected readonly airport = signal<Airport | null>(null);

  protected readonly rankingBlocks = [
    { title: 'Top pilots', icon: 'ti-trophy' },
    { title: 'Monthly statistics', icon: 'ti-chart-pie' },
    { title: 'Top routes', icon: 'ti-route' }
  ];

  protected readonly nextRank = computed(() => {
    const pilot = this.auth.pilot();
    if (!pilot) return null;

    const current = this.ranks().find(r => r.code === pilot.rankCode);
    return this.ranks()
      .filter(r => r.level > (current?.level ?? 0))
      .sort((a, b) => a.level - b.level)[0] ?? null;
  });

  protected readonly hoursProgress = computed(() => {
    const next = this.nextRank();
    const pilot = this.auth.pilot();
    if (!next || !pilot || next.minFlightHours === 0) return 100;
    return Math.min(100, Math.round((pilot.totalFlightMinutes / (next.minFlightHours * 60)) * 100));
  });

  protected readonly pointsProgress = computed(() => {
    const next = this.nextRank();
    const pilot = this.auth.pilot();
    if (!next || !pilot || next.minPoints === 0) return 100;
    return Math.min(100, Math.max(0, Math.round((pilot.points / next.minPoints) * 100)));
  });

  protected readonly rankProgress = computed(() => Math.min(this.hoursProgress(), this.pointsProgress()));

  async ngOnInit(): Promise<void> {
    const icao = this.auth.pilot()?.currentLocationIcao;

    const [ranks, airport] = await Promise.allSettled([
      this.reference.getRanks(),
      icao ? this.reference.getAirport(icao) : Promise.reject()
    ]);

    if (ranks.status === 'fulfilled') this.ranks.set(ranks.value);
    if (airport.status === 'fulfilled') this.airport.set(airport.value);
  }
}