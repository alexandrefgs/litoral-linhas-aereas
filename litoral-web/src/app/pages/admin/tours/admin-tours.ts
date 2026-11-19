import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { apiErrorMessage } from '../../../core/api/api-error';
import { AdminTour, TOUR_STATE, TourOptions } from '../../../core/tours/tours.models';
import { ToursService } from '../../../core/tours/tours.service';
import { ConfirmService } from '../../../shared/confirm-dialog/confirm.service';
import { TourFormModal } from './tour-form-modal';

@Component({
  selector: 'app-admin-tours',
  imports: [DatePipe, DecimalPipe, TourFormModal],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-ink">Tours</h1>
          <p class="text-ink-muted">Legs flown in order. Each approved leg scores normally and the last one pays the bonus and the award.</p>
        </div>
        <button type="button" class="btn-primary flex items-center gap-2" [disabled]="!options()" (click)="editing.set({ tour: null })">
          <i class="ti ti-plus" aria-hidden="true"></i>New tour
        </button>
      </div>

      @if (error(); as message) {
        <p class="rounded-lg bg-danger/10 px-4 py-3 text-sm text-danger">{{ message }}</p>
      }

      @if (loading()) {
        <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (tours().length === 0) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-12 text-center">
          <i class="ti ti-route text-4xl text-ink-muted" aria-hidden="true"></i>
          <p class="text-ink-muted">No tours yet.</p>
        </div>
      } @else {
        <div class="grid items-start gap-4 lg:grid-cols-2">
          @for (tour of tours(); track tour.id) {
            <article class="panel overflow-hidden">
              <div class="relative h-28 bg-gradient-to-br from-atlantico-700 to-onda-500 bg-cover bg-center"
                [style.background-image]="tour.imageUrl ? 'url(' + tour.imageUrl + ')' : null">
                <span class="absolute left-3 top-3 rounded-md px-2.5 py-1 text-xs font-bold shadow" [class]="state[tour.state].class">{{ state[tour.state].label }}</span>
              </div>
              <div class="flex flex-col gap-3 p-5">
                <div class="flex items-start justify-between gap-3">
                  <div class="min-w-0">
                    <h3 class="truncate font-bold text-ink">{{ tour.title }}</h3>
                    <p class="font-mono text-xs text-ink-muted">{{ path(tour) }}</p>
                  </div>
                  <div class="flex shrink-0 gap-1">
                    <button type="button" class="icon-btn h-9 w-9 text-lg" title="Edit" aria-label="Edit" (click)="editing.set({ tour })">
                      <i class="ti ti-pencil" aria-hidden="true"></i>
                    </button>
                    <button type="button" class="icon-btn h-9 w-9 text-lg hover:text-danger" title="Delete" aria-label="Delete" (click)="remove(tour)">
                      <i class="ti ti-trash" aria-hidden="true"></i>
                    </button>
                  </div>
                </div>
                <div class="flex flex-wrap gap-x-4 gap-y-1 text-xs text-ink-muted">
                  <span><i class="ti ti-route" aria-hidden="true"></i> {{ tour.legs.length }} legs</span>
                  <span><i class="ti {{ tour.isCargo ? 'ti-package' : 'ti-plane' }}" aria-hidden="true"></i> {{ tour.isCargo ? 'Cargo · ' : '' }}{{ tour.aircraftTypes.join(', ') }}</span>
                  <span><i class="ti ti-user-shield" aria-hidden="true"></i> {{ tour.minimumRankName }}</span>
                  <span><i class="ti ti-star" aria-hidden="true"></i> +{{ tour.bonusPoints | number }}</span>
                  <span><i class="ti ti-calendar" aria-hidden="true"></i>
                    {{ tour.startsAt ? (tour.startsAt | date: 'dd/MM/yy' : 'UTC') : 'Always' }} – {{ tour.endsAt ? (tour.endsAt | date: 'dd/MM/yy' : 'UTC') : 'open' }}
                  </span>
                </div>
                <div class="flex items-center justify-between border-t border-line pt-3 text-sm">
                  <span class="flex items-center gap-2 text-ink-muted">
                    <span class="flex h-7 w-7 items-center justify-center overflow-hidden rounded-full bg-gradient-to-br from-sol-200 to-sol-400">
                      @if (tour.award.imageUrl) {
                        <img [src]="tour.award.imageUrl" alt="" class="h-full w-full object-cover" />
                      } @else {
                        <i class="ti ti-award text-atlantico-900" aria-hidden="true"></i>
                      }
                    </span>
                    {{ tour.award.name }}
                  </span>
                  <span class="text-ink-muted"><b class="text-ink">{{ tour.pilotsStarted }}</b> started · <b class="text-ink">{{ tour.pilotsCompleted }}</b> completed</span>
                </div>
              </div>
            </article>
          }
        </div>
      }
    </div>

    @if (editing(); as state) {
      @if (options(); as opts) {
        <app-tour-form-modal [tour]="state.tour" [options]="opts" (saved)="onSaved($event)" (closed)="editing.set(null)" />
      }
    }
  `
})
export class AdminTours implements OnInit {
  private readonly api = inject(ToursService);
  private readonly confirm = inject(ConfirmService);

  protected readonly state = TOUR_STATE;
  protected readonly tours = signal<AdminTour[]>([]);
  protected readonly options = signal<TourOptions | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly editing = signal<{ tour: AdminTour | null } | null>(null);

  async ngOnInit(): Promise<void> {
    try {
      const [tours, options] = await Promise.all([this.api.adminList(), this.api.options()]);
      this.tours.set(tours);
      this.options.set(options);
    } catch {
      this.error.set('Something went wrong. Reload the page.');
    } finally {
      this.loading.set(false);
    }
  }

  protected path(tour: AdminTour): string {
    if (!tour.legs.length) return '';
    const stops = [tour.legs[0].departure.icao, ...tour.legs.map(l => l.arrival.icao)];
    return stops.length > 7 ? `${stops.slice(0, 3).join(' → ')} → … → ${stops.slice(-2).join(' → ')}` : stops.join(' → ');
  }

  protected onSaved(tour: AdminTour): void {
    this.tours.update(list => (list.some(t => t.id === tour.id) ? list.map(t => (t.id === tour.id ? tour : t)) : [tour, ...list]));
    this.editing.set(null);
  }

  protected async remove(tour: AdminTour): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Delete tour?',
      message: `"${tour.title}", its legs and its award will be deleted. This can't be undone.`,
      confirmLabel: 'Delete',
      tone: 'danger',
      icon: 'ti-trash'
    });
    if (!ok) return;

    this.error.set(null);
    try {
      await this.api.delete(tour.id);
      this.tours.update(list => list.filter(t => t.id !== tour.id));
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    }
  }
}