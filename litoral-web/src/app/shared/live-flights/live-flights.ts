import { AfterViewInit, Component, ElementRef, OnDestroy, effect, inject, isDevMode, signal, viewChild } from '@angular/core';
import * as L from 'leaflet';
import { APP_CONFIG } from '../../core/config/app-config';
import { LiveFlight, LiveFlightsService } from '../../core/live/live-flights.service';
import { NETWORK_LABELS } from '../../core/pirep/pirep.models';
import { ThemeService } from '../../core/theme/theme.service';
import { demoTraffic } from './demo-traffic';

const REFRESH_MS = 30_000;
const DEMO_REFRESH_MS = 3_000;
const DEFAULT_CENTER: L.LatLngTuple = [-15, -50];
const DEFAULT_ZOOM = 4;

const TILE_URL = {
  light: `https://basemaps.cartocdn.com/rastertiles/light_all/{z}/{x}/{y}.png?key=${APP_CONFIG.cartoApiKey}`,
  dark: `https://basemaps.cartocdn.com/rastertiles/dark_all/{z}/{x}/{y}.png?key=${APP_CONFIG.cartoApiKey}`
};

const ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> &copy; <a href="https://carto.com/attributions">CARTO</a>';

const PLANE_SVG = `<svg viewBox="0 0 24 24" width="26" height="26" aria-hidden="true">
  <path fill="#E09B1A" stroke="#0A1F3C" stroke-width="1" stroke-linejoin="round"
    d="M12 2c.8 0 1.4.9 1.4 2v5.2l7.6 4.4v2l-7.6-2.3v4.5l2.2 1.7V21L12 20l-3.6 1v-1.5l2.2-1.7v-4.5L3 15.6v-2l7.6-4.4V4c0-1.1.6-2 1.4-2z"/>
</svg>`;

function altitudeLabel(ft: number): string {
  return ft >= 10000 ? `FL${Math.round(ft / 100).toString().padStart(3, '0')}` : `${ft.toLocaleString('en-US')} ft`;
}

@Component({
  selector: 'app-live-flights',
  template: `
    <div class="panel overflow-hidden">
      <div class="flex items-center justify-between border-b border-line px-6 py-4">
        <h2 class="flex items-center gap-2 text-lg font-bold text-ink">
          <span class="relative flex h-2.5 w-2.5">
            @if (flights().length > 0) {
              <span class="absolute inline-flex h-full w-full animate-ping rounded-full bg-accent opacity-75"></span>
            }
            <span class="relative inline-flex h-2.5 w-2.5 rounded-full bg-accent"></span>
          </span>
          Live flights
        </h2>
        <div class="flex items-center gap-3">
          @if (devMode) {
            <button type="button" class="rounded-full border px-3 py-1 text-xs font-semibold"
              [class]="demo() ? 'border-primary bg-primary text-on-primary' : 'border-line text-ink-muted hover:border-primary hover:text-primary'"
              (click)="toggleDemo()">
              <i class="ti ti-flask mr-1" aria-hidden="true"></i>Demo traffic
            </button>
          }
          <span class="text-sm text-ink-muted">Pilots flying: {{ flights().length }}</span>
        </div>
      </div>

      <div class="relative h-[420px]">
        <div #container class="h-full w-full"></div>
        @if (flights().length === 0) {
          <div class="pointer-events-none absolute inset-x-0 bottom-6 z-[500] flex justify-center">
            <span class="flex items-center gap-2 rounded-full bg-surface/90 px-4 py-2 text-sm text-ink-muted shadow">
              <i class="ti ti-radar-2" aria-hidden="true"></i>No Litoral pilots flying right now.
            </span>
          </div>
        }
      </div>

      @if (flights().length > 0) {
        <div class="overflow-x-auto border-t border-line">
          <table class="w-full text-left text-sm">
            <thead class="text-xs uppercase tracking-wide text-ink-muted">
              <tr>
                <th class="px-6 py-3 font-semibold">Flight</th>
                <th class="px-6 py-3 font-semibold">Pilot</th>
                <th class="px-6 py-3 font-semibold">Route</th>
                <th class="px-6 py-3 font-semibold">Aircraft</th>
                <th class="px-6 py-3 font-semibold">Altitude</th>
                <th class="px-6 py-3 font-semibold">GS</th>
                <th class="px-6 py-3 font-semibold">Phase</th>
              </tr>
            </thead>
            <tbody>
              @for (f of flights(); track f.id) {
                <tr class="cursor-pointer border-t border-line hover:bg-surface-alt/50" (click)="focus(f)">
                  <td class="px-6 py-3">
                    <p class="font-mono font-bold text-ink">{{ f.callsign }}</p>
                    <p class="text-xs text-ink-muted">{{ networks[f.network] }}</p>
                  </td>
                  <td class="px-6 py-3 text-ink">{{ f.pilotName }} <span class="text-ink-muted">{{ f.pilotId }}</span></td>
                  <td class="px-6 py-3 font-mono text-ink">{{ f.departureIcao }} → {{ f.arrivalIcao }}</td>
                  <td class="px-6 py-3 text-ink">{{ f.registration }} <span class="text-ink-muted">{{ f.aircraftTypeIcao }}</span></td>
                  <td class="px-6 py-3 text-ink">{{ altitude(f.altitudeFt) }}</td>
                  <td class="px-6 py-3 text-ink">{{ f.groundSpeedKt }} kt</td>
                  <td class="px-6 py-3 text-ink-muted">{{ f.phase ?? '—' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `
})
export class LiveFlights implements AfterViewInit, OnDestroy {
  private readonly api = inject(LiveFlightsService);
  private readonly theme = inject(ThemeService);
  private readonly container = viewChild.required<ElementRef<HTMLDivElement>>('container');

  protected readonly networks = NETWORK_LABELS;
  protected readonly flights = signal<LiveFlight[]>([]);
  protected readonly devMode = isDevMode();
  protected readonly demo = signal(false);
  protected readonly altitude = altitudeLabel;

  private map?: L.Map;
  private tiles?: L.TileLayer;
  private readonly markers = new Map<string, L.Marker>();
  private timer?: ReturnType<typeof setInterval>;
  private fitted = false;

  constructor() {
    effect(() => {
      const dark = this.theme.isDark();
      if (this.map) this.setTiles(dark);
    });
  }

  ngAfterViewInit(): void {
    this.map = L.map(this.container().nativeElement, { scrollWheelZoom: true }).setView(DEFAULT_CENTER, DEFAULT_ZOOM);
    this.setTiles(this.theme.isDark());

    this.refresh();
    this.schedule();
  }

  protected toggleDemo(): void {
    this.demo.update(on => !on);
    this.fitted = false;
    this.refresh();
    this.schedule();
  }

  ngOnDestroy(): void {
    clearInterval(this.timer);
    this.map?.remove();
  }

  protected focus(flight: LiveFlight): void {
    const marker = this.markers.get(flight.id);
    if (!marker || !this.map) return;
    this.map.setView(marker.getLatLng(), Math.max(this.map.getZoom(), 7));
    marker.openPopup();
  }

    private schedule(): void {
    clearInterval(this.timer);
    this.timer = setInterval(() => this.refresh(), this.demo() ? DEMO_REFRESH_MS : REFRESH_MS);
  }

  private async refresh(): Promise<void> {
    try {
      const real = await this.api.list();
      const flights = this.demo() ? [...real, ...demoTraffic(Date.now())] : real;
      this.flights.set(flights);
      this.draw(flights);
    } catch {
      return;
    }
  }

  private draw(flights: LiveFlight[]): void {
    if (!this.map) return;

    const ids = new Set(flights.map(f => f.id));
    for (const [id, marker] of this.markers) {
      if (!ids.has(id)) {
        marker.remove();
        this.markers.delete(id);
      }
    }

    for (const flight of flights) {
      const position: L.LatLngTuple = [flight.latitude, flight.longitude];
      const icon = L.divIcon({
        className: '',
        html: `<div style="transform: rotate(${flight.heading}deg); width: 26px; height: 26px;">${PLANE_SVG}</div>`,
        iconSize: [26, 26],
        iconAnchor: [13, 13],
        popupAnchor: [0, -14]
      });
      const popup = `
        <strong style="font-family: monospace">${flight.callsign}</strong><br>
        ${flight.departureIcao} → ${flight.arrivalIcao}<br>
        ${flight.pilotName} · ${flight.registration} (${flight.aircraftTypeIcao})<br>
        ${altitudeLabel(flight.altitudeFt)} · ${flight.groundSpeedKt} kt`;

      const existing = this.markers.get(flight.id);
      if (existing) {
        existing.setLatLng(position).setIcon(icon).setPopupContent(popup);
      } else {
        const marker = L.marker(position, { icon, title: flight.callsign })
          .bindTooltip(flight.callsign, { direction: 'top', offset: [0, -12] })
          .bindPopup(popup)
          .addTo(this.map);
        this.markers.set(flight.id, marker);
      }
    }

    if (!this.fitted && flights.length > 0) {
      this.fitted = true;
      const bounds = L.latLngBounds(flights.map(f => [f.latitude, f.longitude] as L.LatLngTuple));
      this.map.fitBounds(bounds, { padding: [60, 60], maxZoom: 7 });
    }
  }

  private setTiles(dark: boolean): void {
    this.tiles?.remove();
    this.tiles = L.tileLayer(dark ? TILE_URL.dark : TILE_URL.light, {
      attribution: ATTRIBUTION,
      maxZoom: 19
    }).addTo(this.map!);
  }
}