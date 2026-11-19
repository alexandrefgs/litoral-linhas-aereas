import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { PilotTour, TOUR_STATE } from '../../core/tours/tours.models';
import { formatMinutes } from '../../shared/utils/flight';

@Component({
  selector: 'app-tour-detail-modal',
  imports: [DatePipe, DecimalPipe],
  host: { '(document:keydown.escape)': 'closed.emit()' },
  template: `
    @let t = tour();
    <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/60 p-4" (click)="closed.emit()">
      <div class="panel flex max-h-[92vh] w-full max-w-3xl flex-col overflow-hidden shadow-2xl" (click)="$event.stopPropagation()">
        <div class="relative h-40 shrink-0 bg-gradient-to-br from-atlantico-700 to-onda-500 bg-cover bg-center"
          [style.background-image]="t.imageUrl ? 'url(' + t.imageUrl + ')' : null">
          <div class="absolute inset-0 bg-gradient-to-t from-black/70 to-transparent"></div>
          <button type="button" class="absolute right-3 top-3 flex h-9 w-9 items-center justify-center rounded-full bg-black/40 text-xl text-white hover:bg-black/60"
            aria-label="Close" (click)="closed.emit()">
            <i class="ti ti-x" aria-hidden="true"></i>
          </button>
          <div class="absolute bottom-4 left-6 right-6 text-white">
            <span class="rounded-md px-2.5 py-1 text-xs font-bold" [class]="state[t.state].class">{{ state[t.state].label }}</span>
            <h2 class="mt-2 text-2xl font-bold">{{ t.title }}</h2>
          </div>
        </div>

        <div class="flex flex-col gap-5 overflow-y-auto px-6 py-5">
          <p class="whitespace-pre-line text-sm leading-relaxed text-ink">{{ t.description }}</p>

          <div class="grid grid-cols-2 gap-3 text-sm sm:grid-cols-4">
            <div class="rounded-lg bg-surface-alt px-3 py-2">
              <p class="text-xs text-ink-muted">Legs</p>
              <p class="font-bold text-ink">{{ t.legs.length }} · {{ t.totalDistanceNm | number }} nm</p>
            </div>
            <div class="rounded-lg bg-surface-alt px-3 py-2">
              <p class="text-xs text-ink-muted">{{ t.isCargo ? 'Cargo aircraft' : 'Aircraft' }}</p>
              <p class="font-mono font-bold text-ink">{{ t.aircraftTypes.join(', ') }}</p>
            </div>
            <div class="rounded-lg bg-surface-alt px-3 py-2">
              <p class="text-xs text-ink-muted">Minimum rank</p>
              <p class="font-bold text-ink">{{ t.minimumRankName }}</p>
            </div>
            <div class="rounded-lg bg-surface-alt px-3 py-2">
              <p class="text-xs text-ink-muted">Window (UTC)</p>
              <p class="font-bold text-ink">
                {{ t.startsAt ? (t.startsAt | date: 'dd/MM/yy' : 'UTC') : 'Open' }}{{ t.endsAt ? ' – ' + (t.endsAt | date: 'dd/MM/yy' : 'UTC') : '' }}
              </p>
            </div>
          </div>

          <div class="flex items-center gap-4 rounded-xl border border-line p-4">
            <span class="flex h-16 w-16 shrink-0 items-center justify-center overflow-hidden rounded-full bg-gradient-to-br from-sol-200 to-sol-400">
              @if (t.award.imageUrl) {
                <img [src]="t.award.imageUrl" alt="" class="h-full w-full object-cover" />
              } @else {
                <i class="ti ti-route text-3xl text-atlantico-900" aria-hidden="true"></i>
              }
            </span>
            <div>
              <p class="text-xs font-semibold uppercase tracking-wide text-ink-muted">Reward</p>
              <p class="font-bold text-ink">{{ t.award.name }}{{ t.bonusPoints > 0 ? ' + ' + (t.bonusPoints | number) + ' points' : '' }}</p>
              <p class="text-xs text-ink-muted">Each leg also scores like a normal flight. {{ t.pilotsCompleted }} {{ t.pilotsCompleted === 1 ? 'pilot has' : 'pilots have' }} completed it.</p>
            </div>
          </div>

          <ol class="flex flex-col gap-1">
            @for (leg of t.legs; track leg.order) {
              @let status = legStatus(leg.order);
              <li class="flex items-center gap-3 rounded-lg px-3 py-2" [class]="status === 'next' ? 'bg-primary/10' : ''">
                <i class="ti text-lg" aria-hidden="true" [class]="icon(status)"></i>
                <span class="w-10 text-xs font-semibold text-ink-muted">#{{ leg.order }}</span>
                <span class="font-mono font-semibold text-ink">{{ leg.departure.icao }} → {{ leg.arrival.icao }}</span>
                <span class="hidden min-w-0 flex-1 truncate text-sm text-ink-muted sm:block">{{ leg.departure.city }} → {{ leg.arrival.city }}</span>
                <span class="ml-auto text-xs text-ink-muted">{{ leg.distanceNm }} nm · {{ duration(leg.estimatedMinutes) }}</span>
              </li>
            }
          </ol>
        </div>

        <div class="flex flex-wrap items-center justify-between gap-3 border-t border-line px-6 py-4">
          <p class="text-sm" [class]="t.progress?.completedAt ? 'text-onda-600 font-semibold' : 'text-ink-muted'">
            @if (t.progress?.completedAt) {
              <i class="ti ti-circle-check" aria-hidden="true"></i> Completed on {{ t.progress!.completedAt | date: 'd MMM y' }}
            } @else if (t.blockedReason) {
              {{ t.blockedReason }}
            } @else if (t.nextLegOrder) {
              Next: leg {{ t.nextLegOrder }} of {{ t.legs.length }}
            }
          </p>
          @if (!t.progress?.completedAt) {
            <button type="button" class="btn-primary flex items-center gap-2" [disabled]="!t.canBook" (click)="book.emit()">
              <i class="ti ti-ticket" aria-hidden="true"></i>Book leg {{ t.nextLegOrder }}
            </button>
          }
        </div>
      </div>
    </div>
  `
})
export class TourDetailModal {
  readonly tour = input.required<PilotTour>();
  readonly book = output<void>();
  readonly closed = output<void>();

  protected readonly state = TOUR_STATE;

  private readonly done = computed(() => this.tour().progress?.completedLegs ?? 0);

  protected legStatus(order: number): 'done' | 'active' | 'next' | 'todo' {
    if (order <= this.done()) return 'done';
    if (order === this.tour().nextLegOrder) return this.tour().progress?.currentLegState ? 'active' : 'next';
    return 'todo';
  }

  protected icon(status: string): string {
    return status === 'done' ? 'ti-circle-check text-onda-500'
      : status === 'active' ? 'ti-plane-inflight text-primary'
      : status === 'next' ? 'ti-circle-dot text-primary'
      : 'ti-circle text-ink-muted';
  }

  protected duration(minutes: number): string {
    return formatMinutes(minutes);
  }
}