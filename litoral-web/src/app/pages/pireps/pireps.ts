import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NETWORK_LABELS, PIREP_STATUS_CLASS, PirepSummary } from '../../core/pirep/pirep.models';
import { PirepService } from '../../core/pirep/pirep.service';
import { formatMinutes } from '../../shared/utils/flight';

@Component({
  selector: 'app-pireps',
  imports: [DatePipe, RouterLink],
  template: `
    <section class="mx-auto flex max-w-7xl flex-col gap-6 px-6 py-8">
      <div>
        <h1 class="text-2xl font-bold text-ink">PIREPs</h1>
        <p class="text-ink-muted">Your flight reports. Every report is reviewed by the Safety team.</p>
      </div>

      <div class="panel overflow-x-auto">
        @if (loading()) {
          <p class="px-6 py-12 text-center text-ink-muted">Loading...</p>
        } @else if (loadError()) {
          <div class="flex flex-col items-center gap-3 px-6 py-12 text-center">
            <p class="text-danger">Something went wrong. Try again.</p>
            <button type="button" class="btn-secondary" (click)="load()">Try again</button>
          </div>
        } @else if (pireps().length === 0) {
          <div class="flex flex-col items-center gap-3 px-6 py-12 text-center">
            <i class="ti ti-clipboard-off text-4xl text-ink-muted" aria-hidden="true"></i>
            <p class="text-ink-muted">You haven't filed any PIREP yet.</p>
            <a routerLink="/book" class="btn-primary">Book flight</a>
          </div>
        } @else {
          <table class="w-full text-left text-sm">
            <thead class="border-b border-line text-xs uppercase tracking-wide text-ink-muted">
              <tr>
                <th class="px-4 py-3 font-semibold">Date</th>
                <th class="px-4 py-3 font-semibold">Flight</th>
                <th class="px-4 py-3 font-semibold">Route</th>
                <th class="px-4 py-3 font-semibold">Aircraft</th>
                <th class="px-4 py-3 font-semibold">Block</th>
                <th class="px-4 py-3 font-semibold">Landing</th>
                <th class="px-4 py-3 font-semibold">Points</th>
                <th class="px-4 py-3 font-semibold">Status</th>
              </tr>
            </thead>
            <tbody>
              @for (p of pireps(); track p.id) {
                <tr class="cursor-pointer border-b border-line last:border-0 hover:bg-surface-alt/50" [routerLink]="['/pireps', p.id]">
                  <td class="px-4 py-3 text-ink-muted">{{ p.submittedAt | date: 'd MMM yyyy, HH:mm' }}</td>
                  <td class="px-4 py-3">
                    <p class="font-mono font-bold text-ink">{{ p.callsign }}</p>
                    <p class="text-xs text-ink-muted">{{ networks[p.network] }} · {{ p.source === 'Manual' ? 'Manual' : 'ACARS' }}</p>
                  </td>
                  <td class="px-4 py-3 font-mono text-ink">
                    {{ p.departureIcao }} → {{ p.arrivalIcao }}
                    @if (p.arrivalIcao !== p.plannedArrivalIcao) {
                      <span class="ml-1 font-sans text-xs text-danger">diverted</span>
                    }
                  </td>
                  <td class="px-4 py-3 text-ink">{{ p.registration }} <span class="text-ink-muted">{{ p.aircraftTypeIcao }}</span></td>
                  <td class="px-4 py-3 text-ink">{{ format(p.blockMinutes) }}</td>
                  <td class="px-4 py-3 text-ink">{{ p.landingRateFpm !== null ? p.landingRateFpm + ' fpm' : '—' }}</td>
                  <td class="px-4 py-3 font-semibold" [class]="p.hasRejection ? 'text-danger' : 'text-ink'">
                    {{ p.hasRejection ? 'Rejection' : p.scorePoints }}
                  </td>
                  <td class="px-4 py-3">
                    <span class="rounded-full px-2.5 py-1 text-xs font-semibold" [class]="statusClass[p.status]">{{ p.status }}</span>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        }
      </div>
    </section>
  `
})
export class Pireps implements OnInit {
  private readonly api = inject(PirepService);

  protected readonly format = formatMinutes;
  protected readonly networks = NETWORK_LABELS;
  protected readonly statusClass = PIREP_STATUS_CLASS;

  protected readonly pireps = signal<PirepSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);

    try {
      this.pireps.set(await this.api.list());
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }
}