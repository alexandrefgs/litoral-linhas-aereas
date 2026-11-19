import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { apiErrorMessage } from '../../core/api/api-error';
import { AircraftOptions } from '../../core/booking/booking.models';
import { EVENT_STATE, PilotEvent } from '../../core/events/events.models';
import { EventsService } from '../../core/events/events.service';
import { PilotTour, TOUR_STATE } from '../../core/tours/tours.models';
import { ToursService } from '../../core/tours/tours.service';
import { AircraftPickerModal } from '../book/aircraft-picker-modal';
import { TourDetailModal } from './tour-detail-modal';

type Tab = 'tours' | 'events';

@Component({
  selector: 'app-tours',
  imports: [DatePipe, DecimalPipe, RouterLink, TourDetailModal, AircraftPickerModal],
  template: `
    <section class="mx-auto flex max-w-7xl flex-col gap-6 px-6 py-8">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 class="text-2xl font-bold text-ink">Tours & Events</h1>
          <p class="text-ink-muted">
            {{ tab() === 'tours'
              ? 'Fly every leg in order to earn the tour award and bonus. Each leg counts after the PIREP is approved.'
              : 'Fly to or from the event airports inside the window and earn bonus points on each approved flight.' }}
          </p>
        </div>
        <div class="flex rounded-lg border border-line p-1">
          @for (option of tabs; track option.value) {
            <button type="button" (click)="select(option.value)"
              class="flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium"
              [class]="tab() === option.value ? 'bg-primary text-on-primary' : 'text-ink hover:bg-surface-alt'">
              <i class="ti {{ option.icon }}" aria-hidden="true"></i>{{ option.label }}
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

      @if (loading()) {
        <p class="py-24 text-center text-ink-muted">Loading...</p>
      } @else if (loadError()) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-16 text-center">
          <p class="text-danger">Something went wrong. Try again.</p>
          <button type="button" class="btn-secondary" (click)="load()">Try again</button>
        </div>
      } @else if (tab() === 'tours') {
        @if (tours().length === 0) {
          <div class="panel flex flex-col items-center gap-3 px-6 py-16 text-center">
            <i class="ti ti-route text-4xl text-ink-muted" aria-hidden="true"></i>
            <p class="text-ink-muted">No tours available right now. Stay tuned!</p>
          </div>
        } @else {
          <div class="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            @for (t of tours(); track t.id) {
              @let done = t.progress?.completedLegs ?? 0;
              <article class="card-hover flex cursor-pointer flex-col overflow-hidden rounded-xl border bg-surface"
                [class]="t.progress && !t.progress.completedAt ? 'border-primary ring-2 ring-primary/20' : 'border-line'"
                [class.opacity-60]="t.state !== 'Open' && !t.progress"
                (click)="detail.set(t)">
                <div class="relative h-40 bg-gradient-to-br from-atlantico-700 to-onda-500 bg-cover bg-center"
                  [style.background-image]="t.imageUrl ? 'url(' + t.imageUrl + ')' : null">
                  @if (!t.imageUrl) {
                    <i class="ti ti-route absolute inset-0 m-auto h-fit w-fit text-5xl text-white/40" aria-hidden="true"></i>
                  }
                  <span class="absolute left-3 top-3 rounded-md px-2.5 py-1 text-xs font-bold shadow" [class]="state[t.state].class">{{ state[t.state].label }}</span>
                  <span class="absolute bottom-3 right-3 flex h-12 w-12 items-center justify-center overflow-hidden rounded-full border-2 border-white bg-gradient-to-br from-sol-200 to-sol-400 shadow-lg">
                    @if (t.award.imageUrl) {
                      <img [src]="t.award.imageUrl" alt="" class="h-full w-full object-cover" />
                    } @else {
                      <i class="ti ti-award text-2xl text-atlantico-900" aria-hidden="true"></i>
                    }
                  </span>
                </div>
                <div class="flex flex-1 flex-col gap-3 p-5">
                  <div>
                    <h2 class="text-lg font-semibold text-ink">{{ t.title }}</h2>
                    <p class="font-mono text-xs text-ink-muted">{{ path(t) }}</p>
                  </div>
                  <div class="flex flex-wrap gap-x-4 gap-y-1 text-xs text-ink-muted">
                    <span><i class="ti ti-route" aria-hidden="true"></i> {{ t.legs.length }} legs · {{ t.totalDistanceNm | number }} nm</span>
                    <span><i class="ti {{ t.isCargo ? 'ti-package' : 'ti-plane' }}" aria-hidden="true"></i> {{ t.isCargo ? 'Cargo · ' : '' }}{{ t.aircraftTypes.join(', ') }}</span>
                    @if (t.bonusPoints > 0) {
                      <span><i class="ti ti-star" aria-hidden="true"></i> +{{ t.bonusPoints | number }}</span>
                    }
                    @if (t.endsAt) {
                      <span><i class="ti ti-calendar-x" aria-hidden="true"></i> until {{ t.endsAt | date: 'dd/MM' : 'UTC' }}</span>
                    }
                  </div>
                  <div class="mt-auto">
                    @if (t.progress?.completedAt) {
                      <p class="flex items-center gap-2 text-sm font-semibold text-onda-600"><i class="ti ti-circle-check" aria-hidden="true"></i>Completed</p>
                    } @else {
                      <div class="flex items-center justify-between text-xs text-ink-muted">
                        <span>{{ done }} / {{ t.legs.length }} legs</span>
                        @if (t.progress?.currentLegState === 'PendingReview') {
                          <span class="text-highlight">Waiting for review</span>
                        } @else if (t.progress?.currentLegState) {
                          <span class="text-primary">Leg {{ t.nextLegOrder }} booked</span>
                        }
                      </div>
                      <div class="mt-1 h-2 overflow-hidden rounded-full bg-surface-alt">
                        <div class="h-full rounded-full bg-primary" [style.width.%]="done / t.legs.length * 100"></div>
                      </div>
                    }
                  </div>
                </div>
              </article>
            }
          </div>
        }
      } @else {
        @if (events().length === 0) {
          <div class="panel flex flex-col items-center gap-3 px-6 py-16 text-center">
            <i class="ti ti-flag text-4xl text-ink-muted" aria-hidden="true"></i>
            <p class="text-ink-muted">No events scheduled right now.</p>
          </div>
        } @else {
          <div class="flex flex-col gap-4">
            @for (e of events(); track e.id) {
              <article class="panel flex flex-col overflow-hidden md:flex-row" [class.opacity-60]="e.state === 'Ended'">
                <div class="relative h-40 shrink-0 bg-gradient-to-br from-atlantico-700 to-onda-500 bg-cover bg-center md:h-auto md:w-72"
                  [style.background-image]="e.imageUrl ? 'url(' + e.imageUrl + ')' : null">
                  @if (!e.imageUrl) {
                    <i class="ti ti-flag absolute inset-0 m-auto h-fit w-fit text-5xl text-white/40" aria-hidden="true"></i>
                  }
                  <span class="absolute left-3 top-3 rounded-md px-2.5 py-1 text-xs font-bold shadow" [class]="eventState[e.state].class">{{ eventState[e.state].label }}</span>
                </div>
                <div class="flex flex-1 flex-col gap-3 p-5">
                  <div class="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <h2 class="text-lg font-semibold text-ink">{{ e.title }}</h2>
                      <p class="text-sm text-ink-muted">{{ e.startsAt | date: 'EEE, dd MMM · HH:mm' : 'UTC' }} – {{ e.endsAt | date: 'HH:mm' : 'UTC' }} UTC</p>
                    </div>
                    @if (e.bonusPoints > 0) {
                      <span class="rounded-lg bg-sol-400/25 px-3 py-1.5 text-sm font-bold text-ink">+{{ e.bonusPoints | number }} per flight</span>
                    }
                  </div>
                  <p class="whitespace-pre-line text-sm text-ink">{{ e.description }}</p>
                  <div class="mt-auto flex flex-wrap items-center gap-2 text-xs">
                    @for (a of e.airports; track a) {
                      <span class="rounded bg-surface-alt px-2 py-0.5 font-mono font-semibold text-ink">{{ a }}</span>
                    } @empty {
                      <span class="rounded bg-surface-alt px-2 py-0.5 text-ink-muted">Any airport</span>
                    }
                    <span class="ml-auto text-ink-muted">{{ e.flights }} flights · {{ e.pilots }} pilots{{ e.myFlights ? ' · ' + e.myFlights + ' yours' : '' }}</span>
                  </div>
                </div>
              </article>
            }
          </div>
        }
      }
    </section>

    @if (detail(); as t) {
      <app-tour-detail-modal [tour]="t" (book)="openPicker(t)" (closed)="detail.set(null)" />
    }
    @if (picking(); as t) {
      <app-aircraft-picker-modal [title]="'Leg ' + t.nextLegOrder + ' · ' + legPath(t)" [options]="pickerOptions()" [saving]="saving()" [error]="pickerError()"
        (confirm)="book(t, $event)" (closed)="picking.set(null)" />
    }
  `
})
export class Tours implements OnInit {
  private readonly toursApi = inject(ToursService);
  private readonly eventsApi = inject(EventsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly state = TOUR_STATE;
  protected readonly eventState = EVENT_STATE;
  protected readonly tabs: { value: Tab; label: string; icon: string }[] = [
    { value: 'tours', label: 'Tours', icon: 'ti-route' },
    { value: 'events', label: 'Events', icon: 'ti-flag' }
  ];

  protected readonly tab = signal<Tab>('tours');
  protected readonly tours = signal<PilotTour[]>([]);
  protected readonly events = signal<PilotEvent[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly message = signal<{ ok: boolean; text: string } | null>(null);
  protected readonly detail = signal<PilotTour | null>(null);
  protected readonly picking = signal<PilotTour | null>(null);
  protected readonly pickerOptions = signal<AircraftOptions | null>(null);
  protected readonly pickerError = signal<string | null>(null);
  protected readonly saving = signal(false);

  ngOnInit(): void {
    this.tab.set(this.route.snapshot.queryParamMap.get('tab') === 'events' ? 'events' : 'tours');
    this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);
    try {
      const [tours, events] = await Promise.all([this.toursApi.list(), this.eventsApi.list()]);
      this.tours.set(tours);
      this.events.set(events);
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected select(tab: Tab): void {
    this.tab.set(tab);
    this.router.navigate([], { relativeTo: this.route, queryParams: { tab: tab === 'tours' ? null : tab }, replaceUrl: true });
  }

  protected path(t: PilotTour): string {
    const stops = [t.legs[0]?.departure.icao, ...t.legs.map(l => l.arrival.icao)].filter(Boolean);
    return stops.length > 6 ? `${stops.slice(0, 3).join(' → ')} → … → ${stops[stops.length - 1]}` : stops.join(' → ');
  }

  protected legPath(t: PilotTour): string {
    const leg = t.legs.find(l => l.order === t.nextLegOrder);
    return leg ? `${leg.departure.icao} → ${leg.arrival.icao}` : '';
  }

  protected async openPicker(t: PilotTour): Promise<void> {
    this.detail.set(null);
    this.picking.set(t);
    this.pickerOptions.set(null);
    this.pickerError.set(null);
    try {
      this.pickerOptions.set(await this.toursApi.aircraft(t.id));
    } catch (error) {
      this.pickerError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    }
  }

  protected async book(t: PilotTour, aircraftId: string | null): Promise<void> {
    this.saving.set(true);
    this.pickerError.set(null);
    try {
      const booking = await this.toursApi.book(t.id, aircraftId);
      this.picking.set(null);
      this.message.set({ ok: true, text: `${booking.callsign} booked: leg ${t.nextLegOrder} of ${t.title}.` });
      this.tours.set(await this.toursApi.list());
    } catch (error) {
      this.pickerError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.saving.set(false);
    }
  }
}