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

@Component({
  selector: 'app-track-map',
  host: { class: 'block' },
  template: `<div #container class="h-full w-full"></div>`
})
export class TrackMap implements AfterViewInit, OnDestroy {
  readonly track = input.required<TrackPoint[]>();

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

    this.map = L.map(this.container().nativeElement, { scrollWheelZoom: false });
    this.setTiles(this.theme.isDark());

    if (points.length === 0) {
      this.map.setView([-15, -50], 4);
      return;
    }

    const line = L.polyline(points, { color: '#1D6FD8', weight: 3 }).addTo(this.map);
    const dot = { radius: 6, color: '#FFFFFF', weight: 2, fillOpacity: 1 };
    L.circleMarker(points[0], { ...dot, fillColor: '#2EC4B6' }).addTo(this.map);
    L.circleMarker(points[points.length - 1], { ...dot, fillColor: '#E09B1A' }).addTo(this.map);

    this.map.fitBounds(line.getBounds(), { padding: [30, 30], maxZoom: 12 });
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