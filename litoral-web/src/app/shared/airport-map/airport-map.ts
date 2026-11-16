import { AfterViewInit, Component, ElementRef, OnDestroy, effect, inject, input, viewChild } from '@angular/core';
import * as L from 'leaflet';
import { APP_CONFIG } from '../../core/config/app-config';
import { ThemeService } from '../../core/theme/theme.service';

const TILE_URL = {
  light: `https://basemaps.cartocdn.com/rastertiles/light_all/{z}/{x}/{y}.png?key=${APP_CONFIG.cartoApiKey}`,
  dark: `https://basemaps.cartocdn.com/rastertiles/dark_all/{z}/{x}/{y}.png?key=${APP_CONFIG.cartoApiKey}`
};

const ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> &copy; <a href="https://carto.com/attributions">CARTO</a>';

@Component({
  selector: 'app-airport-map',
  host: { class: 'block' },
  template: `<div #container class="h-full w-full"></div>`
})
export class AirportMap implements AfterViewInit, OnDestroy {
  readonly latitude = input.required<number>();
  readonly longitude = input.required<number>();
  readonly label = input('');
  readonly zoom = input(13);

  private readonly theme = inject(ThemeService);
  private readonly container = viewChild.required<ElementRef<HTMLDivElement>>('container');

  private map?: L.Map;
  private tiles?: L.TileLayer;
  private marker?: L.CircleMarker;

  constructor() {
    effect(() => {
      const dark = this.theme.isDark();
      if (this.map) this.setTiles(dark);
    });

    effect(() => {
      const position: L.LatLngExpression = [this.latitude(), this.longitude()];
      if (!this.map) return;
      this.map.setView(position, this.zoom());
      this.marker?.setLatLng(position);
    });
  }

  ngAfterViewInit(): void {
    const position: L.LatLngExpression = [this.latitude(), this.longitude()];

    this.map = L.map(this.container().nativeElement, { scrollWheelZoom: false }).setView(position, this.zoom());
    this.setTiles(this.theme.isDark());

    this.marker = L.circleMarker(position, {
      radius: 8,
      color: '#FFFFFF',
      weight: 3,
      fillColor: '#1D6FD8',
      fillOpacity: 1
    }).addTo(this.map);

    if (this.label()) {
      this.marker.bindTooltip(this.label(), { direction: 'top', offset: [0, -8] });
    }
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