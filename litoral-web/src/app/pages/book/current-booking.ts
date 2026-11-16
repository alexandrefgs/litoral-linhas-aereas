import { Component, OnDestroy, computed, inject, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { apiErrorMessage } from '../../core/api/api-error';
import { Booking } from '../../core/booking/booking.models';
import { BookingService } from '../../core/booking/booking.service';
import { formatMinutes } from '../../shared/utils/flight';

const TYPE_LABELS: Record<Booking['type'], string> = {
  Random: 'Random flight',
  Custom: 'Custom flight',
  Monthly: 'Monthly roster',
  Special: 'Special schedule'
};

@Component({
  selector: 'app-current-booking',
  imports: [RouterLink],
  template: `
    @let flight = booking();
    <div class="panel overflow-hidden">
      <div class="flex flex-wrap items-center justify-between gap-4 border-b border-line bg-primary px-6 py-4 text-on-primary">
        <div>
          <p class="text-xs font-semibold uppercase tracking-widest opacity-80">{{ typeLabel() }}</p>
          <p class="font-mono text-3xl font-extrabold">{{ flight.callsign }}</p>
        </div>
        <div class="text-right">
          <p class="text-xs uppercase tracking-widest opacity-80">{{ flight.status === 'InProgress' ? 'In progress' : 'Expires in' }}</p>
          <p class="text-2xl font-bold">{{ flight.status === 'InProgress' ? '—' : remaining() }}</p>
        </div>
      </div>

      <div class="grid gap-6 px-6 py-5 md:grid-cols-[1fr_auto_1fr] md:items-center">
        <div>
          <p class="font-mono text-3xl font-bold text-ink">{{ flight.departure.icao }}</p>
          <p class="text-ink-muted">{{ flight.departure.city }}</p>
        </div>
        <div class="flex flex-col items-center text-ink-muted">
          <i class="ti ti-plane text-3xl text-primary" aria-hidden="true"></i>
          <span class="text-sm">{{ flight.distanceNm }} NM · {{ format(flight.estimatedMinutes) }}</span>
        </div>
        <div class="md:text-right">
          <p class="font-mono text-3xl font-bold text-ink">{{ flight.arrival.icao }}</p>
          <p class="text-ink-muted">{{ flight.arrival.city }}</p>
        </div>
      </div>

      <div class="grid gap-4 border-t border-line px-6 py-4 text-sm sm:grid-cols-3">
        <div>
          <p class="text-xs uppercase tracking-wide text-ink-muted">Aircraft</p>
          <p class="font-semibold text-ink"><span class="font-mono">{{ flight.aircraft.registration }}</span> · {{ flight.aircraft.typeIcao }}</p>
        </div>
        <div>
          <p class="text-xs uppercase tracking-wide text-ink-muted">Crew</p>
          <p class="font-semibold text-ink">{{ flight.crew ? flight.crew.displayId + ' · ' + flight.crew.name : 'No second pilot available' }}</p>
        </div>
        <div>
          <p class="text-xs uppercase tracking-wide text-ink-muted">Network callsign if taken</p>
          <p class="font-mono font-semibold text-ink">{{ flight.networkCallsign }}</p>
        </div>
      </div>

      @if (error(); as message) {
        <p class="border-t border-danger/30 bg-danger/10 px-6 py-3 text-sm text-danger">{{ message }}</p>
      }

      <div class="flex flex-wrap items-center justify-end gap-2 border-t border-line px-6 py-4">
        @if (confirmCancel()) {
          <span class="text-sm text-ink-muted">{{ cancelWarning() }}</span>
          <button type="button" class="rounded-lg bg-danger px-4 py-2 text-sm font-medium text-white hover:opacity-90" [disabled]="cancelling()" (click)="cancel()">
            {{ cancelling() ? 'Cancelling...' : 'Yes, cancel' }}
          </button>
          <button type="button" class="btn-secondary" (click)="confirmCancel.set(false)">Keep flight</button>
        } @else {
          <button type="button" class="btn-secondary hover:text-danger" (click)="confirmCancel.set(true)">Cancel flight</button>
          @if (showBriefingLink()) {
            <a routerLink="/briefing" class="btn-primary gap-2 px-5"><i class="ti ti-file-description" aria-hidden="true"></i>Open briefing</a>
          }
        }
      </div>
    </div>
  `
})
export class CurrentBooking implements OnDestroy {
  private readonly api = inject(BookingService);

  readonly booking = input.required<Booking>();
  readonly showBriefingLink = input(true);
  readonly cancelled = output<void>();

  protected readonly format = formatMinutes;
  protected readonly confirmCancel = signal(false);
  protected readonly cancelling = signal(false);
  protected readonly error = signal<string | null>(null);
  private readonly now = signal(Date.now());
  private readonly timer = setInterval(() => this.now.set(Date.now()), 30_000);

  protected readonly typeLabel = computed(() => TYPE_LABELS[this.booking().type]);

  protected readonly remaining = computed(() => {
    const minutes = Math.max(0, Math.round((Date.parse(this.booking().expiresAt) - this.now()) / 60_000));
    return formatMinutes(minutes);
  });

  protected readonly cancelWarning = computed(() => {
    const flight = this.booking();
    if (flight.type === 'Special') return 'Cancelling a special schedule costs 500 points.';
    if (flight.pointsCost > 0) return `The ${flight.pointsCost} points are not refunded.`;
    return 'Cancel this flight?';
  });

  ngOnDestroy(): void {
    clearInterval(this.timer);
  }

  protected async cancel(): Promise<void> {
    this.cancelling.set(true);
    this.error.set(null);

    try {
      await this.api.cancel();
      this.cancelled.emit();
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.cancelling.set(false);
      this.confirmCancel.set(false);
    }
  }
}