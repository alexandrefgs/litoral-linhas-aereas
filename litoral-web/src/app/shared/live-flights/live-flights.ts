import { AfterViewInit, Component, ElementRef, OnDestroy, computed, effect, inject, isDevMode, signal, viewChild } from '@angular/core';
import * as L from 'leaflet';
import { APP_CONFIG } from '../../core/config/app-config';
import { LiveFlight, LiveFlightsService } from '../../core/live/live-flights.service';
import { NETWORK_LABELS, NETWORK_LOGOS, TrackPoint, networkTracker } from '../../core/pirep/pirep.models';
import { ThemeService } from '../../core/theme/theme.service';
import { demoTrack, demoTraffic } from './demo-traffic';
import { LiveFlightsTable } from './live-flights-table';
import { altitudeFt, flightLevel, minutesLabel, unwrapLongitudes } from './live-format';

const REFRESH_MS = 30_000;
const DEMO_REFRESH_MS = 3_000;
const DEFAULT_CENTER: L.LatLngTuple = [-15, -50];
const DEFAULT_ZOOM = 4;
const PANEL_PADDING: L.PointExpression = [380, 40];

const TILE_URL = {
  light: `https://basemaps.cartocdn.com/rastertiles/light_all/{z}/{x}/{y}.png?key=${APP_CONFIG.cartoApiKey}`,
  dark: `https://basemaps.cartocdn.com/rastertiles/dark_all/{z}/{x}/{y}.png?key=${APP_CONFIG.cartoApiKey}`
};

const ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> &copy; <a href="https://carto.com/attributions">CARTO</a>';

const PLANE_PATH = 'M12 2c.8 0 1.4.9 1.4 2v5.2l7.6 4.4v2l-7.6-2.3v4.5l2.2 1.7V21L12 20l-3.6 1v-1.5l2.2-1.7v-4.5L3 15.6v-2l7.6-4.4V4c0-1.1.6-2 1.4-2z';

function planeIcon(flight: LiveFlight, selected: boolean): L.DivIcon {
  const fill = selected ? '#FFFFFF' : '#E09B1A';
  return L.divIcon({
    className: 'live-plane',
    html: `
      <div class="live-plane__wrap">
        <div class="live-plane__icon" style="transform: rotate(${flight.heading}deg)">
          <svg viewBox="0 0 24 24" width="28" height="28" aria-hidden="true">
            <path fill="${fill}" stroke="#0A1F3C" stroke-width="1" stroke-linejoin="round" d="${PLANE_PATH}"/>
          </svg>
        </div>
        <div class="live-plane__label${selected ? ' live-plane__label--selected' : ''}">
          <strong>${flight.callsign}</strong>
          <span>${flight.departure.icao} <i class="ti ti-plane"></i> ${flight.arrival.icao}</span>
        </div>
      </div>`,
    iconSize: [28, 28],
    iconAnchor: [14, 14]
  });
}

function airportIcon(kind: 'DEP' | 'ARR'): L.DivIcon {
  return L.divIcon({
    className: 'live-airport',
    html: `<div class="live-airport__pin live-airport__pin--${kind.toLowerCase()}">
        <i class="ti ${kind === 'DEP' ? 'ti-plane-departure' : 'ti-plane-arrival'}"></i><span>${kind}</span>
      </div>`,
    iconSize: [58, 22],
    iconAnchor: [29, 11]
  });
}

@Component({
  selector: 'app-live-flights',
  imports: [LiveFlightsTable],
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

      <div class="relative h-[560px]">
        <div #container class="h-full w-full"></div>

        @if (selected(); as f) {
          <aside class="absolute bottom-4 left-4 top-4 z-[600] flex w-80 flex-col overflow-hidden rounded-xl border border-white/10 bg-atlantico-900/95 text-white shadow-2xl backdrop-blur">
            <div class="flex items-start justify-between px-5 pb-3 pt-4">
              <div>
                <p class="font-mono text-3xl font-extrabold tracking-tight">{{ f.callsign }}</p>
                <p class="mt-1 flex items-center gap-2 text-sm">
                  <span class="rounded bg-sol-500/20 px-2 py-0.5 text-xs font-bold text-sol-300">{{ f.pilotId }}</span>
                  <span class="font-medium">{{ f.pilotName }}</span>
                </p>
              </div>
              <button type="button" class="flex h-8 w-8 items-center justify-center rounded-full border border-sol-400 text-sol-400 hover:bg-sol-400 hover:text-atlantico-900"
                aria-label="Close flight details" (click)="select(null)">
                <i class="ti ti-x" aria-hidden="true"></i>
              </button>
            </div>

            <div class="relative h-36 shrink-0 bg-gradient-to-br from-atlantico-700 to-atlantico-900">
              @if (f.photoUrl) {
                <img [src]="f.photoUrl" [alt]="f.registration" class="h-full w-full object-cover" />
                @if (f.photoCredit) {
                  <span class="absolute bottom-1 right-2 text-[10px] text-white/80">© {{ f.photoCredit }}</span>
                }
              } @else {
                <div class="flex h-full items-center justify-center text-6xl text-white/20">
                  <i class="ti ti-plane" aria-hidden="true"></i>
                </div>
              }
              <div class="absolute inset-x-0 bottom-0 h-12 bg-gradient-to-t from-atlantico-900/95 to-transparent"></div>
            </div>

            <div class="flex flex-1 flex-col gap-4 overflow-y-auto px-5 pb-5">
              <div class="flex items-center justify-between text-center">
                <div>
                  <p class="font-mono text-3xl font-extrabold">{{ f.departure.icao }}</p>
                  <p class="text-xs text-white/70">{{ f.departure.city }}</p>
                </div>
                <i class="ti ti-plane text-2xl text-sol-400" aria-hidden="true"></i>
                <div>
                  <p class="font-mono text-3xl font-extrabold">{{ f.arrival.icao }}</p>
                  <p class="text-xs text-white/70">{{ f.arrival.city }}</p>
                </div>
              </div>

              <div class="grid grid-cols-2 gap-3 text-center text-sm">
                <div>
                  <p class="text-xs text-white/60">Flight type</p>
                  <p class="font-semibold">{{ f.isCargo ? 'Cargo' : 'Passengers' }}</p>
                </div>
                <div>
                  <p class="text-xs text-white/60">Aircraft</p>
                  <p class="font-semibold">{{ f.aircraftTypeIcao }} | {{ f.registration }}</p>
                </div>
              </div>

              <div>
                <div class="h-4 overflow-hidden rounded-full bg-white/10">
                  <div class="progress-stripes flex h-full items-center justify-center rounded-full bg-onda-500 text-[10px] font-bold"
                    [style.width.%]="Math.max(f.progress, 12)">{{ f.progress }}%</div>
                </div>
                <p class="mt-1.5 text-center text-sm font-semibold">{{ f.phase ?? '—' }}</p>
              </div>

              <div class="grid grid-cols-2 gap-y-4 border-y border-white/10 py-4 text-center">
                <div>
                  <p class="text-xl font-bold">{{ f.groundSpeedKt }}</p>
                  <p class="text-xs text-white/60">Ground speed (kt)</p>
                </div>
                <div>
                  <p class="text-xl font-bold">{{ level(f.altitudeFt) }}</p>
                  <p class="text-xs text-white/60">Altitude</p>
                </div>
                <div>
                  <p class="text-xl font-bold">{{ f.remainingNm }}</p>
                  <p class="text-xs text-white/60">Remaining (NM)</p>
                </div>
                <div>
                  <p class="text-xl font-bold">{{ minutes(f.minutesToArrival) }}</p>
                  <p class="text-xs text-white/60">Arrival in</p>
                </div>
              </div>

              <div class="text-center text-sm">
                <p class="text-xs text-white/60">Network</p>
                @if (logos[f.network] && !brokenLogos().has(f.network)) {
                  <img [src]="logos[f.network]" [alt]="networks[f.network]" class="mx-auto mt-1 h-12 w-12 object-contain"
                    (error)="logoFailed(f.network)" />
                } @else {
                  <p class="font-semibold" [class.text-danger]="f.network === 'Offline'">{{ networks[f.network] }}</p>
                }
                @if (f.simulator) {
                  <p class="text-xs text-white/60">{{ f.simulator }}</p>
                }
                @if (tracker(f); as t) {
                  <a [href]="t.url" target="_blank" rel="noopener"
                    class="mt-2 inline-flex items-center gap-1.5 rounded-full border border-onda-400 px-3 py-1 text-xs font-semibold text-onda-300 hover:bg-onda-400 hover:text-atlantico-900">
                    Track on {{ t.name }}<i class="ti ti-external-link" aria-hidden="true"></i>
                  </a>
                }
              </div>

              <div class="mt-auto flex justify-center gap-4 text-[11px] text-white/60">
                <span class="flex items-center gap-1.5"><span class="h-0.5 w-5 border-t-2 border-dashed border-atlantico-300"></span>Planned</span>
                <span class="flex items-center gap-1.5"><span class="h-0.5 w-5 bg-sol-500"></span>Flown</span>
              </div>
            </div>
          </aside>
        }

        @if (flights().length === 0) {
          <div class="pointer-events-none absolute inset-x-0 bottom-6 z-[500] flex justify-center">
            <span class="flex items-center gap-2 rounded-full bg-surface/90 px-4 py-2 text-sm text-ink-muted shadow">
              <i class="ti ti-radar-2" aria-hidden="true"></i>No Litoral pilots flying right now.
            </span>
          </div>
        }
      </div>

      @if (flights().length > 0) {
        <div class="border-t border-line">
          <app-live-flights-table [flights]="flights()" [selectedId]="selectedId()" (selected)="select($event, true)" />
        </div>
      }
    </div>
  `,
  styles: `
    :host ::ng-deep .live-plane { background: none; border: none; }
    :host ::ng-deep .live-plane__wrap { position: relative; width: 28px; height: 28px; cursor: pointer; }
    :host ::ng-deep .live-plane__icon { width: 28px; height: 28px; filter: drop-shadow(0 1px 2px rgb(0 0 0 / 0.5)); }
    :host ::ng-deep .live-plane__label {
      position: absolute; left: 34px; top: -6px; white-space: nowrap; padding: 3px 7px;
      background: rgb(10 31 60 / 0.92); border: 1px solid rgb(255 255 255 / 0.35); border-radius: 3px;
      color: #fff; line-height: 1.15; pointer-events: none;
    }
    :host ::ng-deep .live-plane__label strong { display: block; font-size: 13px; font-weight: 800; }
    :host ::ng-deep .live-plane__label span { display: flex; align-items: center; gap: 3px; font-size: 10px; font-weight: 600; opacity: 0.85; }
    :host ::ng-deep .live-plane__label--selected { background: #E09B1A; border-color: #FFFFFF; color: #0A1F3C; }
    :host ::ng-deep .live-airport { background: none; border: none; }
    :host ::ng-deep .live-airport__pin {
      display: inline-flex; align-items: center; gap: 3px; padding: 2px 6px; border-radius: 4px;
      font-size: 10px; font-weight: 800; letter-spacing: 0.04em; box-shadow: 0 1px 4px rgb(0 0 0 / 0.4);
    }
    :host ::ng-deep .live-airport__pin--dep { background: #2EC4B6; color: #063B36; }
    :host ::ng-deep .live-airport__pin--arr { background: #FFD166; color: #4A300C; }
    .progress-stripes {
      background-image: linear-gradient(45deg, rgb(255 255 255 / 0.18) 25%, transparent 25%, transparent 50%,
        rgb(255 255 255 / 0.18) 50%, rgb(255 255 255 / 0.18) 75%, transparent 75%, transparent);
      background-size: 14px 14px;
    }
  `
})
export class LiveFlights implements AfterViewInit, OnDestroy {
  private readonly api = inject(LiveFlightsService);
  private readonly theme = inject(ThemeService);
  private readonly container = viewChild.required<ElementRef<HTMLDivElement>>('container');

  protected readonly Math = Math;
  protected readonly networks = NETWORK_LABELS;
  protected readonly logos = NETWORK_LOGOS;
  protected readonly brokenLogos = signal(new Set<string>());
  protected readonly level = flightLevel;
  protected readonly altitude = altitudeFt;
  protected readonly minutes = minutesLabel;
  protected readonly devMode = isDevMode();

  protected readonly flights = signal<LiveFlight[]>([]);
  protected readonly demo = signal(false);
  protected readonly selectedId = signal<string | null>(null);
  protected readonly selected = computed(() => this.flights().find(f => f.id === this.selectedId()) ?? null);

  private map?: L.Map;
  private tiles?: L.TileLayer;
  private readonly markers = new Map<string, L.Marker>();
  private readonly routeLayer = L.layerGroup();
  private timer?: ReturnType<typeof setInterval>;
  private fitted = false;
  private routeToken = 0;

  constructor() {
    effect(() => {
      const dark = this.theme.isDark();
      if (this.map) this.setTiles(dark);
    });
  }

  ngAfterViewInit(): void {
    this.map = L.map(this.container().nativeElement, { scrollWheelZoom: true, worldCopyJump: true })
      .setView(DEFAULT_CENTER, DEFAULT_ZOOM);
    this.setTiles(this.theme.isDark());
    this.routeLayer.addTo(this.map);
    this.map.on('click', () => this.select(null));

    this.refresh();
    this.schedule();
  }

  ngOnDestroy(): void {
    clearInterval(this.timer);
    this.map?.remove();
  }

  protected tracker(flight: LiveFlight): { name: string; url: string } | null {
    return networkTracker(flight.network, flight.callsign);
  }

  protected logoFailed(network: string): void {
    this.brokenLogos.update(set => new Set(set).add(network));
  }

  protected toggleDemo(): void {
    this.demo.update(on => !on);
    if (this.selectedId()?.startsWith('demo-')) this.select(null);
    this.fitted = false;
    this.refresh();
    this.schedule();
  }

  protected select(id: string | null, scrollToMap = false): void {
    this.selectedId.set(id);
    this.redrawMarkers();
    this.drawRoute(true);

    if (id && scrollToMap) this.container().nativeElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  private schedule(): void {
    clearInterval(this.timer);
    this.timer = setInterval(() => this.refresh(), this.demo() ? DEMO_REFRESH_MS : REFRESH_MS);
  }

  private async refresh(): Promise<void> {
    let real: LiveFlight[] = [];
    try {
      real = await this.api.list();
    } catch {
      if (!this.demo()) return;
    }

    const flights = this.demo() ? [...real, ...demoTraffic(Date.now())] : real;
    this.flights.set(flights);

    if (this.selectedId() && !this.selected()) this.selectedId.set(null);

    this.redrawMarkers();
    this.drawRoute(false);
    this.fitAll();
  }

  private redrawMarkers(): void {
    if (!this.map) return;

    const flights = this.flights();
    const ids = new Set(flights.map(f => f.id));

    for (const [id, marker] of this.markers) {
      if (!ids.has(id)) {
        marker.remove();
        this.markers.delete(id);
      }
    }

    for (const flight of flights) {
      const isSelected = flight.id === this.selectedId();
      const icon = planeIcon(flight, isSelected);
      const position: L.LatLngTuple = [flight.latitude, flight.longitude];
      const existing = this.markers.get(flight.id);

      if (existing) {
        existing.setLatLng(position).setIcon(icon).setZIndexOffset(isSelected ? 1000 : 0);
      } else {
        const marker = L.marker(position, { icon, zIndexOffset: isSelected ? 1000 : 0 })
          .on('click', event => {
            L.DomEvent.stopPropagation(event);
            this.select(flight.id);
          })
          .addTo(this.map);
        this.markers.set(flight.id, marker);
      }
    }
  }

  private async drawRoute(fit: boolean): Promise<void> {
    const flight = this.selected();
    const token = ++this.routeToken;
    this.routeLayer.clearLayers();
    if (!flight || !this.map) return;

    const [from, current, to] = unwrapLongitudes([
      [flight.departure.latitude, flight.departure.longitude],
      [flight.latitude, flight.longitude],
      [flight.arrival.latitude, flight.arrival.longitude]
    ]);

    L.polyline([from, to], { color: '#7FB1F3', weight: 2, dashArray: '6 6', opacity: 0.9 }).addTo(this.routeLayer);
    L.marker(from, { icon: airportIcon('DEP'), interactive: false }).addTo(this.routeLayer);
    L.marker(to, { icon: airportIcon('ARR'), interactive: false }).addTo(this.routeLayer);

    if (fit) {
      this.map.fitBounds(L.latLngBounds([from, current, to]), { paddingTopLeft: PANEL_PADDING, paddingBottomRight: [60, 60], maxZoom: 9 });
    }

    const track = await this.loadTrack(flight.id);
    if (token !== this.routeToken || track.length === 0) return;

    const points = unwrapLongitudes([
      from,
      ...track.map(p => [p.latitude, p.longitude] as [number, number]),
      [flight.latitude, flight.longitude]
    ]);

    L.polyline(points, { color: '#E09B1A', weight: 3, opacity: 0.95 }).addTo(this.routeLayer);
  }

  private async loadTrack(id: string): Promise<TrackPoint[]> {
    if (id.startsWith('demo-')) return demoTrack(id, Date.now());

    try {
      return await this.api.track(id);
    } catch {
      return [];
    }
  }

  private fitAll(): void {
    const flights = this.flights();
    if (this.fitted || flights.length === 0 || !this.map || this.selectedId()) return;

    this.fitted = true;
    const bounds = L.latLngBounds(flights.map(f => [f.latitude, f.longitude] as L.LatLngTuple));
    this.map.fitBounds(bounds, { padding: [80, 80], maxZoom: 7 });
  }

  private setTiles(dark: boolean): void {
    this.tiles?.remove();
    this.tiles = L.tileLayer(dark ? TILE_URL.dark : TILE_URL.light, {
      attribution: ATTRIBUTION,
      maxZoom: 19
    }).addTo(this.map!);
  }
}