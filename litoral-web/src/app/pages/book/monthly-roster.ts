import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { apiErrorMessage } from '../../core/api/api-error';
import { AircraftOptions, RosterLeg, RosterStatus } from '../../core/booking/booking.models';
import { BookingService } from '../../core/booking/booking.service';
import { formatMinutes } from '../../shared/utils/flight';
import { AircraftPickerModal } from './aircraft-picker-modal';

const MONTHS = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

@Component({
  selector: 'app-monthly-roster',
  imports: [AircraftPickerModal],
  template: `
    @if (loading()) {
      <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
    } @else if (status(); as data) {
      @if (data.roster; as roster) {
        <div class="flex flex-col gap-5">
          <div class="panel flex flex-wrap items-center justify-between gap-4 p-6">
            <div>
              <h2 class="text-lg font-bold text-ink">{{ monthName(roster.month) }} {{ roster.year }} roster</h2>
              <p class="text-sm text-ink-muted">Base {{ roster.baseIcao }} · {{ roster.flightsPerDay }} flight(s) per day · Bonus of {{ roster.bonus }} points when completed</p>
            </div>
            <div class="w-full sm:w-64">
              <div class="mb-1 flex justify-between text-sm">
                <span class="text-ink-muted">Progress</span>
                <span class="font-semibold text-ink">{{ roster.completedFlights }} / {{ roster.totalFlights }}</span>
              </div>
              <div class="h-2 overflow-hidden rounded-full bg-surface-alt">
                <div class="h-full rounded-full bg-primary" [style.width.%]="roster.completedFlights / roster.totalFlights * 100"></div>
              </div>
            </div>
          </div>

          @if (roster.completedAt) {
            <p class="rounded-lg bg-accent/20 px-4 py-3 text-sm text-ink">
              <i class="ti ti-trophy mr-1" aria-hidden="true"></i>Roster completed! A new one will be available next month.
            </p>
          }
          @if (data.hasOtherOpenBooking) {
            <p class="rounded-lg border border-highlight bg-highlight/15 px-4 py-3 text-sm text-ink">
              <i class="ti ti-alert-triangle mr-1" aria-hidden="true"></i>You already have an open flight. Complete or cancel it to continue your roster.
            </p>
          }

          @for (sequence of sequences(); track sequence.number) {
            <div class="panel p-5">
              <h3 class="mb-3 text-sm font-semibold uppercase tracking-wide text-ink-muted">Sequence {{ sequence.number }}</h3>
              <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                @for (leg of sequence.legs; track leg.id) {
                  <button type="button" (click)="open(leg)" [disabled]="leg.state !== 'Next' || data.hasOtherOpenBooking"
                    class="relative flex flex-col overflow-hidden rounded-xl border text-left transition"
                    [class]="legClass(leg)">
                    <div class="relative h-20 w-full bg-cover bg-center" [style.background-image]="cover(leg)">
                      @if (!leg.arrivalPhotoUrl) {
                        <i class="ti ti-building-airport absolute inset-0 m-auto h-fit w-fit text-3xl text-white/40" aria-hidden="true"></i>
                      }
                    </div>
                    <div class="flex flex-col gap-2 p-4">
                      <div class="flex items-center justify-between gap-2">
                        <span class="font-mono text-sm font-bold text-primary">{{ leg.callsign }}</span>
                        @switch (leg.state) {
                          @case ('Completed') { <i class="ti ti-circle-check-filled text-lg text-accent" aria-hidden="true"></i> }
                          @case ('Booked') { <span class="rounded-full bg-primary px-2 py-0.5 text-[11px] font-semibold text-on-primary">Booked</span> }
                          @case ('Next') {
                            @if (leg.departureIcao !== data.locationIcao) {
                              <span class="rounded-full bg-highlight px-2 py-0.5 text-[11px] font-semibold text-on-highlight" title="You'll get a free jumpseat to the departure">Unavailable</span>
                            } @else {
                              <span class="rounded-full bg-accent/20 px-2 py-0.5 text-[11px] font-semibold text-ink">Next</span>
                            }
                          }
                          @default { <i class="ti ti-lock text-ink-muted" aria-hidden="true"></i> }
                        }
                      </div>
                      <p class="font-mono text-xl font-bold text-ink">{{ leg.departureIcao }} <i class="ti ti-arrow-right text-base text-ink-muted" aria-hidden="true"></i> {{ leg.arrivalIcao }}</p>
                      <p class="text-xs text-ink-muted">{{ leg.arrivalCity }} · {{ format(leg.estimatedMinutes) }}
                        @if (leg.isCargo) { · <span class="font-semibold">Cargo</span> }
                      </p>
                    </div>
                  </button>
                }
              </div>
            </div>
          }
        </div>
      } @else if (data.canCreate) {
        <div class="panel flex flex-col gap-5 p-6">
          <div>
            <h2 class="text-lg font-bold text-ink">Monthly roster</h2>
            <p class="text-sm text-ink-muted">
              A sequence of flights for the rest of the month, starting and ending at your base. Complete it to earn 100 points per flight.
            </p>
          </div>
          <div class="grid gap-3 sm:grid-cols-3">
            @for (option of [1, 2, 3]; track option) {
              <label class="flex cursor-pointer flex-col gap-1 rounded-xl border px-5 py-4"
                [class]="perDay() === option ? 'border-primary bg-primary/5' : 'border-line hover:bg-surface-alt'">
                <span class="flex items-center gap-2">
                  <input type="radio" name="perDay" class="accent-primary" [checked]="perDay() === option" (change)="perDay.set(option)" />
                  <span class="font-semibold text-ink">{{ option }} per day</span>
                </span>
                <span class="text-sm text-ink-muted">{{ data.remainingDays * option }} flights · {{ data.remainingDays * option * 100 }} bonus points</span>
              </label>
            }
          </div>
          @if (error(); as message) {
            <p class="rounded-lg bg-danger/10 px-4 py-3 text-sm text-danger">{{ message }}</p>
          }
          <div>
            <button type="button" class="btn-primary gap-2 px-5" [disabled]="creating()" (click)="create()">
              <i class="ti ti-calendar-plus" aria-hidden="true"></i>{{ creating() ? 'Building roster...' : 'Generate my roster' }}
            </button>
          </div>
        </div>
      }
    } @else {
      <div class="panel flex flex-col items-center gap-3 px-6 py-12 text-center">
        <p class="text-danger">{{ error() ?? 'Something went wrong. Try again.' }}</p>
        <button type="button" class="btn-secondary" (click)="load()">Try again</button>
      </div>
    }

    @if (selected(); as leg) {
      <app-aircraft-picker-modal [title]="leg.callsign + ' · ' + leg.departureIcao + ' → ' + leg.arrivalIcao" [options]="options()"
        [saving]="saving()" [error]="error()" (confirm)="book(leg, $event)" (closed)="selected.set(null)" />
    }
  `
})
export class MonthlyRoster implements OnInit {
  private readonly api = inject(BookingService);

  protected readonly format = formatMinutes;
  protected readonly status = signal<RosterStatus | null>(null);
  protected readonly loading = signal(true);
  protected readonly creating = signal(false);
  protected readonly perDay = signal(1);
  protected readonly selected = signal<RosterLeg | null>(null);
  protected readonly options = signal<AircraftOptions | null>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly sequences = computed(() => {
    const groups = new Map<number, RosterLeg[]>();
    for (const leg of this.status()?.roster?.legs ?? []) {
      groups.set(leg.sequence, [...(groups.get(leg.sequence) ?? []), leg]);
    }
    return [...groups.entries()].map(([number, legs]) => ({ number, legs }));
  });

  ngOnInit(): void {
    this.load();
  }

  protected monthName(month: number): string {
    return MONTHS[month - 1];
  }

  protected legClass(leg: RosterLeg): string {
    switch (leg.state) {
      case 'Completed': return 'border-line bg-surface-alt/60 opacity-70';
      case 'Booked': return 'border-primary bg-primary/5';
      case 'Next': return 'border-primary hover:shadow-lg disabled:cursor-not-allowed disabled:hover:shadow-none';
      default: return 'border-line opacity-50 cursor-not-allowed';
    }
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      this.status.set(await this.api.roster());
    } catch (error) {
      this.status.set(null);
      this.error.set(apiErrorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected async create(): Promise<void> {
    this.creating.set(true);
    this.error.set(null);

    try {
      this.status.set(await this.api.createRoster(this.perDay()));
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.creating.set(false);
    }
  }

  protected async open(leg: RosterLeg): Promise<void> {
    this.selected.set(leg);
    this.options.set(null);
    this.error.set(null);

    try {
      this.options.set(await this.api.legAircraft(leg.id));
    } catch (error) {
      this.options.set({ routeId: leg.routeId, callsign: leg.callsign, validityHours: 24, aircraft: [] });
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    }
  }

  protected async book(leg: RosterLeg, aircraftId: string | null): Promise<void> {
    this.saving.set(true);
    this.error.set(null);

    try {
      await this.api.bookLeg(leg.id, aircraftId);
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.saving.set(false);
    }
  }
  protected cover(leg: RosterLeg): string {
    const gradient = 'linear-gradient(135deg, #0F3768 0%, #1D6FD8 60%, #2EC4B6 100%)';
    return leg.arrivalPhotoUrl ? `url('${leg.arrivalPhotoUrl}'), ${gradient}` : gradient;
  }
}