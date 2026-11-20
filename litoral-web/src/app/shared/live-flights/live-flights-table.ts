import { Component, input, output, signal } from '@angular/core';
import { LiveFlight } from '../../core/live/live-flights.service';
import { NETWORK_LABELS, NETWORK_LOGO_LIGHT_INVERT, NETWORK_LOGOS, networkTracker } from '../../core/pirep/pirep.models';
import { countryFlagUrl } from '../utils/country-flag';
import { altitudeFt, initials, minutesLabel } from './live-format';

@Component({
  selector: 'app-live-flights-table',
  template: `
    <div class="overflow-x-auto">
      <table class="w-full min-w-[1000px] text-left text-sm">
        <thead class="border-b border-line text-xs uppercase tracking-wide text-ink-muted">
          <tr>
            <th class="px-6 py-3 font-semibold">Pilot</th>
            <th class="px-4 py-3 font-semibold">Flight | Aircraft</th>
            <th class="px-4 py-3 font-semibold">Route | Progress</th>
            <th class="px-4 py-3 font-semibold">Flight data</th>
            <th class="px-6 py-3 font-semibold">Network</th>
          </tr>
        </thead>
        <tbody>
          @for (f of flights(); track f.id) {
            <tr class="cursor-pointer border-b border-line transition-colors last:border-0"
              [class]="f.id === selectedId() ? 'bg-primary/5' : 'hover:bg-surface-alt/60'" (click)="selected.emit(f.id)">
              <td class="px-6 py-4">
                <div class="flex items-center gap-3">
                  <span class="flex h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-full bg-primary/15 text-base font-bold text-primary">
                    @if (f.pilotPhotoUrl) {
                      <img [src]="f.pilotPhotoUrl" [alt]="f.pilotName" class="h-full w-full object-cover" />
                    } @else {
                      {{ initials(f.pilotName) }}
                    }
                  </span>
                  <div>
                    <p class="font-medium text-ink">{{ f.pilotName }}</p>
                    <span class="mt-1 inline-block rounded bg-sol-500/15 px-2 py-0.5 text-xs font-bold text-sol-600">{{ f.pilotId }}</span>
                  </div>
                </div>
              </td>

              <td class="px-4 py-4">
                <p class="font-mono text-base font-bold text-sol-500">{{ f.callsign }}</p>
                <p class="text-ink">{{ f.aircraftTypeIcao }} · {{ f.registration }}</p>
                <p class="text-xs text-ink-muted">{{ f.aircraftTypeName }} · {{ f.isCargo ? 'Cargo' : 'Passengers' }}</p>
              </td>

              <td class="px-4 py-4">
                <div class="flex items-center gap-4">
                  <div class="w-28 shrink-0">
                    <p class="text-[10px] font-semibold uppercase tracking-wide text-ink-muted">Departure</p>
                    <p class="flex items-center gap-1.5 font-mono text-base font-bold text-ink">
                      {{ f.departure.icao }}
                      @if (flag(f.departure.country); as url) {
                        <img [src]="url" [alt]="f.departure.country" class="h-3 w-[18px] rounded-sm object-cover" />
                      }
                    </p>
                    <p class="truncate text-xs text-ink-muted">{{ f.departure.city }}</p>
                  </div>

                  <div class="min-w-[220px] flex-1">
                    <div class="mb-2 flex justify-between text-xs">
                      <span class="font-bold text-ink">{{ f.progress }}%</span>
                      <span class="text-ink-muted">{{ f.phase ?? '—' }}</span>
                    </div>
                    <div class="relative h-1.5 rounded-full bg-line">
                      <div class="h-full rounded-full bg-onda-500" [style.width.%]="f.progress"></div>
                      <span class="absolute top-1/2 flex h-5 w-5 -translate-x-1/2 -translate-y-1/2 items-center justify-center rounded-full bg-onda-500 text-[11px] text-white shadow"
                        [style.left.%]="f.progress">
                        <svg viewBox="0 0 24 24" class="h-3.5 w-3.5 rotate-90" aria-hidden="true">
                          <path fill="currentColor" d="M12 2c.8 0 1.4.9 1.4 2v5.2l7.6 4.4v2l-7.6-2.3v4.5l2.2 1.7V21L12 20l-3.6 1v-1.5l2.2-1.7v-4.5L3 15.6v-2l7.6-4.4V4c0-1.1.6-2 1.4-2z"/>
                        </svg>
                      </span>
                    </div>
                  </div>

                  <div class="w-28 shrink-0 text-right">
                    <p class="text-[10px] font-semibold uppercase tracking-wide text-ink-muted">Arrival</p>
                    <p class="flex items-center justify-end gap-1.5 font-mono text-base font-bold text-ink">
                      {{ f.arrival.icao }}
                      @if (flag(f.arrival.country); as url) {
                        <img [src]="url" [alt]="f.arrival.country" class="h-3 w-[18px] rounded-sm object-cover" />
                      }
                    </p>
                    <p class="truncate text-xs text-ink-muted">{{ f.arrival.city }}</p>
                  </div>
                </div>
              </td>

              <td class="px-4 py-4">
                <div class="grid grid-cols-2 gap-x-5 gap-y-2 whitespace-nowrap">
                  <div>
                    <p class="text-[10px] font-semibold uppercase tracking-wide text-ink-muted">Altitude</p>
                    <p class="font-semibold text-ink">{{ altitude(f.altitudeFt) }}</p>
                  </div>
                  <div>
                    <p class="text-[10px] font-semibold uppercase tracking-wide text-ink-muted">Ground speed</p>
                    <p class="font-semibold text-ink">{{ f.groundSpeedKt }} kt</p>
                  </div>
                  <div>
                    <p class="text-[10px] font-semibold uppercase tracking-wide text-ink-muted">Remaining</p>
                    <p class="font-semibold text-ink">{{ f.remainingNm }} NM</p>
                  </div>
                  <div>
                    <p class="text-[10px] font-semibold uppercase tracking-wide text-ink-muted">Arrival in</p>
                    <p class="font-semibold text-ink">{{ minutes(f.minutesToArrival) }}</p>
                  </div>
                </div>
              </td>

              <td class="px-6 py-4">
                @if (f.network === 'Offline') {
                  <p class="font-semibold text-danger">Offline</p>
                  <p class="text-xs text-ink-muted">{{ f.simulator ?? '' }}</p>
                } @else {
                  <div class="flex items-center gap-3">
                    <span class="flex h-11 w-11 shrink-0 items-center justify-center">
                      @if (logos[f.network] && !brokenLogos().has(f.network)) {
                        <img [src]="logos[f.network]" [alt]="networks[f.network]" class="h-full w-full object-contain"
                          [class.invert]="invertOnLight[f.network]" [class.dark:invert-0]="invertOnLight[f.network]"
                          (error)="logoFailed(f.network)" />
                      } @else {
                        <i class="ti ti-broadcast text-2xl text-ink-muted" aria-hidden="true"></i>
                      }
                    </span>
                    <div>
                      @if (tracker(f); as t) {
                        <a [href]="t.url" target="_blank" rel="noopener" [title]="'Open on ' + t.name" (click)="$event.stopPropagation()"
                          class="font-mono font-bold text-onda-500 transition-colors hover:text-onda-300">
                          {{ f.callsign }}
                        </a>
                      } @else {
                        <p class="font-mono font-bold text-onda-500">{{ f.callsign }}</p>
                      }
                      <p class="text-xs text-ink-muted">{{ networks[f.network] }}{{ f.simulator ? ' · ' + f.simulator : '' }}</p>
                    </div>
                  </div>
                }
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `
})
export class LiveFlightsTable {
  readonly flights = input.required<LiveFlight[]>();
  readonly selectedId = input<string | null>(null);
  readonly selected = output<string>();

  protected readonly networks = NETWORK_LABELS;
  protected readonly logos = NETWORK_LOGOS;
  protected readonly invertOnLight = NETWORK_LOGO_LIGHT_INVERT;
  protected readonly brokenLogos = signal(new Set<string>());
  protected readonly flag = countryFlagUrl;
  protected readonly initials = initials;
  protected readonly altitude = altitudeFt;
  protected readonly minutes = minutesLabel;
  protected tracker(flight: LiveFlight): { name: string; url: string } | null {
    return networkTracker(flight.network, flight.callsign);
  }
  protected logoFailed(network: string): void {
    this.brokenLogos.update(set => new Set(set).add(network));
  }
}