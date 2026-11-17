import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { apiErrorMessage } from '../../core/api/api-error';
import {
  PilotSpecialFlight,
  SPECIAL_FLIGHT_STATE,
  SPECIAL_FLIGHT_TYPE_LABEL,
  SpecialFlight,
  specialFlightRoute
} from '../../core/special-flights/special-flights.models';
import { SpecialFlightsService } from '../../core/special-flights/special-flights.service';

type Filter = 'open' | 'mine' | 'history';

const OPEN_STATES = ['Available', 'ComingSoon', 'Taken'];

@Component({
  selector: 'app-special-flights',
  imports: [DatePipe, RouterLink],
  template: `
    <section class="mx-auto flex max-w-7xl flex-col gap-6 px-6 py-8">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 class="text-2xl font-bold text-ink">Special Flights</h1>
          <p class="text-ink-muted">Ferries, redeliveries and charters. The first pilot to take it gets the flight. You must be at the departure airport.</p>
        </div>
        <div class="flex rounded-lg border border-line p-1">
          @for (option of filters; track option.value) {
            <button type="button" (click)="filter.set(option.value)"
              class="rounded-md px-3 py-1.5 text-sm font-medium"
              [class]="filter() === option.value ? 'bg-primary text-on-primary' : 'text-ink hover:bg-surface-alt'">
              {{ option.label }}
            </button>
          }
        </div>
      </div>

      @if (message(); as m) {
        <div class="flex flex-wrap items-center justify-between gap-3 rounded-md px-4 py-3 text-sm" [class]="m.ok ? 'bg-accent/15 text-ink' : 'bg-danger/10 text-danger'">
          <span>{{ m.text }}</span>
          @if (m.ok) {
            <a routerLink="/briefing" class="btn-primary px-3 py-1.5">Go to briefing</a>
          }
        </div>
      }

      @if (loading() && !items().length) {
        <p class="py-24 text-center text-ink-muted">Loading...</p>
      } @else if (loadError()) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-16 text-center">
          <p class="text-danger">Something went wrong. Try again.</p>
          <button type="button" class="btn-secondary" (click)="load()">Try again</button>
        </div>
      } @else {
        <div class="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          @for (item of filtered(); track item.flight.id) {
            @let f = item.flight;
            @let dimmed = f.state === 'ComingSoon' || f.state === 'Completed' || f.state === 'Expired' || (f.state === 'Taken' && !item.isMine);
            <article class="card-hover flex flex-col overflow-hidden rounded-xl border bg-surface"
              [class]="item.isMine && f.state === 'Taken' ? 'border-accent ring-2 ring-accent/30' : 'border-line'"
              [class.opacity-60]="dimmed">
              <div class="relative h-44 bg-cover bg-center" [style.background-image]="cover(f)">
                @if (!(f.photoUrl ?? f.aircraftPhotoUrl)) {
                  <i class="ti ti-plane absolute inset-0 m-auto h-fit w-fit text-5xl text-white/40" aria-hidden="true"></i>
                }
                <span class="absolute left-1/2 top-3 -translate-x-1/2 rounded-md px-3 py-1 text-xs font-bold shadow" [class]="state[f.state].class">
                  {{ state[f.state].label }}
                </span>
                @if (f.state === 'ComingSoon') {
                  <span class="absolute bottom-3 left-1/2 -translate-x-1/2 rounded-md bg-black/60 px-3 py-1 font-mono text-xs text-white">
                    Opens in {{ countdown(f.startsAt) }}
                  </span>
                }
                @if (f.photoCredit) {
                  <span class="absolute bottom-1 right-2 max-w-[60%] truncate text-[10px] text-white/80 [text-shadow:0_1px_2px_rgba(0,0,0,.8)]">© {{ f.photoCredit }}</span>
                }
              </div>

              <div class="flex flex-1 flex-col gap-4 p-5">
                <div class="flex items-center justify-between gap-2">
                  <span class="rounded-md bg-sol-500/15 px-2.5 py-1 text-xs font-semibold text-sol-600 dark:text-sol-300">{{ typeLabel[f.type] }}</span>
                  <span class="text-xl leading-none tracking-tight" [title]="'Difficulty ' + f.difficulty + '/5'">
                    @for (star of stars; track star) {
                      <span [class]="star <= f.difficulty ? 'text-sol-500' : 'text-line'">★</span>
                    }
                  </span>
                </div>

                <div>
                  <h2 class="text-lg font-semibold text-ink">{{ f.title }}</h2>
                  <p class="font-mono text-sm text-ink-muted">{{ route(f) }} · {{ f.registration }} ({{ f.aircraftTypeIcao }})</p>
                </div>

                <div class="grid grid-cols-2 gap-3 text-sm">
                  <div class="flex items-center gap-2.5">
                    <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-surface-alt text-lg text-sol-500"><i class="ti ti-calendar-check" aria-hidden="true"></i></span>
                    <span><span class="block text-xs text-ink-muted">Start (UTC)</span><span class="font-medium text-ink">{{ f.startsAt | date: 'dd/MM HH:mm' : 'UTC' }}</span></span>
                  </div>
                  <div class="flex items-center gap-2.5">
                    <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-surface-alt text-lg text-sol-500"><i class="ti ti-calendar-x" aria-hidden="true"></i></span>
                    <span><span class="block text-xs text-ink-muted">End (UTC)</span><span class="font-medium text-ink">{{ f.endsAt | date: 'dd/MM HH:mm' : 'UTC' }}</span></span>
                  </div>
                  <div class="flex items-center gap-2.5">
                    <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-surface-alt">
                      <img [src]="'images/ranks/' + f.minimumRankCode.toLowerCase() + '.png'" alt="" class="h-5 w-auto" />
                    </span>
                    <span><span class="block text-xs text-ink-muted">Minimum rank</span><span class="font-medium text-ink">{{ f.minimumRankName }}</span></span>
                  </div>
                  <div class="flex items-center gap-2.5">
                    <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-surface-alt text-lg text-sol-500"><i class="ti ti-map-pin" aria-hidden="true"></i></span>
                    <span><span class="block text-xs text-ink-muted">Departure</span><span class="font-mono font-medium text-ink">{{ f.legs[0]?.departureIcao }}</span></span>
                  </div>
                </div>

                @if (f.description) {
                  <p class="text-sm text-ink-muted">{{ f.description }}</p>
                }

                @if (f.legs.length > 1 || item.isMine) {
                  <ol class="flex flex-col gap-1.5 text-sm">
                    @for (leg of f.legs; track leg.order) {
                      <li class="flex items-center gap-2">
                        <i class="ti text-base" aria-hidden="true"
                          [class]="leg.completedAt ? 'ti-circle-check text-onda-500' : leg.bookingStatus === 'Booked' || leg.bookingStatus === 'InProgress' ? 'ti-plane-inflight text-primary' : 'ti-circle text-ink-muted'"></i>
                        <span class="font-mono font-semibold text-ink">{{ leg.callsign }}</span>
                        <span class="font-mono text-ink-muted">{{ leg.departureIcao }} → {{ leg.arrivalIcao }}</span>
                        <span class="ml-auto text-xs text-ink-muted">{{ leg.distanceNm }} nm · {{ leg.isInternational ? '+500' : '+300' }}</span>
                      </li>
                    }
                  </ol>
                }

                <div class="mt-auto border-t border-line pt-4">
                  @if (f.state === 'Completed') {
                    <p class="flex items-center gap-2 text-sm font-medium text-onda-500"><i class="ti ti-checks" aria-hidden="true"></i> Completed{{ f.pilotDisplayId ? ' by ' + f.pilotDisplayId : '' }}</p>
                  } @else if (f.state === 'Expired') {
                    <p class="flex items-center gap-2 text-sm font-medium text-danger"><i class="ti ti-clock-x" aria-hidden="true"></i> Expired</p>
                  } @else if (f.state === 'ComingSoon') {
                    <p class="flex items-center gap-2 text-sm text-ink-muted"><i class="ti ti-lock" aria-hidden="true"></i> Available from {{ f.startsAt | date: 'dd/MM HH:mm' : 'UTC' }} UTC</p>
                  } @else if (item.isMine) {
                    @if (item.canBookNextLeg) {
                      <button type="button" class="btn-primary w-full py-2.5" [disabled]="busy() === f.id" (click)="bookNext(item)">
                        {{ busy() === f.id ? 'Booking...' : 'Book leg ' + item.nextLegOrder }}
                      </button>
                    } @else if (item.blockedReason) {
                      <p class="flex items-center gap-2 text-sm text-danger"><i class="ti ti-alert-circle" aria-hidden="true"></i> {{ item.blockedReason }}</p>
                    } @else {
                      <a routerLink="/briefing" class="btn-primary w-full py-2.5">
                        <i class="ti ti-file-text mr-1.5" aria-hidden="true"></i> Leg {{ item.nextLegOrder }} booked · Go to briefing
                      </a>
                    }
                  } @else if (f.state === 'Taken') {
                    <p class="flex items-center gap-2 text-sm text-ink-muted">
                      <i class="ti ti-user-check" aria-hidden="true"></i> Taken by
                      @if (f.pilotNumber) {
                        <a [routerLink]="['/profile', f.pilotNumber]" class="font-medium text-primary hover:underline">{{ f.pilotDisplayId }} · {{ f.pilotName }}</a>
                      }
                    </p>
                  } @else if (confirming() === f.id) {
                    <div class="flex flex-col gap-2">
                      <p class="text-sm text-ink">Take this flight? If it's not finished by the end, or you cancel it, you lose 500 points.</p>
                      <div class="flex gap-2">
                        <button type="button" class="btn-secondary flex-1" [disabled]="busy() === f.id" (click)="confirming.set(null)">Cancel</button>
                        <button type="button" class="btn-primary flex-1" [disabled]="busy() === f.id" (click)="take(item)">{{ busy() === f.id ? 'Taking...' : 'Confirm' }}</button>
                      </div>
                    </div>
                  } @else if (item.canTake) {
                    <button type="button" class="btn-primary w-full py-2.5" (click)="confirming.set(f.id)">
                      <i class="ti ti-hand-grab mr-1.5" aria-hidden="true"></i> Take flight
                    </button>
                  } @else {
                    <p class="flex items-center gap-2 text-sm text-ink-muted"><i class="ti ti-lock" aria-hidden="true"></i> {{ item.blockedReason }}</p>
                  }
                </div>
              </div>
            </article>
          } @empty {
            <div class="panel col-span-full flex flex-col items-center gap-3 px-6 py-16 text-center">
              <i class="ti ti-calendar-off text-4xl text-ink-muted" aria-hidden="true"></i>
              <p class="text-ink-muted">{{ filter() === 'history' ? 'No finished special flights in the last 30 days.' : filter() === 'mine' ? "You don't have a special flight." : 'No special flights right now. Check back soon.' }}</p>
            </div>
          }
        </div>
      }
    </section>
  `
})
export class SpecialFlights implements OnInit, OnDestroy {
  private readonly api = inject(SpecialFlightsService);

  protected readonly filters: { value: Filter; label: string }[] = [
    { value: 'open', label: 'Open' },
    { value: 'mine', label: 'Mine' },
    { value: 'history', label: 'History' }
  ];
  protected readonly state = SPECIAL_FLIGHT_STATE;
  protected readonly typeLabel = SPECIAL_FLIGHT_TYPE_LABEL;
  protected readonly route = specialFlightRoute;
  protected readonly stars = [1, 2, 3, 4, 5];

  protected readonly items = signal<PilotSpecialFlight[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly filter = signal<Filter>('open');
  protected readonly confirming = signal<string | null>(null);
  protected readonly busy = signal<string | null>(null);
  protected readonly message = signal<{ ok: boolean; text: string } | null>(null);
  protected readonly now = signal(Date.now());

  private timer?: ReturnType<typeof setInterval>;

  protected readonly filtered = computed(() => {
    const filter = this.filter();
    return this.items().filter(i =>
      filter === 'mine' ? i.isMine
        : filter === 'history' ? !OPEN_STATES.includes(i.flight.state)
        : OPEN_STATES.includes(i.flight.state));
  });

  ngOnInit(): void {
    this.load();
    this.timer = setInterval(() => {
      this.now.set(Date.now());
      const due = this.items().some(i => i.flight.state === 'ComingSoon' && new Date(i.flight.startsAt).getTime() <= Date.now());
      if (due) this.load();
    }, 30_000);
  }

  ngOnDestroy(): void {
    clearInterval(this.timer);
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);
    try {
      this.items.set(await this.api.list());
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected async take(item: PilotSpecialFlight): Promise<void> {
    await this.run(item, () => this.api.take(item.flight.id), `You took ${item.flight.title}. Leg 1 is booked.`);
  }

  protected async bookNext(item: PilotSpecialFlight): Promise<void> {
    await this.run(item, () => this.api.bookNextLeg(item.flight.id), `Leg ${item.nextLegOrder} of ${item.flight.title} is booked.`);
  }

  protected cover(flight: SpecialFlight): string {
    const gradient = 'linear-gradient(135deg, #0F3768 0%, #1D6FD8 60%, #2EC4B6 100%)';
    const url = flight.photoUrl ?? flight.aircraftPhotoUrl;
    return url ? `url('${url}'), ${gradient}` : gradient;
  }

  protected countdown(iso: string): string {
    const minutes = Math.max(0, Math.round((new Date(iso).getTime() - this.now()) / 60_000));
    const days = Math.floor(minutes / 1440);
    const hours = Math.floor((minutes % 1440) / 60);
    const mins = minutes % 60;
    return days > 0 ? `${days}d ${hours}h` : hours > 0 ? `${hours}h ${mins}m` : `${mins}m`;
  }

  private async run(item: PilotSpecialFlight, action: () => Promise<PilotSpecialFlight>, success: string): Promise<void> {
    this.busy.set(item.flight.id);
    this.message.set(null);
    try {
      await action();
      this.message.set({ ok: true, text: success });
      this.confirming.set(null);
      await this.load();
    } catch (error) {
      this.message.set({ ok: false, text: apiErrorMessage(error) ?? 'Something went wrong. Try again.' });
    } finally {
      this.busy.set(null);
    }
  }
}