import { AfterViewInit, Component, ElementRef, OnDestroy, effect, inject, input, viewChild } from '@angular/core';
import * as L from 'leaflet';
import { APP_CONFIG } from '../../core/config/app-config';
import { FlightMap } from '../../core/profile/profile.models';
import { ThemeService } from '../../core/theme/theme.service';

const TILE_URL = {
  light: `https://basemaps.cartocdn.com/rastertiles/light_all/{z}/{x}/{y}.png?key=${APP_CONFIG.cartoApiKey}`,
  dark: `https://basemaps.cartocdn.com/rastertiles/dark_all/{z}/{x}/{y}.png?key=${APP_CONFIG.cartoApiKey}`
};

const ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> &copy; <a href="https://carto.com/attributions">CARTO</a>';

const AIRPORT_ICON = L.divIcon({
  className: '',
  html: `<span style="display:flex;width:22px;height:22px;align-items:center;justify-content:center;border-radius:9999px;background:#E09B1A;border:2px solid #fff;box-shadow:0 1px 4px rgba(0,0,0,.4);color:#fff;font-size:12px"><i class="ti ti-plane"></i></span>`,
  iconSize: [22, 22],
  iconAnchor: [11, 11]
});

@Component({
  selector: 'app-flights-map',
  host: { class: 'block' },
  template: `<div #container class="h-full w-full"></div>`
})
export class FlightsMap implements AfterViewInit, OnDestroy {
  readonly data = input.required<FlightMap>();

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
    this.map = L.map(this.container().nativeElement, { worldCopyJump: true, minZoom: 2 });
    this.setTiles(this.theme.isDark());

    const { airports, routes } = this.data();
    const byIcao = new Map(airports.map(a => [a.icao, a]));
    const maxFlights = Math.max(1, ...routes.map(r => r.flights));
    const bounds: L.LatLngTuple[] = [];

    for (const route of routes) {
      const from = byIcao.get(route.departureIcao);
      const to = byIcao.get(route.arrivalIcao);
      if (!from || !to) continue;

      let toLon = to.longitude;
      while (toLon - from.longitude > 180) toLon -= 360;
      while (toLon - from.longitude < -180) toLon += 360;

      L.polyline([[from.latitude, from.longitude], [to.latitude, toLon]], {
        color: '#4A8FEA',
        weight: 1.5 + (route.flights / maxFlights) * 2.5,
        opacity: 0.75
      })
        .bindTooltip(`${route.departureIcao} → ${route.arrivalIcao} · ${route.flights} ${route.flights === 1 ? 'flight' : 'flights'}`, { sticky: true })
        .addTo(this.map);
    }

    for (const airport of airports) {
      L.marker([airport.latitude, airport.longitude], { icon: AIRPORT_ICON })
        .bindTooltip(`<strong>${airport.icao}</strong> · ${airport.city || airport.name}`, { direction: 'top', offset: [0, -10] })
        .addTo(this.map);
      bounds.push([airport.latitude, airport.longitude]);
    }

    if (bounds.length === 0) this.map.setView([-15, -50], 3);
    else this.map.fitBounds(L.latLngBounds(bounds), { padding: [40, 40], maxZoom: 6 });
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