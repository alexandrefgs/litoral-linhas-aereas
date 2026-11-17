import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { apiErrorMessage } from '../../core/api/api-error';
import { AircraftOptions, Destination } from '../../core/booking/booking.models';
import { BookingService } from '../../core/booking/booking.service';
import { formatMinutes } from '../../shared/utils/flight';
import { AircraftPickerModal } from './aircraft-picker-modal';

@Component({
  selector: 'app-custom-flight',
  imports: [AircraftPickerModal],
  template: `
    <div class="flex flex-col gap-4">
      <div class="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h2 class="text-lg font-bold text-ink">Custom flight</h2>
          <p class="text-sm text-ink-muted">Choose any destination from your airport. Free to a base, {{ customCost() }} points elsewhere.</p>
        </div>
        <div class="relative w-full sm:w-72">
          <i class="ti ti-search pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-ink-muted" aria-hidden="true"></i>
          <input type="search" class="field-input pl-9" placeholder="Search destination"
            [value]="search()" (input)="search.set($any($event.target).value)" />
        </div>
      </div>

      @if (loading()) {
        <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (loadError()) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-12 text-center">
          <p class="text-danger">Something went wrong. Try again.</p>
          <button type="button" class="btn-secondary" (click)="load()">Try again</button>
        </div>
      } @else if (!filtered().length) {
        <div class="panel flex flex-col items-center gap-2 px-6 py-12 text-center">
          <i class="ti ti-route-off text-4xl text-ink-muted" aria-hidden="true"></i>
          <p class="text-ink-muted">{{ destinations().length ? 'No destinations match your search.' : 'There are no routes from your airport yet.' }}</p>
        </div>
      } @else {
        <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          @for (item of filtered(); track item.routeId) {
                        <button type="button" (click)="open(item)"
                class="panel card-hover flex flex-col overflow-hidden text-left"
              [class.opacity-60]="!item.availableAircraft">
              <div class="relative h-32 w-full bg-cover bg-center" [style.background-image]="cover(item)">
                @if (!item.arrival.photoUrl) {
                  <i class="ti ti-building-airport absolute inset-0 m-auto h-fit w-fit text-4xl text-white/40" aria-hidden="true"></i>
                }
                @if (item.arrival.photoCredit) {
                  <span class="absolute bottom-1 left-2 max-w-[70%] truncate text-[10px] text-white/80 [text-shadow:0_1px_2px_rgba(0,0,0,.8)]">© {{ item.arrival.photoCredit }}</span>
                }
              </div>
              <div class="flex flex-1 flex-col gap-3 p-5">
                <div class="flex items-start justify-between gap-2">
                  <div>
                    <p class="font-mono text-3xl font-extrabold text-ink">{{ item.arrival.icao }}</p>
                    <p class="text-sm text-ink-muted">{{ item.arrival.city }}, {{ item.arrival.country }}</p>
                  </div>
                  <span class="rounded-full px-2.5 py-1 text-xs font-bold"
                    [class]="item.cost ? 'bg-highlight text-on-highlight' : 'bg-accent/20 text-ink'">
                    {{ item.cost ? item.cost + ' pts' : 'Free' }}
                  </span>
                </div>
                <div class="flex items-center gap-2 text-sm">
                  <span class="font-mono font-semibold text-primary">{{ item.callsign }}</span>
                  @if (item.isCargo) {
                    <span class="rounded-full bg-accent/20 px-2 py-0.5 text-[11px] font-semibold text-ink">Cargo</span>
                  }
                </div>
                <div class="flex flex-wrap items-center justify-between gap-2 border-t border-line pt-3 text-sm text-ink-muted">
                  <span>{{ item.distanceNm }} NM · {{ format(item.estimatedMinutes) }}</span>
                  <span [class.text-danger]="!item.availableAircraft">
                    <i class="ti ti-plane mr-1" aria-hidden="true"></i>{{ item.availableAircraft || 'No' }} aircraft
                  </span>
                </div>
                <div class="flex flex-wrap gap-1">
                  @for (type of item.aircraftTypes; track type) {
                    <span class="rounded-md bg-surface-alt px-2 py-0.5 font-mono text-xs font-semibold text-ink">{{ type }}</span>
                  }
                </div>
              </div>
            </button>
          }
        </div>
      }
    </div>

    @if (selected(); as item) {
      <app-aircraft-picker-modal [title]="item.callsign + ' · ' + item.arrival.icao" [options]="options()" [cost]="item.cost"
        [saving]="saving()" [error]="error()" (confirm)="book(item, $event)" (closed)="selected.set(null)" />
    }
  `
})
export class CustomFlight implements OnInit {
  private readonly api = inject(BookingService);

  protected readonly format = formatMinutes;
  protected readonly destinations = signal<Destination[]>([]);
  protected readonly customCost = signal(50);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly search = signal('');
  protected readonly selected = signal<Destination | null>(null);
  protected readonly options = signal<AircraftOptions | null>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    return this.destinations().filter(d =>
      !term
      || d.arrival.icao.toLowerCase().includes(term)
      || d.arrival.city.toLowerCase().includes(term)
      || d.arrival.country.toLowerCase().includes(term)
      || d.callsign.toLowerCase().includes(term));
  });

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);

    try {
      const [destinations, overview] = await Promise.all([this.api.destinations(), this.api.overview()]);
      this.destinations.set(destinations);
      this.customCost.set(overview.customCost);
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected async open(item: Destination): Promise<void> {
    this.selected.set(item);
    this.options.set(null);
    this.error.set(null);

    try {
      this.options.set(await this.api.routeAircraft(item.routeId));
    } catch (error) {
      this.options.set({ routeId: item.routeId, callsign: item.callsign, validityHours: 24, aircraft: [] });
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    }
  }

  protected async book(item: Destination, aircraftId: string | null): Promise<void> {
    this.saving.set(true);
    this.error.set(null);

    try {
      await this.api.bookCustom(item.routeId, aircraftId);
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.saving.set(false);
    }
  }
  protected cover(item: Destination): string {
    const gradient = 'linear-gradient(135deg, #0F3768 0%, #1D6FD8 60%, #2EC4B6 100%)';
    return item.arrival.photoUrl ? `url('${item.arrival.photoUrl}'), ${gradient}` : gradient;
  }
}