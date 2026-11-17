import { AfterViewInit, Component, ElementRef, OnDestroy, effect, inject, input, viewChild } from '@angular/core';
import * as L from 'leaflet';
import { APP_CONFIG } from '../../core/config/app-config';
import { TrackPoint } from '../../core/pirep/pirep.models';
import { ThemeService } from '../../core/theme/theme.service';

const TILE_URL = {
  light: `https://basemaps.cartocdn.com/rastertiles/light_all/{z}/{x}/{y}.png?key=${APP_CONFIG.cartoApiKey}`,
  dark: `https://basemaps.cartocdn.com/rastertiles/dark_all/{z}/{x}/{y}.png?key=${APP_CONFIG.cartoApiKey}`
};

const ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> &copy; <a href="https://carto.com/attributions">CARTO</a>';

export interface MapAirport {
  icao: string;
  latitude: number;
  longitude: number;
}

@Component({
  selector: 'app-track-map',
  host: { class: 'block' },
  template: `<div #container class="h-full w-full"></div>`
})
export class TrackMap implements AfterViewInit, OnDestroy {
  readonly track = input.required<TrackPoint[]>();
  readonly departure = input<MapAirport | null>(null);
  readonly arrival = input<MapAirport | null>(null);

  private readonly theme = inject(ThemeService);
  private readonly container = viewChild.required<ElementRef<HTMLDivElement>>('container');

  private map?: L.Map;
  private tiles?: L.TileLayer;

  constructor() {
    effect(() => {
      const dark = this.theme.isDark();
      if (this.map) this.setTiles(dark);
    });
  }

  ngAfterViewInit(): void {
    const points = this.track().map(p => [p.latitude, p.longitude] as L.LatLngTuple);
    const departure = this.departure();
    const arrival = this.arrival();

    this.map = L.map(this.container().nativeElement, { scrollWheelZoom: false });
    this.setTiles(this.theme.isDark());

    const bounds = L.latLngBounds([]);

    if (departure && arrival && (departure.latitude || departure.longitude) && (arrival.latitude || arrival.longitude)) {
      const planned: L.LatLngTuple[] = [[departure.latitude, departure.longitude], [arrival.latitude, arrival.longitude]];
      L.polyline(planned, { color: '#8B5CF6', weight: 2, dashArray: '6 6', opacity: 0.9 }).addTo(this.map);
      this.airportMarker(planned[0], departure.icao, 'DEP');
      this.airportMarker(planned[1], arrival.icao, 'ARR');
      planned.forEach(p => bounds.extend(p));
    }

    if (points.length > 0) {
      L.polyline(points, { color: '#E09B1A', weight: 3 }).addTo(this.map);
      const dot = { radius: 5, color: '#FFFFFF', weight: 2, fillOpacity: 1 };
      L.circleMarker(points[0], { ...dot, fillColor: '#2EC4B6' }).addTo(this.map);
      L.circleMarker(points[points.length - 1], { ...dot, fillColor: '#E09B1A' }).addTo(this.map);
      points.forEach(p => bounds.extend(p));
    }

    if (bounds.isValid()) {
      this.map.fitBounds(bounds, { padding: [40, 40], maxZoom: 12 });
    } else {
      this.map.setView([-15, -50], 4);
    }
  }

  private airportMarker(position: L.LatLngTuple, icao: string, label: string): void {
    L.circleMarker(position, { radius: 6, color: '#FFFFFF', weight: 2, fillColor: '#1D6FD8', fillOpacity: 1 })
      .bindTooltip(`<strong>${label}</strong> ${icao}`, { permanent: true, direction: 'top', offset: [0, -8], className: 'font-mono text-xs' })
      .addTo(this.map!);
  }

  ngOnDestroy(): void {
    this.map?.remove();
  }

  private setTiles(dark: boolean): void {
    this.tiles?.remove();
    this.tiles = L.tileLayer(dark ? TILE_URL.dark : TILE_URL.light, {
      attribution: ATTRIBUTION,
      maxZoom: 19
    }).addTo(this.map!);
  }
}