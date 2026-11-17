import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { hhmm } from '../../core/flights/flight.models';
import { ProfileFlight } from '../../core/profile/profile.models';
import { ProfileService } from '../../core/profile/profile.service';

@Component({
  selector: 'app-my-last-flights',
  imports: [DatePipe, RouterLink],
  host: { class: 'panel card-hover flex flex-col' },
  template: `
    <div class="flex items-center justify-between border-b border-line px-6 py-4">
      <h2 class="text-lg font-bold text-ink">Your last flights</h2>
      @if (flights().length) {
        <a routerLink="/profile" [queryParams]="{ tab: 'flights' }" class="text-sm font-medium text-primary hover:underline">See all</a>
      }
    </div>

    @if (loading()) {
      <p class="px-6 py-10 text-center text-ink-muted">Loading...</p>
    } @else if (!flights().length) {
      <div class="flex flex-1 flex-col items-center justify-center gap-3 px-6 py-10 text-center">
        <i class="ti ti-plane-off text-4xl text-ink-muted" aria-hidden="true"></i>
        <p class="text-ink-muted">You haven't flown with Litoral yet. How about the first one?</p>
        <a routerLink="/book" class="btn-primary"><i class="ti ti-world-search mr-2" aria-hidden="true"></i>Book flight</a>
      </div>
    } @else {
      <div class="overflow-x-auto">
        <table class="w-full text-left text-sm">
          <thead class="text-xs uppercase text-ink-muted">
            <tr class="border-b border-line">
              <th class="px-6 py-3 font-semibold">Flight</th>
              <th class="px-6 py-3 font-semibold">Route</th>
              <th class="px-6 py-3 font-semibold">Aircraft</th>
              <th class="px-6 py-3 font-semibold">Time</th>
              <th class="px-6 py-3 text-right font-semibold">Points</th>
            </tr>
          </thead>
          <tbody>
            @for (f of flights(); track f.id) {
              <tr class="cursor-pointer border-b border-line last:border-0 hover:bg-surface-alt/50" [routerLink]="['/flights', f.id]">
                <td class="px-6 py-3">
                  <p class="font-mono font-bold text-primary">{{ f.callsign }}</p>
                  <p class="text-xs text-ink-muted">{{ f.date | date: 'd MMM' : 'UTC' }}</p>
                </td>
                <td class="px-6 py-3 font-mono text-ink">{{ f.departureIcao }} → {{ f.arrivalIcao }}</td>
                <td class="px-6 py-3 text-ink">{{ f.registration }} <span class="text-ink-muted">· {{ f.aircraftTypeIcao }}</span></td>
                <td class="px-6 py-3 font-mono text-ink">{{ hhmm(f.blockMinutes) }}</td>
                <td class="px-6 py-3 text-right font-semibold" [class]="f.points < 0 ? 'text-danger' : 'text-ink'">{{ f.points }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `
})
export class MyLastFlights implements OnInit {
  private readonly api = inject(ProfileService);

  readonly pilotNumber = input.required<number>();

  protected readonly flights = signal<ProfileFlight[]>([]);
  protected readonly loading = signal(true);
  protected readonly hhmm = hhmm;

  async ngOnInit(): Promise<void> {
    try {
      const page = await this.api.flights(this.pilotNumber(), { page: 1, pageSize: 5, search: '', sort: 'date' });
      this.flights.set(page.items);
    } catch {
      this.flights.set([]);
    } finally {
      this.loading.set(false);
    }
  }
}