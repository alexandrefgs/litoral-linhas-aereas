import { Component, inject, signal } from '@angular/core';
import { apiErrorMessage } from '../../core/api/api-error';
import { FlightDuration, RandomProposal } from '../../core/booking/booking.models';
import { BookingService } from '../../core/booking/booking.service';
import { formatMinutes } from '../../shared/utils/flight';

@Component({
  selector: 'app-random-flight',
  template: `
    <div class="flex flex-col gap-5">
      <div class="panel flex flex-col gap-4 p-6">
        <div>
          <h2 class="text-lg font-bold text-ink">Random flight</h2>
          <p class="text-sm text-ink-muted">Pick a duration and we'll choose a route and an aircraft available at your airport. Always free.</p>
        </div>
        <div class="flex flex-wrap gap-2">
          @for (option of durations; track option.value) {
            <button type="button" (click)="duration.set(option.value)"
              class="rounded-lg border px-4 py-2 text-sm font-medium"
              [class]="duration() === option.value ? 'border-primary bg-primary text-on-primary' : 'border-line text-ink hover:bg-surface-alt'">
              {{ option.label }}
            </button>
          }
        </div>
        <div>
          <button type="button" class="btn-primary gap-2 px-5" [disabled]="loading()" (click)="propose()">
            <i class="ti ti-arrows-shuffle" aria-hidden="true"></i>{{ loading() ? 'Searching...' : proposal() ? 'Try another' : 'Find a flight' }}
          </button>
        </div>
        @if (error(); as message) {
          <p class="rounded-lg bg-danger/10 px-4 py-3 text-sm text-danger"><i class="ti ti-alert-circle mr-1" aria-hidden="true"></i>{{ message }}</p>
        }
      </div>

      @if (proposal(); as flight) {
        <div class="panel overflow-hidden">
          <div class="grid gap-6 p-6 md:grid-cols-[1fr_280px]">
            <div class="flex flex-col gap-4">
              <div class="flex items-center gap-3">
                <span class="font-mono text-2xl font-extrabold text-primary">{{ flight.route.callsign }}</span>
                @if (flight.route.isCargo) {
                  <span class="rounded-full bg-accent/20 px-2.5 py-1 text-xs font-semibold text-ink">Cargo</span>
                }
              </div>
              <div class="flex items-center gap-4">
                <div>
                  <p class="font-mono text-3xl font-bold text-ink">{{ origin() }}</p>
                </div>
                <i class="ti ti-arrow-right text-2xl text-primary" aria-hidden="true"></i>
                <div>
                  <p class="font-mono text-3xl font-bold text-ink">{{ flight.route.arrival.icao }}</p>
                  <p class="text-sm text-ink-muted">{{ flight.route.arrival.city }}, {{ flight.route.arrival.country }}</p>
                </div>
              </div>
              <p class="text-ink-muted">{{ flight.route.distanceNm }} NM · {{ format(flight.route.estimatedMinutes) }}</p>
            </div>
            <div class="flex flex-col gap-2">
              <div class="flex aspect-[16/9] items-center justify-center overflow-hidden rounded-lg bg-surface-alt">
                @if (flight.aircraft.photoUrl) {
                  <img [src]="flight.aircraft.photoUrl" [alt]="flight.aircraft.registration" class="h-full w-full object-cover" />
                } @else {
                  <i class="ti ti-plane text-5xl text-ink-muted" aria-hidden="true"></i>
                }
              </div>
              <p class="text-center"><span class="font-mono font-bold text-ink">{{ flight.aircraft.registration }}</span>
                <span class="text-sm text-ink-muted"> · {{ flight.aircraft.typeName }}</span></p>
            </div>
          </div>
          <div class="flex justify-end gap-2 border-t border-line px-6 py-4">
            <button type="button" class="btn-primary px-5" [disabled]="saving()" (click)="book(flight)">
              {{ saving() ? 'Booking...' : 'Book this flight' }}
            </button>
          </div>
        </div>
      }
    </div>
  `
})
export class RandomFlight {
  private readonly api = inject(BookingService);

  protected readonly format = formatMinutes;
  protected readonly duration = signal<FlightDuration>('Any');
  protected readonly proposal = signal<RandomProposal | null>(null);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly origin = signal('');

  protected readonly durations: { value: FlightDuration; label: string }[] = [
    { value: 'Any', label: 'Any' },
    { value: 'Short', label: 'Up to 1h' },
    { value: 'Medium', label: '1h – 2h' },
    { value: 'Long', label: 'Over 2h' }
  ];

  protected async propose(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const [proposal, overview] = await Promise.all([this.api.proposeRandom(this.duration()), this.api.overview()]);
      this.origin.set(overview.locationIcao);
      this.proposal.set(proposal);
    } catch (error) {
      this.proposal.set(null);
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.loading.set(false);
    }
  }

  protected async book(flight: RandomProposal): Promise<void> {
    this.saving.set(true);
    this.error.set(null);

    try {
      await this.api.bookRandom(flight.route.routeId, flight.aircraft.id);
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.saving.set(false);
    }
  }
}