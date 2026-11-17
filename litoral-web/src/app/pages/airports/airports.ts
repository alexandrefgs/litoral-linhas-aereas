import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { apiErrorMessage } from '../../core/api/api-error';
import {
  AIRCRAFT_STATUS_CLASS,
  AirportAircraft,
  AirportDeparture,
  AirportDirectory,
  AirportFilter,
  AirportPilot,
  DirectoryAirport
} from '../../core/airports/airports.models';
import { AirportsService } from '../../core/airports/airports.service';
import { AirportMap } from '../../shared/airport-map/airport-map';
import { countryFlagUrl } from '../../shared/utils/country-flag';
import { AirportModal } from './airport-modal';

type Dialog =
  | { kind: 'aircraft'; airport: DirectoryAirport }
  | { kind: 'flights'; airport: DirectoryAirport }
  | { kind: 'jumpseat'; airport: DirectoryAirport }
  | { kind: 'location'; airport: DirectoryAirport };

const FILTERS: { key: AirportFilter; label: string }[] = [
  { key: 'all', label: 'All' },
  { key: 'bases', label: 'Bases' },
  { key: 'passenger', label: 'Passenger' },
  { key: 'cargo', label: 'Cargo' }
];

@Component({
  selector: 'app-airports',
  imports: [DatePipe, FormsModule, AirportMap, AirportModal],
  template: `
    <section class="mx-auto flex max-w-7xl flex-col gap-6 px-6 py-8">
      <div class="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <h1 class="text-2xl font-bold text-ink">Airports</h1>
          <p class="text-ink-muted">Click an airport to jumpseat there. It's free to our bases and costs 3 points per nm elsewhere. One jumpseat per flight.</p>
        </div>
        @if (directory(); as d) {
          <div class="flex gap-6 text-sm">
            <div>
              <p class="text-xs uppercase tracking-wide text-ink-muted">You are at</p>
              <p class="font-mono text-lg font-bold text-accent">{{ d.currentIcao }}</p>
            </div>
            <div>
              <p class="text-xs uppercase tracking-wide text-ink-muted">Points</p>
              <p class="text-lg font-bold" [class]="d.points < 0 ? 'text-danger' : 'text-ink'">{{ d.points.toLocaleString('en-US') }}</p>
            </div>
          </div>
        }
      </div>

      <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div class="flex flex-wrap gap-1.5">
          @for (f of filters; track f.key) {
            <button type="button" (click)="filter.set(f.key)"
              class="rounded-full px-3.5 py-1.5 text-sm font-semibold transition-colors"
              [class]="filter() === f.key ? 'bg-primary text-on-primary' : 'border border-line bg-surface text-ink-muted hover:text-ink'">
              {{ f.label }}
            </button>
          }
        </div>
        <div class="relative sm:w-72">
          <i class="ti ti-search pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-ink-muted" aria-hidden="true"></i>
          <input type="search" class="field-input pl-9" placeholder="ICAO, IATA or city" [ngModel]="search()" (ngModelChange)="search.set($event)" />
        </div>
      </div>

      @if (message(); as m) {
        <p class="rounded-md px-4 py-3 text-sm" [class]="m.ok ? 'bg-accent/15 text-ink' : 'bg-danger/10 text-danger'">{{ m.text }}</p>
      }

      @if (loading() && !directory()) {
        <p class="py-24 text-center text-ink-muted">Loading...</p>
      } @else if (loadError()) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-16 text-center">
          <p class="text-danger">Something went wrong. Try again.</p>
          <button type="button" class="btn-secondary" (click)="load()">Try again</button>
        </div>
      } @else if (directory(); as d) {
        <div class="grid gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          @for (a of filtered(); track a.icao) {
            @let current = a.icao === d.currentIcao;
            <article class="group flex flex-col overflow-hidden rounded-xl border bg-surface transition"
              [class]="current ? 'border-line opacity-60' : 'cursor-pointer border-line hover:-translate-y-0.5 hover:border-primary/60 hover:shadow-lg'"
              (click)="openJumpseat(a)">
              <div class="relative h-36 bg-cover bg-center" [style.background-image]="cover(a)">
                @if (!a.photoUrl) {
                  <i class="ti ti-building-airport absolute inset-0 m-auto h-fit w-fit text-5xl text-white/40" aria-hidden="true"></i>
                }
                @if (a.photoCredit) {
                  <span class="absolute bottom-1 left-2 max-w-[60%] truncate text-[10px] text-white/80 [text-shadow:0_1px_2px_rgba(0,0,0,.8)]">© {{ a.photoCredit }}</span>
                }
                @if (current) {
                  <span class="absolute bottom-2 right-2 rounded-md bg-accent px-2.5 py-1 text-xs font-bold text-on-accent shadow">You are here</span>
                }
              </div>

              <div class="flex flex-1 flex-col gap-3 p-4">
                <div class="flex items-start justify-between gap-2">
                  <div class="min-w-0">
                    <p class="font-mono text-3xl font-bold leading-none text-ink">{{ a.icao }}</p>
                    <p class="mt-1 truncate text-sm text-ink-muted">
                      @if (a.iata) { <span class="font-mono font-bold text-ink">{{ a.iata }}</span> | }{{ a.city }}
                    </p>
                  </div>
                  <div class="flex flex-col items-end gap-1.5">
                    <span class="flex items-center gap-1.5">
                      @if (a.isBase) { <span class="rounded bg-primary/15 px-1.5 py-0.5 text-[10px] font-bold uppercase text-primary">Base</span> }
                      @if (flag(a.country); as url) { <img [src]="url" alt="" class="h-3.5 w-auto rounded-sm" /> }
                    </span>
                    <span class="flex text-sol-500" [title]="'Minimum rank: ' + a.minimumRankName">
                      @for (c of chevrons(a.minimumRankLevel); track $index) {<img [src]="'images/ranks/' + a.minimumRankCode.toLowerCase() + '.png'" [alt]="a.minimumRankName"
                      [title]="'Minimum rank: ' + a.minimumRankName" class="h-6 w-auto object-contain" /> }
                    </span>
                  </div>
                </div>

                <div class="grid grid-cols-2 gap-2">
                  <button type="button" class="flex items-center gap-2 rounded-lg p-1.5 text-left transition-colors hover:bg-surface-alt"
                    (click)="$event.stopPropagation(); openAircraft(a)">
                    <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-surface-alt text-lg text-sol-500"><i class="ti ti-plane" aria-hidden="true"></i></span>
                    <span>
                      <span class="block text-xs text-ink-muted">Aircraft</span>
                      <span class="block text-sm font-semibold text-ink">{{ a.aircraftOnGround }}</span>
                    </span>
                  </button>
                  <button type="button" class="flex items-center gap-2 rounded-lg p-1.5 text-left transition-colors hover:bg-surface-alt"
                    (click)="$event.stopPropagation(); openFlights(a)">
                    <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-surface-alt text-lg text-sol-500"><i class="ti ti-plane-departure" aria-hidden="true"></i></span>
                    <span>
                      <span class="block text-xs text-ink-muted">Flights</span>
                      <span class="block text-sm font-semibold text-ink">{{ a.departures }}</span>
                    </span>
                  </button>
                </div>

                <p class="flex items-center gap-1.5 truncate text-[11px] uppercase tracking-wide text-ink-muted">
                  <i class="ti ti-map-pin" aria-hidden="true"></i> <span class="truncate">{{ a.name }}</span>
                </p>

                <div class="mt-auto flex items-center justify-between gap-2 border-t border-line pt-3">
                  <div class="flex items-center gap-2">
                    <i class="ti ti-users text-ink-muted" aria-hidden="true"></i>
                    @if (a.pilotCount) {
                      <div class="flex -space-x-2">
                        @for (p of a.pilots; track p.pilotNumber) {
                          <button type="button" class="h-7 w-7 overflow-hidden rounded-full ring-2 ring-surface" [title]="p.displayId + ' - ' + p.name"
                            (click)="$event.stopPropagation(); openProfile(p)">
                            @if (p.photoUrl) {
                              <img [src]="p.photoUrl" alt="" class="h-full w-full object-cover" />
                            } @else {
                              <span class="flex h-full w-full items-center justify-center bg-primary text-[10px] font-bold text-on-primary">{{ initials(p) }}</span>
                            }
                          </button>
                        }
                        @if (a.pilotCount > a.pilots.length) {
                          <span class="flex h-7 w-7 items-center justify-center rounded-full bg-surface-alt text-[10px] font-bold text-ink ring-2 ring-surface">+{{ a.pilotCount - a.pilots.length }}</span>
                        }
                      </div>
                    } @else {
                      <span class="rounded bg-danger/10 px-1.5 py-0.5 text-[10px] font-semibold text-danger">No pilots</span>
                    }
                  </div>

                  @if (current) {
                    <button type="button" class="flex items-center gap-1 text-sm text-ink-muted hover:text-ink" (click)="$event.stopPropagation(); dialog.set({ kind: 'location', airport: a })">
                      <i class="ti ti-map-2" aria-hidden="true"></i> Location
                    </button>
                  } @else {
                    <span class="flex items-center gap-1 text-sm font-semibold" [class]="a.jumpseatCost ? 'text-sol-500' : 'text-onda-500'">
                      <i class="ti ti-coin" aria-hidden="true"></i> {{ a.jumpseatCost ? a.jumpseatCost.toLocaleString('en-US') + ' pts' : 'Free' }}
                    </span>
                  }
                </div>
              </div>
            </article>
          } @empty {
            <p class="col-span-full py-16 text-center text-ink-muted">No airports match your search.</p>
          }
        </div>
      }
    </section>

    @if (dialog(); as dlg) {
      @switch (dlg.kind) {
        @case ('aircraft') {
          <app-airport-modal [title]="'Aircraft on the ground at ' + dlg.airport.icao" (closed)="close()">
            @if (listLoading()) {
              <p class="px-6 py-12 text-center text-ink-muted">Loading...</p>
            } @else if (aircraft().length === 0) {
              <p class="px-6 py-12 text-center text-ink-muted">No aircraft on the ground at {{ dlg.airport.icao }}.</p>
            } @else {
              <table class="w-full text-left text-sm">
                <thead class="border-b border-line text-xs uppercase tracking-wide text-ink-muted">
                  <tr>
                    <th class="px-6 py-3 font-semibold">Aircraft</th>
                    <th class="px-6 py-3 font-semibold">Last flight</th>
                    <th class="px-6 py-3 font-semibold">Status</th>
                  </tr>
                </thead>
                <tbody>
                  @for (x of aircraft(); track x.registration) {
                    <tr class="border-b border-line last:border-0">
                      <td class="px-6 py-3">
                        <div class="flex items-center gap-4">
                          <div class="flex h-14 w-24 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-surface-alt">
                            @if (x.photoUrl) {
                              <img [src]="x.photoUrl" alt="" class="h-full w-full object-cover" />
                            } @else {
                              <i class="ti ti-plane text-2xl text-ink-muted" aria-hidden="true"></i>
                            }
                          </div>
                          <div>
                            <p class="font-mono text-lg font-bold text-ink">{{ x.registration }}</p>
                            <p class="text-xs text-ink-muted">{{ x.typeName }}</p>
                          </div>
                        </div>
                      </td>
                      <td class="px-6 py-3">
                        @if (x.lastCallsign || x.lastFlightAt) {
                          <p class="font-mono font-bold text-ink">{{ x.lastCallsign ?? '—' }}</p>
                          <p class="text-xs text-ink-muted">{{ x.lastFlightAt | date: 'd MMM yyyy' }}</p>
                        } @else {
                          <span class="text-ink-muted">No flights yet</span>
                        }
                      </td>
                      <td class="px-6 py-3">
                        <span class="rounded-md px-2.5 py-1 text-xs font-semibold" [class]="statusClass[x.status] ?? 'bg-surface-alt text-ink'">{{ x.status }}</span>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            }
          </app-airport-modal>
        }

        @case ('flights') {
          <app-airport-modal [title]="'Flights departing from ' + dlg.airport.icao" (closed)="close()">
            @if (listLoading()) {
              <p class="px-6 py-12 text-center text-ink-muted">Loading...</p>
            } @else if (departures().length === 0) {
              <p class="px-6 py-12 text-center text-ink-muted">No scheduled departures from {{ dlg.airport.icao }}.</p>
            } @else {
              <table class="w-full text-left text-sm">
                <thead class="border-b border-line text-xs uppercase tracking-wide text-ink-muted">
                  <tr>
                    <th class="px-6 py-3 font-semibold">Flight</th>
                    <th class="px-6 py-3 font-semibold">Destination</th>
                    <th class="px-6 py-3 font-semibold">Distance</th>
                    <th class="px-6 py-3 font-semibold">Time</th>
                    <th class="px-6 py-3 font-semibold">Status</th>
                  </tr>
                </thead>
                <tbody>
                  @for (r of departures(); track r.routeId) {
                    <tr class="border-b border-line last:border-0">
                      <td class="px-6 py-3">
                        <p class="font-mono font-bold text-primary">{{ r.callsign }}</p>
                        <p class="text-xs text-ink-muted">{{ r.isCargo ? 'Cargo' : 'Passengers' }} · {{ r.aircraftTypes.join(', ') }}</p>
                      </td>
                      <td class="px-6 py-3">
                        <p class="flex items-center gap-1.5 font-mono font-bold text-ink">
                          @if (flag(r.arrivalCountry); as url) { <img [src]="url" alt="" class="h-3 w-auto rounded-sm" /> }
                          {{ r.arrivalIcao }}
                        </p>
                        <p class="text-xs text-ink-muted">
                          @if (r.arrivalIata) { <span class="font-semibold">{{ r.arrivalIata }}</span> - }{{ r.arrivalCity }}
                        </p>
                      </td>
                      <td class="px-6 py-3 text-ink">{{ r.distanceNm.toLocaleString('en-US') }} nm</td>
                      <td class="px-6 py-3 font-mono text-ink">{{ hhmm(r.estimatedMinutes) }}</td>
                      <td class="px-6 py-3">
                        <span class="rounded-md px-2.5 py-1 text-xs font-semibold" [class]="r.hasAircraft ? 'bg-onda-500/15 text-onda-500' : 'bg-surface-alt text-ink-muted'">
                          {{ r.hasAircraft ? 'Available' : 'No aircraft' }}
                        </span>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            }
          </app-airport-modal>
        }

        @case ('jumpseat') {
          <app-airport-modal width="max-w-md" (closed)="close()">
            <div class="flex flex-col items-center gap-4 px-6 pb-6 pt-8 text-center">
              <span class="flex h-16 w-16 items-center justify-center rounded-full border-2 border-primary text-3xl text-primary">
                <i class="ti ti-armchair" aria-hidden="true"></i>
              </span>
              <h2 class="text-2xl text-ink">Jumpseat to <strong>{{ dlg.airport.city }}</strong>?</h2>
              <p class="text-sm text-ink-muted">
                You will move from <strong class="font-mono text-ink">{{ directory()?.currentIcao }}</strong>
                to <strong class="font-mono text-ink">{{ dlg.airport.icao }}</strong>. This can't be undone.
              </p>

              @if (directory()?.jumpseatBlockedReason; as reason) {
                <p class="rounded-md bg-danger/10 px-4 py-2 text-sm text-danger">{{ reason }}</p>
                <button type="button" class="btn-secondary" (click)="close()">Close</button>
              } @else {
                @if (jumpseatError(); as error) {
                  <p class="rounded-md bg-danger/10 px-4 py-2 text-sm text-danger">{{ error }}</p>
                }
                <div class="flex gap-3">
                  <button type="button" class="btn-secondary px-5 py-2.5" [disabled]="busy()" (click)="close()">No, cancel</button>
                  <button type="button" class="btn-primary px-5 py-2.5" [disabled]="busy()" (click)="jumpseat(dlg.airport)">
                    {{ busy() ? 'Moving...' : 'Yes, proceed' }}
                  </button>
                </div>
              }
            </div>
            <div class="border-t border-line px-6 py-4 text-center">
              @if (dlg.airport.jumpseatCost) {
                <p class="text-2xl font-bold text-sol-500">{{ dlg.airport.jumpseatCost.toLocaleString('en-US') }} points</p>
                <p class="text-xs text-ink-muted">will be deducted from your balance ({{ directory()?.points?.toLocaleString('en-US') }} now)</p>
              } @else {
                <p class="text-2xl font-bold text-onda-500">Free</p>
                <p class="text-xs text-ink-muted">Jumpseat to our bases costs nothing</p>
              }
            </div>
          </app-airport-modal>
        }

        @case ('location') {
          <app-airport-modal [title]="dlg.airport.icao + ' · ' + dlg.airport.name" width="max-w-4xl" (closed)="close()">
            <app-airport-map class="h-[28rem]" [latitude]="dlg.airport.latitude" [longitude]="dlg.airport.longitude" [label]="dlg.airport.icao" />
          </app-airport-modal>
        }
      }
    }
  `
})
export class Airports implements OnInit {
  private readonly api = inject(AirportsService);
  private readonly router = inject(Router);

  protected readonly filters = FILTERS;
  protected readonly statusClass = AIRCRAFT_STATUS_CLASS;

  protected readonly directory = signal<AirportDirectory | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly search = signal('');
  protected readonly filter = signal<AirportFilter>('all');
  protected readonly dialog = signal<Dialog | null>(null);
  protected readonly aircraft = signal<AirportAircraft[]>([]);
  protected readonly departures = signal<AirportDeparture[]>([]);
  protected readonly listLoading = signal(false);
  protected readonly busy = signal(false);
  protected readonly jumpseatError = signal<string | null>(null);
  protected readonly message = signal<{ ok: boolean; text: string } | null>(null);

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const filter = this.filter();

    return (this.directory()?.airports ?? []).filter(a => {
      if (filter === 'bases' && !a.isBase) return false;
      if (filter === 'passenger' && !a.passenger) return false;
      if (filter === 'cargo' && !a.cargo) return false;
      if (!term) return true;
      return a.icao.toLowerCase().includes(term)
        || (a.iata ?? '').toLowerCase().includes(term)
        || a.city.toLowerCase().includes(term)
        || a.name.toLowerCase().includes(term);
    });
  });

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);
    try {
      this.directory.set(await this.api.list());
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected openJumpseat(airport: DirectoryAirport): void {
    if (airport.icao === this.directory()?.currentIcao) return;
    this.jumpseatError.set(null);
    this.dialog.set({ kind: 'jumpseat', airport });
  }

  protected async openAircraft(airport: DirectoryAirport): Promise<void> {
    this.dialog.set({ kind: 'aircraft', airport });
    await this.loadList(async () => this.aircraft.set(await this.api.aircraft(airport.icao)));
  }

  protected async openFlights(airport: DirectoryAirport): Promise<void> {
    this.dialog.set({ kind: 'flights', airport });
    await this.loadList(async () => this.departures.set(await this.api.departures(airport.icao)));
  }

  protected openProfile(pilot: AirportPilot): void {
    this.router.navigate(['/profile', pilot.pilotNumber]);
  }

  protected close(): void {
    if (!this.busy()) this.dialog.set(null);
  }

  protected async jumpseat(airport: DirectoryAirport): Promise<void> {
    this.busy.set(true);
    this.jumpseatError.set(null);
    try {
      const result = await this.api.jumpseat(airport.icao);
      this.busy.set(false);
      this.dialog.set(null);
      this.message.set({ ok: true, text: `You are now at ${result.toIcao}.` + (result.cost ? ` ${result.cost} points were deducted.` : '') });
      await this.load();
    } catch (error) {
      this.jumpseatError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.busy.set(false);
    }
  }

  protected cover(airport: DirectoryAirport): string {
    const gradient = 'linear-gradient(135deg, #0F3768 0%, #1D6FD8 60%, #2EC4B6 100%)';
    return airport.photoUrl ? `url('${airport.photoUrl}'), ${gradient}` : gradient;
  }

  protected chevrons(level: number): number[] {
    return Array.from({ length: Math.max(1, level) });
  }

  protected initials(pilot: AirportPilot): string {
    return `${pilot.firstName[0] ?? ''}${pilot.lastName[0] ?? ''}`.toUpperCase();
  }

  protected flag(country: string): string | null {
    return countryFlagUrl(country);
  }

  protected hhmm(minutes: number): string {
    return `${Math.floor(minutes / 60).toString().padStart(2, '0')}:${(minutes % 60).toString().padStart(2, '0')}`;
  }

  private async loadList(action: () => Promise<void>): Promise<void> {
    this.listLoading.set(true);
    try {
      await action();
    } catch {
      this.aircraft.set([]);
      this.departures.set([]);
    } finally {
      this.listLoading.set(false);
    }
  }
}