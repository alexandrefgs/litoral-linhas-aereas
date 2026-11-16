import { Component, OnInit, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { BookingOverview } from '../../core/booking/booking.models';
import { BookingService } from '../../core/booking/booking.service';
import { CurrentBooking } from './current-booking';
import { CustomFlight } from './custom-flight';
import { MonthlyRoster } from './monthly-roster';
import { RandomFlight } from './random-flight';

type BookTab = 'random' | 'custom' | 'roster';

@Component({
  selector: 'app-book',
  imports: [CurrentBooking, RandomFlight, CustomFlight, MonthlyRoster],
  template: `
    <section class="mx-auto flex max-w-7xl flex-col gap-6 px-6 py-8">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-ink">Book flight</h1>
          @if (overview(); as data) {
            <p class="text-ink-muted">
              You are at <span class="font-mono font-semibold text-ink">{{ data.locationIcao }}</span> · {{ data.locationCity }}
            </p>
          }
        </div>
        @if (overview(); as data) {
          <div class="flex items-center gap-2 rounded-full bg-surface-alt px-4 py-2 text-sm">
            <i class="ti ti-star text-primary" aria-hidden="true"></i>
            <span class="font-semibold text-ink">{{ data.points }}</span><span class="text-ink-muted">points</span>
          </div>
        }
      </div>

      @if (loading()) {
        <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (booking.current(); as current) {
        <app-current-booking [booking]="current" (cancelled)="load()" />
      } @else {
        <div class="flex gap-1 border-b border-line">
          @for (option of tabs; track option.value) {
            <button type="button" (click)="tab.set(option.value)"
              class="-mb-px flex items-center gap-2 border-b-2 px-4 py-2.5 text-sm font-semibold"
              [class]="tab() === option.value ? 'border-primary text-primary' : 'border-transparent text-ink-muted hover:text-ink'">
              <i class="ti {{ option.icon }} text-lg" aria-hidden="true"></i>{{ option.label }}
            </button>
          }
        </div>

        @switch (tab()) {
          @case ('random') { <app-random-flight /> }
          @case ('custom') { <app-custom-flight /> }
          @case ('roster') { <app-monthly-roster /> }
        }
      }
    </section>
  `
})
export class Book implements OnInit {
  protected readonly booking = inject(BookingService);
  private readonly router = inject(Router);

  protected readonly overview = signal<BookingOverview | null>(null);
  protected readonly loading = signal(true);
  protected readonly tab = signal<BookTab>('custom');

  protected readonly tabs: { value: BookTab; label: string; icon: string }[] = [
    { value: 'custom', label: 'Custom flight', icon: 'ti-map-pin' },
    { value: 'random', label: 'Random flight', icon: 'ti-arrows-shuffle' },
    { value: 'roster', label: 'Monthly roster', icon: 'ti-calendar-month' }
  ];

  private waitingForBooking = false;

  constructor() {
    effect(() => {
      if (this.booking.current() && this.waitingForBooking) {
        this.waitingForBooking = false;
        this.router.navigate(['/briefing']);
      }
    });
  }

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    const overview = await this.booking.refreshCurrent();
    this.overview.set(overview);
    this.waitingForBooking = !overview?.current;
    this.loading.set(false);
  }
}