import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminPirepsService } from '../../../core/admin/admin-pireps.service';
import { AdminPirepListItem, NETWORK_LABELS, PIREP_STATUS_CLASS, PirepStatus } from '../../../core/pirep/pirep.models';
import { formatMinutes } from '../../../shared/utils/flight';

@Component({
  selector: 'app-admin-pireps',
  imports: [DatePipe, RouterLink],
  template: `
    <div class="flex flex-col gap-6">
      <div>
        <h1 class="text-2xl font-bold text-ink">PIREPs</h1>
        <p class="text-ink-muted">Review every flight report before the points are credited.</p>
      </div>

      <div class="flex gap-1 border-b border-line">
        @for (option of tabs; track option.label) {
          <button type="button" (click)="setTab(option.value)"
            class="-mb-px border-b-2 px-4 py-2.5 text-sm font-semibold"
            [class]="tab() === option.value ? 'border-primary text-primary' : 'border-transparent text-ink-muted hover:text-ink'">
            {{ option.label }}
          </button>
        }
      </div>

      <div class="panel overflow-x-auto">
        @if (loading()) {
          <p class="px-6 py-12 text-center text-ink-muted">Loading...</p>
        } @else if (loadError()) {
          <div class="flex flex-col items-center gap-3 px-6 py-12 text-center">
            <p class="text-danger">Something went wrong. Try again.</p>
            <button type="button" class="btn-secondary" (click)="load()">Try again</button>
          </div>
        } @else if (items().length === 0) {
          <p class="px-6 py-12 text-center text-ink-muted">{{ tab() === 'Pending' ? 'No PIREPs waiting for review.' : 'No PIREPs found.' }}</p>
        } @else {
          <table class="w-full text-left text-sm">
            <thead class="border-b border-line text-xs uppercase tracking-wide text-ink-muted">
              <tr>
                <th class="px-4 py-3 font-semibold">Filed</th>
                <th class="px-4 py-3 font-semibold">Pilot</th>
                <th class="px-4 py-3 font-semibold">Flight</th>
                <th class="px-4 py-3 font-semibold">Route</th>
                <th class="px-4 py-3 font-semibold">Block</th>
                <th class="px-4 py-3 font-semibold">Landing</th>
                <th class="px-4 py-3 font-semibold">Points</th>
                <th class="px-4 py-3 font-semibold">Conformity</th>
                <th class="px-4 py-3 font-semibold">Status</th>
              </tr>
            </thead>
            <tbody>
              @for (item of items(); track item.summary.id) {
                @let p = item.summary;
                <tr class="cursor-pointer border-b border-line last:border-0 hover:bg-surface-alt/50" [routerLink]="['/admin/pireps', p.id]">
                  <td class="px-4 py-3 text-ink-muted">{{ p.submittedAt | date: 'd MMM, HH:mm' }}</td>
                  <td class="px-4 py-3">
                    <p class="text-ink">{{ item.pilotName }}</p>
                    <p class="text-xs text-ink-muted">{{ item.pilotId }}</p>
                  </td>
                  <td class="px-4 py-3">
                    <p class="font-mono font-bold text-ink">{{ p.callsign }}</p>
                    <p class="text-xs text-ink-muted">
                      {{ networks[p.network] }} ·
                      @if (p.source === 'Manual') {
                        <span class="font-semibold text-sol-600">Manual</span>
                      } @else {
                        ACARS
                      }
                    </p>
                  </td>
                  <td class="px-4 py-3 font-mono text-ink">
                    {{ p.departureIcao }} → {{ p.arrivalIcao }}
                    @if (item.isDiversion) {
                      <span class="ml-1 font-sans text-xs text-danger">diverted</span>
                    }
                  </td>
                  <td class="px-4 py-3 text-ink">{{ format(p.blockMinutes) }}</td>
                  <td class="px-4 py-3 text-ink">{{ p.landingRateFpm !== null ? p.landingRateFpm + ' fpm' : '—' }}</td>
                  <td class="px-4 py-3 font-semibold" [class]="p.hasRejection ? 'text-danger' : 'text-ink'">
                    {{ p.hasRejection ? 'Rejection' : p.scorePoints }}
                  </td>
                  <td class="px-4 py-3">
                    <div class="flex items-center gap-2">
                      <div class="h-1.5 w-16 overflow-hidden rounded-full bg-line">
                        <div class="h-full rounded-full" [class]="conformityBar(p.conformity)" [style.width.%]="p.conformity"></div>
                      </div>
                      <span class="font-semibold text-ink">{{ p.conformity }}%</span>
                    </div>
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
    </div>
  `
})
export class AdminPireps implements OnInit {
  private readonly api = inject(AdminPirepsService);

  protected readonly format = formatMinutes;
  protected readonly networks = NETWORK_LABELS;
  protected readonly statusClass = PIREP_STATUS_CLASS;

  protected readonly tabs: { value: PirepStatus | null; label: string }[] = [
    { value: 'Pending', label: 'Pending' },
    { value: 'Approved', label: 'Approved' },
    { value: 'Rejected', label: 'Rejected' },
    { value: null, label: 'All' }
  ];

  protected readonly tab = signal<PirepStatus | null>('Pending');
  protected readonly items = signal<AdminPirepListItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);

  ngOnInit(): void {
    this.load();
  }

  protected setTab(value: PirepStatus | null): void {
    this.tab.set(value);
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);

    try {
      this.items.set(await this.api.list(this.tab()));
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected conformityBar(value: number): string {
    return value >= 98 ? 'bg-onda-500' : value >= 80 ? 'bg-sol-500' : 'bg-danger';
  }
}