import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { FlightMap, PagedResult, ProfileFlight, ProfileFlightsQuery } from '../../core/profile/profile.models';
import { ProfileService } from '../../core/profile/profile.service';
import { FlightsMap } from './flights-map';

@Component({
  selector: 'app-profile-flights',
  imports: [DatePipe, FormsModule, FlightsMap],
  template: `
    <div class="flex flex-col gap-6">
      <div class="panel overflow-hidden">
        <div class="border-b border-line px-6 py-4">
          <h2 class="text-lg font-semibold text-ink">Map of all flights</h2>
        </div>
        @if (map(); as data) {
          <app-flights-map [data]="data" class="h-[28rem]" />
        } @else if (mapError()) {
          <p class="px-6 py-16 text-center text-danger">Could not load the map.</p>
        } @else {
          <p class="px-6 py-16 text-center text-ink-muted">Loading...</p>
        }
      </div>

      <div class="panel">
        <div class="flex flex-col gap-4 border-b border-line px-6 py-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 class="text-lg font-semibold text-ink">All flights</h2>
            <p class="text-xs text-ink-muted">Click <strong>Flight time</strong> to see the longest flights first.</p>
          </div>
          <div class="flex items-center gap-3">
            <label class="flex items-center gap-2 text-sm text-ink-muted">
              Show
              <select class="field-input w-auto py-1.5" [ngModel]="query().pageSize" (ngModelChange)="update({ pageSize: +$event, page: 1 })">
                @for (size of pageSizes; track size) { <option [ngValue]="size">{{ size }}</option> }
              </select>
            </label>
            <div class="relative">
              <i class="ti ti-search pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-ink-muted" aria-hidden="true"></i>
              <input type="search" class="field-input w-56 py-1.5 pl-9" placeholder="Search flights"
                [ngModel]="search()" (ngModelChange)="onSearch($event)" />
            </div>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead class="border-b border-line text-xs uppercase tracking-wide text-ink-muted">
              <tr>
                <th class="px-4 py-3 font-semibold">Callsign</th>
                <th class="px-4 py-3 font-semibold">Dep – Arr</th>
                <th class="px-4 py-3 font-semibold">Aircraft</th>
                <th class="px-4 py-3 font-semibold">
                  <button type="button" class="flex items-center gap-1 uppercase hover:text-ink" (click)="toggleSort()">
                    Flight time
                    <i class="ti" [class]="query().sort === 'time' ? 'ti ti-sort-descending text-primary' : 'ti ti-arrows-sort'" aria-hidden="true"></i>
                  </button>
                </th>
                <th class="px-4 py-3 font-semibold">Points</th>
                <th class="px-4 py-3 font-semibold">
                  <button type="button" class="flex items-center gap-1 uppercase hover:text-ink" (click)="update({ sort: 'date', page: 1 })">
                    Date
                    <i class="ti" [class]="query().sort === 'date' ? 'ti ti-sort-descending text-primary' : 'ti ti-arrows-sort'" aria-hidden="true"></i>
                  </button>
                </th>
              </tr>
            </thead>
            <tbody>
              @if (loading() && !result()) {
                <tr><td colspan="6" class="px-4 py-12 text-center text-ink-muted">Loading...</td></tr>
              } @else if (error()) {
                <tr><td colspan="6" class="px-4 py-12 text-center text-danger">Something went wrong. Try again.</td></tr>
              } @else if (result()?.items?.length === 0) {
                <tr><td colspan="6" class="px-4 py-12 text-center text-ink-muted">{{ search() ? 'No flights match your search.' : 'No flights yet.' }}</td></tr>
              } @else {
                @for (f of result()?.items ?? []; track f.id) {
                  <tr class="border-b border-line last:border-0" [class]="isOwner() ? 'cursor-pointer hover:bg-surface-alt/50' : ''" (click)="open(f)">
                    <td class="px-4 py-3 font-mono font-bold text-primary">{{ f.callsign }}</td>
                    <td class="px-4 py-3 font-mono text-ink">{{ f.departureIcao }} – {{ f.arrivalIcao }}</td>
                    <td class="px-4 py-3 text-ink">{{ f.aircraftTypeIcao }} <span class="text-ink-muted">({{ f.registration }})</span></td>
                    <td class="px-4 py-3 font-mono text-ink">{{ hhmm(f.blockMinutes) }}</td>
                    <td class="px-4 py-3 font-semibold" [class]="f.isRejected || f.points < 0 ? 'text-danger' : 'text-onda-500'">
                      {{ f.isRejected ? 'Rejected' : f.points }}
                    </td>
                    <td class="px-4 py-3 text-ink-muted">{{ f.date | date: 'dd/MM/yyyy' }}</td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>

        @if (result(); as r) {
          @if (r.total > 0) {
            <div class="flex flex-col gap-3 border-t border-line px-6 py-4 text-sm sm:flex-row sm:items-center sm:justify-between">
              <p class="text-ink-muted">Showing {{ firstItem() }} to {{ lastItem() }} of {{ r.total }} flights</p>
              <div class="flex items-center gap-1">
                <button type="button" class="btn-secondary px-3 py-1.5" [disabled]="r.page === 1" (click)="update({ page: r.page - 1 })">Previous</button>
                @for (p of pages(); track $index) {
                  @if (p === null) {
                    <span class="px-2 text-ink-muted">…</span>
                  } @else {
                    <button type="button" class="h-8 min-w-8 rounded-md px-2 text-sm font-medium"
                      [class]="p === r.page ? 'bg-primary text-on-primary' : 'text-ink hover:bg-surface-alt'"
                      (click)="update({ page: p })">{{ p }}</button>
                  }
                }
                <button type="button" class="btn-secondary px-3 py-1.5" [disabled]="r.page >= totalPages()" (click)="update({ page: r.page + 1 })">Next</button>
              </div>
            </div>
          }
        }
      </div>
    </div>
  `
})
export class ProfileFlights implements OnInit {
  readonly pilotNumber = input.required<number>();
  readonly isOwner = input(false);

  private readonly api = inject(ProfileService);
  private readonly router = inject(Router);
  private searchTimer?: ReturnType<typeof setTimeout>;
  private requestId = 0;

  protected readonly pageSizes = [10, 25, 50];
  protected readonly map = signal<FlightMap | null>(null);
  protected readonly mapError = signal(false);
  protected readonly result = signal<PagedResult<ProfileFlight> | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal(false);
  protected readonly search = signal('');
  protected readonly query = signal<ProfileFlightsQuery>({ page: 1, pageSize: 10, search: '', sort: 'date' });

  protected readonly totalPages = computed(() => {
    const r = this.result();
    return r ? Math.max(1, Math.ceil(r.total / r.pageSize)) : 1;
  });

  protected readonly firstItem = computed(() => {
    const r = this.result();
    return r ? (r.page - 1) * r.pageSize + 1 : 0;
  });

  protected readonly lastItem = computed(() => {
    const r = this.result();
    return r ? Math.min(r.total, r.page * r.pageSize) : 0;
  });

  protected readonly pages = computed<(number | null)[]>(() => {
    const total = this.totalPages();
    const current = this.result()?.page ?? 1;
    if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);

    const set = new Set([1, total, current - 1, current, current + 1].filter(p => p >= 1 && p <= total));
    const sorted = [...set].sort((a, b) => a - b);
    const result: (number | null)[] = [];
    sorted.forEach((p, i) => {
      if (i > 0 && p - sorted[i - 1] > 1) result.push(null);
      result.push(p);
    });
    return result;
  });

  async ngOnInit(): Promise<void> {
    this.load();
    try {
      this.map.set(await this.api.map(this.pilotNumber()));
    } catch {
      this.mapError.set(true);
    }
  }

  protected update(change: Partial<ProfileFlightsQuery>): void {
    this.query.update(q => ({ ...q, ...change }));
    this.load();
  }

  protected toggleSort(): void {
    this.update({ sort: this.query().sort === 'time' ? 'date' : 'time', page: 1 });
  }

  protected onSearch(value: string): void {
    this.search.set(value);
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.update({ search: value, page: 1 }), 350);
  }

  protected open(flight: ProfileFlight): void {
    if (this.isOwner()) this.router.navigate(['/pireps', flight.id]);
  }

  protected hhmm(minutes: number): string {
    return `${Math.floor(minutes / 60).toString().padStart(2, '0')}:${(minutes % 60).toString().padStart(2, '0')}`;
  }

  private async load(): Promise<void> {
    const id = ++this.requestId;
    this.loading.set(true);
    this.error.set(false);
    try {
      const result = await this.api.flights(this.pilotNumber(), this.query());
      if (id === this.requestId) this.result.set(result);
    } catch {
      if (id === this.requestId) this.error.set(true);
    } finally {
      if (id === this.requestId) this.loading.set(false);
    }
  }
}