import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { AdminAirport, AdminAirportsService } from '../../../core/admin/admin-airports.service';
import { apiErrorMessage } from '../../../core/api/api-error';
import { RankInfo } from '../../../core/reference/reference.models';
import { ReferenceService } from '../../../core/reference/reference.service';
import { AirportFormModal } from './airport-form-modal';

type AirportFilter = 'all' | 'hubs' | 'base' | 'restricted';

@Component({
  selector: 'app-admin-airports',
  imports: [AirportFormModal],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-ink">Airports</h1>
          <p class="text-ink-muted">Register airports, set the minimum rank and hubs.</p>
        </div>
        <button type="button" class="btn-primary gap-2 px-4 py-2.5" (click)="openCreate()" [disabled]="loading()">
          <i class="ti ti-plus text-lg" aria-hidden="true"></i>New airport
        </button>
      </div>

      <div class="flex flex-wrap items-center gap-3">
        <div class="relative min-w-60 flex-1">
          <i class="ti ti-search pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-ink-muted" aria-hidden="true"></i>
          <input type="search" class="field-input pl-9" placeholder="Search by ICAO, name or city"
            [value]="search()" (input)="search.set($any($event.target).value)" />
        </div>
        <div class="flex rounded-lg border border-line p-1">
          @for (option of filterOptions(); track option.value) {
            <button type="button" (click)="filter.set(option.value)"
              class="rounded-md px-3 py-1.5 text-sm font-medium"
              [class]="filter() === option.value ? 'bg-primary text-on-primary' : 'text-ink hover:bg-surface-alt'">
              {{ option.label }}
            </button>
          }
        </div>
        <span class="text-sm text-ink-muted">{{ filtered().length }} / {{ airports().length }}</span>
      </div>

      <div class="panel overflow-x-auto">
        @if (loading()) {
          <p class="px-6 py-12 text-center text-ink-muted">Loading...</p>
        } @else if (loadError()) {
          <div class="flex flex-col items-center gap-3 px-6 py-12 text-center">
            <p class="text-danger">Something went wrong. Try again.</p>
            <button type="button" class="btn-secondary" (click)="load()">Try again</button>
          </div>
        } @else if (filtered().length === 0) {
          <p class="px-6 py-12 text-center text-ink-muted">No airports found.</p>
        } @else {
          <table class="w-full text-left text-sm">
            <thead class="border-b border-line text-xs uppercase tracking-wide text-ink-muted">
              <tr>
                <th class="px-4 py-3 font-semibold">ICAO</th>
                <th class="px-4 py-3 font-semibold">Name</th>
                <th class="px-4 py-3 font-semibold">Country</th>
                <th class="px-4 py-3 font-semibold">Minimum rank</th>
                <th class="px-4 py-3 font-semibold">Operation</th>
                <th class="px-4 py-3 font-semibold">Hub</th>
                <th class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody>
              @for (airport of filtered(); track airport.icao) {
                <tr class="border-b border-line last:border-0 hover:bg-surface-alt/50">
                  <td class="px-4 py-3 font-mono font-bold text-ink">{{ airport.icao }}</td>
                  <td class="px-4 py-3">
                    <p class="font-medium text-ink">{{ airport.name }}</p>
                    <p class="text-xs text-ink-muted">{{ airport.city }}</p>
                  </td>
                  <td class="px-4 py-3 text-ink">{{ airport.country }}</td>
                  <td class="px-4 py-3">
                    <span class="rounded-full px-2.5 py-1 text-xs font-bold"
                      [class]="airport.minimumRank === baseRank() ? 'bg-surface-alt text-ink' : 'bg-primary text-on-primary'">
                      {{ airport.minimumRank }}
                    </span>
                  </td>
                  <td class="px-4 py-3">
                    <div class="flex gap-1">
                      @if (airport.passenger) {
                        <span class="rounded-full bg-primary/10 px-2.5 py-1 text-xs font-semibold text-primary">Passenger</span>
                      }
                      @if (airport.cargo) {
                        <span class="rounded-full bg-accent/20 px-2.5 py-1 text-xs font-semibold text-ink">Cargo</span>
                      }
                    </div>
                  </td>
                  <td class="px-4 py-3">
                    @if (airport.isHub) {
                      <span class="inline-flex items-center gap-1 rounded-full bg-highlight px-2.5 py-1 text-xs font-bold text-on-highlight">
                        <i class="ti ti-star-filled text-[11px]" aria-hidden="true"></i>Hub
                      </span>
                    } @else {
                      <span class="text-ink-muted">—</span>
                    }
                  </td>
                  <td class="px-4 py-3">
                    @if (confirmDelete() === airport.icao) {
                      <div class="flex items-center justify-end gap-2">
                        <span class="text-xs text-ink-muted">Delete this airport?</span>
                        <button type="button" class="rounded-md bg-danger px-3 py-1 text-xs font-medium text-white hover:opacity-90"
                          (click)="remove(airport.icao)">Delete</button>
                        <button type="button" class="btn-secondary px-3 py-1 text-xs" (click)="confirmDelete.set(null)">Cancel</button>
                      </div>
                    } @else {
                      <div class="flex justify-end gap-1">
                        <button type="button" class="icon-btn h-9 w-9 text-lg" aria-label="Edit airport" (click)="openEdit(airport)">
                          <i class="ti ti-pencil" aria-hidden="true"></i>
                        </button>
                        <button type="button" class="icon-btn h-9 w-9 text-lg hover:text-danger" aria-label="Delete"
                          (click)="askDelete(airport.icao)">
                          <i class="ti ti-trash" aria-hidden="true"></i>
                        </button>
                      </div>
                    }
                    @if (rowError()?.icao === airport.icao) {
                      <p class="mt-1 text-right text-xs text-danger">{{ rowError()?.message }}</p>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        }
      </div>
    </div>

    @if (modalOpen()) {
      <app-airport-form-modal [airport]="editing()" [registeredIcaos]="registeredIcaos()"
        (saved)="onSaved($event)" (closed)="modalOpen.set(false)" />
    }
  `
})
export class AdminAirports implements OnInit {
  private readonly api = inject(AdminAirportsService);
  private readonly reference = inject(ReferenceService);

  protected readonly airports = signal<AdminAirport[]>([]);
  protected readonly ranks = signal<RankInfo[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly search = signal('');
  protected readonly filter = signal<AirportFilter>('all');

  protected readonly modalOpen = signal(false);
  protected readonly editing = signal<AdminAirport | null>(null);
  protected readonly confirmDelete = signal<string | null>(null);
  protected readonly rowError = signal<{ icao: string; message: string } | null>(null);

  protected readonly registeredIcaos = computed(() => this.airports().map(a => a.icao));
  protected readonly baseRank = computed(() => this.ranks()[0]?.code ?? 'FO');
  protected readonly restrictedRank = computed(() => this.ranks()[1]?.code ?? 'CMD');

  protected readonly filterOptions = computed<{ value: AirportFilter; label: string }[]>(() => [
    { value: 'all', label: 'All' },
    { value: 'hubs', label: 'Hubs' },
    { value: 'base', label: this.baseRank() },
    { value: 'restricted', label: this.restrictedRank() }
  ]);

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const filter = this.filter();
    const base = this.baseRank();

    return this.airports().filter(a => {
      if (filter === 'hubs' && !a.isHub) return false;
      if (filter === 'base' && a.minimumRank !== base) return false;
      if (filter === 'restricted' && a.minimumRank === base) return false;

      return !term
        || a.icao.toLowerCase().includes(term)
        || a.name.toLowerCase().includes(term)
        || a.city.toLowerCase().includes(term);
    });
  });

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);

    try {
      const [airports, ranks] = await Promise.all([this.api.list(), this.reference.getRanks()]);
      this.ranks.set([...ranks].sort((a, b) => a.level - b.level));
      this.airports.set(airports);
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected openCreate(): void {
    this.editing.set(null);
    this.modalOpen.set(true);
  }

  protected openEdit(airport: AdminAirport): void {
    this.editing.set(airport);
    this.modalOpen.set(true);
  }

  protected onSaved(airport: AdminAirport): void {
    this.airports.update(list =>
      [...list.filter(a => a.icao !== airport.icao), airport].sort((a, b) => a.icao.localeCompare(b.icao))
    );
    this.modalOpen.set(false);
  }

  protected askDelete(icao: string): void {
    this.rowError.set(null);
    this.confirmDelete.set(icao);
  }

  protected async remove(icao: string): Promise<void> {
    try {
      await this.api.remove(icao);
      this.airports.update(list => list.filter(a => a.icao !== icao));
      this.confirmDelete.set(null);
    } catch (error) {
      this.confirmDelete.set(null);
      this.rowError.set({ icao, message: apiErrorMessage(error) ?? 'Something went wrong. Try again.' });
    }
  }
}