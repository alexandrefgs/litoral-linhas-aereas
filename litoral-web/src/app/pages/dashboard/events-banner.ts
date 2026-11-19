import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PilotEvent } from '../../core/events/events.models';
import { EventsService } from '../../core/events/events.service';

const UPCOMING_DAYS = 7;

@Component({
  selector: 'app-events-banner',
  imports: [DatePipe, DecimalPipe, RouterLink],
  template: `
    @if (featured(); as e) {
      <a routerLink="/tours" [queryParams]="{ tab: 'events' }"
        class="relative flex flex-wrap items-center justify-between gap-4 overflow-hidden rounded-xl bg-atlantico-900 bg-cover bg-center px-5 py-4 text-white"
        [style.background-image]="e.imageUrl ? 'linear-gradient(90deg, rgba(10,31,60,.95) 30%, rgba(10,31,60,.55)), url(' + e.imageUrl + ')' : null">
        <div class="flex items-center gap-4">
          <span class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl text-2xl" [class]="e.state === 'Live' ? 'bg-danger' : 'bg-sol-300 text-atlantico-900'">
            <i class="ti ti-flag" aria-hidden="true"></i>
          </span>
          <div>
            <p class="text-xs font-semibold uppercase tracking-widest" [class]="e.state === 'Live' ? 'text-red-300' : 'text-sol-300'">
              {{ e.state === 'Live' ? 'Live now' : 'Upcoming event' }}{{ more() > 0 ? ' · +' + more() + ' more' : '' }}
            </p>
            <p class="text-lg font-bold">{{ e.title }}</p>
            <p class="text-sm text-white/80">
              {{ e.startsAt | date: 'EEE dd MMM, HH:mm' : 'UTC' }} – {{ e.endsAt | date: 'HH:mm' : 'UTC' }} UTC
              · <span class="font-mono">{{ e.airports.length ? e.airports.join(', ') : 'Any airport' }}</span>
            </p>
          </div>
        </div>
        @if (e.bonusPoints > 0) {
          <span class="rounded-lg bg-white/15 px-3 py-1.5 text-sm font-bold">+{{ e.bonusPoints | number }} points per flight</span>
        }
      </a>
    }
  `
})
export class EventsBanner implements OnInit {
  private readonly api = inject(EventsService);
  private readonly events = signal<PilotEvent[]>([]);

  private readonly visible = computed(() => {
    const limit = Date.now() + UPCOMING_DAYS * 24 * 60 * 60 * 1000;
    return this.events().filter(e => e.state === 'Live' || (e.state === 'Upcoming' && new Date(e.startsAt).getTime() <= limit));
  });

  protected readonly featured = computed(() => this.visible()[0] ?? null);
  protected readonly more = computed(() => Math.max(0, this.visible().length - 1));

  async ngOnInit(): Promise<void> {
    try {
      this.events.set(await this.api.list());
    } catch {}
  }
}