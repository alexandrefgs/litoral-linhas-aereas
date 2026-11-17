import { DatePipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { BookingAirport, FlightCategory, Metar } from '../../core/booking/booking.models';
import { countryFlagUrl } from '../../shared/utils/country-flag';

const CATEGORY_COLOR: Record<FlightCategory, string> = {
  VFR: '#22A699',
  MVFR: '#1D6FD8',
  IFR: '#EF476F',
  LIFR: '#8B5CF6'
};

@Component({
  selector: 'app-briefing-airport',
  imports: [DatePipe],
  host: { class: 'block' },
  template: `
    @let a = airport();
    @let m = metar();
    <article class="panel card-hover flex h-full flex-col overflow-hidden">
      <div class="relative h-44 bg-cover bg-center" [style.background-image]="cover()">
        @if (!a.photoUrl) {
          <i class="ti ti-building-airport absolute inset-0 m-auto h-fit w-fit text-5xl text-white/40" aria-hidden="true"></i>
        }
        @if (m?.category; as category) {
          <span class="absolute left-3 top-3 rounded-md px-2 py-0.5 text-xs font-bold text-white shadow" [style.background]="categoryColor(category)">{{ category }}</span>
        }
        @if (a.photoCredit) {
          <span class="absolute bottom-1 right-2 max-w-[70%] truncate text-[10px] text-white/80 [text-shadow:0_1px_2px_rgba(0,0,0,.8)]">© {{ a.photoCredit }}</span>
        }
      </div>

      <div class="flex flex-1 flex-col gap-4 p-5">
        <div class="flex items-start justify-between gap-3">
          <div class="min-w-0">
            <span class="rounded-md bg-surface-alt px-2 py-0.5 text-[11px] font-semibold uppercase tracking-wide text-ink-muted">{{ kind() === 'departure' ? 'Departure' : 'Arrival' }}</span>
            <p class="mt-1 flex items-baseline gap-2 font-mono">
              <span class="text-3xl font-extrabold text-ink">{{ a.icao }}</span>
              @if (a.iata) {
                <span class="text-sm text-ink-muted">{{ a.iata }}</span>
              }
            </p>
            <p class="truncate text-xs text-ink-muted" [title]="a.name">{{ a.city }} · {{ a.name }}</p>
          </div>
          <div class="flex shrink-0 flex-col items-end gap-2">
            <div class="flex items-center gap-2">
              @if (isBase()) {
                <span class="rounded bg-primary px-1.5 py-0.5 text-[10px] font-bold text-on-primary">BASE</span>
              }
              @if (flag(); as url) {
                <img [src]="url" [alt]="a.country" class="h-4 w-auto rounded-sm" />
              }
            </div>
            <a [href]="'https://chartfox.org/' + a.icao" target="_blank" rel="noopener" class="btn-secondary px-2.5 py-1 text-xs">
              <i class="ti ti-map-2 mr-1" aria-hidden="true"></i>Charts
            </a>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-3 text-sm sm:grid-cols-3">
          @for (item of details(); track item.label) {
            <div class="flex items-center gap-2.5">
              <span class="stat-icon h-9 w-9 text-lg"><i class="ti {{ item.icon }}" aria-hidden="true"></i></span>
              <div class="min-w-0">
                <p class="text-[11px] text-ink-muted">{{ item.label }}</p>
                <p class="font-semibold leading-tight text-ink">{{ item.value }}</p>
              </div>
            </div>
          }
        </div>

        <div class="mt-auto flex flex-col gap-2">
          <div>
            <p class="mb-1 flex items-center justify-between text-[11px] font-semibold uppercase tracking-wide text-ink-muted">
              METAR
              @if (m?.observedAt) {
                <span class="font-mono normal-case">{{ m!.observedAt | date: 'HH:mm' : 'UTC' }}Z</span>
              }
            </p>
            <p class="rounded-lg bg-surface-alt px-3 py-2 font-mono text-xs leading-relaxed text-ink">{{ m?.raw ?? 'METAR not available.' }}</p>
          </div>
          <div>
            <p class="mb-1 text-[11px] font-semibold uppercase tracking-wide text-ink-muted">TAF</p>
            <p class="whitespace-pre-line rounded-lg bg-surface-alt px-3 py-2 font-mono text-xs leading-relaxed text-ink">{{ m?.taf ?? 'TAF not available.' }}</p>
          </div>
        </div>
      </div>
    </article>
  `
})
export class BriefingAirport {
  readonly airport = input.required<BookingAirport>();
  readonly metar = input<Metar | null>(null);
  readonly kind = input.required<'departure' | 'arrival'>();
  readonly isBase = input(false);
  readonly runway = input<string | null>(null);
  readonly alternate = input<string | null>(null);

  protected readonly flag = computed(() => countryFlagUrl(this.airport().country));

  protected readonly cover = computed(() => {
    const gradient = 'linear-gradient(135deg, #0F3768 0%, #1D6FD8 60%, #2EC4B6 100%)';
    const url = this.airport().photoUrl;
    return url ? `url('${url}'), ${gradient}` : gradient;
  });

  protected readonly details = computed(() => {
    const m = this.metar();
    const departure = this.kind() === 'departure';
    return [
      { icon: 'ti-temperature', label: 'Temperature', value: m?.temperatureC != null ? `${m.temperatureC} °C` : '—' },
      { icon: 'ti-wind', label: 'Wind', value: this.wind(m) },
      { icon: 'ti-gauge', label: 'QNH', value: m?.qnhHpa != null ? `${m.qnhHpa} hPa` : '—' },
      { icon: departure ? 'ti-plane-departure' : 'ti-plane-arrival', label: departure ? 'Departure RWY' : 'Arrival RWY', value: this.runway() ?? '—' },
      { icon: 'ti-eye', label: 'Visibility', value: m?.visibility ?? '—' },
      departure
        ? { icon: 'ti-droplet', label: 'Dew point', value: m?.dewpointC != null ? `${m.dewpointC} °C` : '—' }
        : { icon: 'ti-arrow-fork', label: 'Alternate', value: this.alternate() ?? '—' }
    ];
  });

  protected categoryColor(category: FlightCategory): string {
    return CATEGORY_COLOR[category] ?? '#1D6FD8';
  }

  private wind(m: Metar | null): string {
    if (!m || m.windSpeedKt == null) return '—';
    if (m.windSpeedKt === 0) return 'Calm';
    const direction = m.windDirection === 'VRB' ? 'VRB' : `${m.windDirection ?? '—'}°`;
    const gust = m.windGustKt ? ` G${m.windGustKt}` : '';
    return `${direction}/${m.windSpeedKt}${gust} kt`;
  }
}