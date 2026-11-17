import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  AdminAircraft,
  AdminAircraftType,
  AdminFleetService,
  AircraftStatus,
  FleetAirport
} from '../../../core/admin/admin-fleet.service';
import { apiErrorMessage } from '../../../core/api/api-error';
import { formatMinutes } from '../../../shared/utils/flight';
import { AircraftFormModal } from './aircraft-form-modal';
import { MaintenanceModal } from './maintenance-modal';

type StatusFilter = 'all' | 'available' | 'inUse' | 'maintenance' | 'due' | 'inactive';

const STATUS_LABELS: Record<AircraftStatus, string> = {
  Available: 'Available',
  Reserved: 'Reserved',
  InFlight: 'In flight',
  Maintenance: 'Maintenance',
  Inactive: 'Inactive'
};

const STATUS_CLASSES: Record<AircraftStatus, string> = {
  Available: 'bg-accent/20 text-ink',
  Reserved: 'bg-primary/10 text-primary',
  InFlight: 'bg-primary text-on-primary',
  Maintenance: 'bg-highlight text-on-highlight',
  Inactive: 'bg-surface-alt text-ink-muted'
};

@Component({
  selector: 'app-fleet-aircraft',
  imports: [DatePipe, AircraftFormModal, MaintenanceModal],
  template: `
    <div class="flex flex-col gap-4">
      <div class="grid gap-3 sm:grid-cols-4">
        @for (card of summary(); track card.label) {
          <div class="panel px-4 py-3">
            <p class="text-xs uppercase tracking-wide text-ink-muted">{{ card.label }}</p>
            <p class="text-2xl font-bold" [class]="card.alert && card.value ? 'text-danger' : 'text-ink'">{{ card.value }}</p>
          </div>
        }
      </div>

      <div class="flex flex-wrap items-center gap-3">
        <div class="relative min-w-60 flex-1">
          <i class="ti ti-search pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-ink-muted" aria-hidden="true"></i>
          <input type="search" class="field-input pl-9" placeholder="Search by registration or airport"
            [value]="search()" (input)="search.set($any($event.target).value)" />
        </div>
        <select class="field-input w-auto" [value]="typeFilter()" (change)="typeFilter.set($any($event.target).value)" aria-label="Filter by type">
          <option value="">All types</option>
          @for (type of types(); track type.id) {
            <option [value]="type.id">{{ type.icaoCode }}{{ type.isCargo ? ' (Cargo)' : '' }}</option>
          }
        </select>
        <div class="flex flex-wrap rounded-lg border border-line p-1">
          @for (option of statusOptions; track option.value) {
            <button type="button" (click)="statusFilter.set(option.value)"
              class="rounded-md px-3 py-1.5 text-sm font-medium"
              [class]="statusFilter() === option.value ? 'bg-primary text-on-primary' : 'text-ink hover:bg-surface-alt'">
              {{ option.label }}
            </button>
          }
        </div>
        <button type="button" class="btn-primary gap-2 px-4 py-2.5" (click)="openCreate()" [disabled]="loading() || !types().length">
          <i class="ti ti-plus text-lg" aria-hidden="true"></i>New aircraft
        </button>
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
          <div class="flex flex-col items-center gap-2 px-6 py-12 text-center">
            <i class="ti ti-plane-off text-4xl text-ink-muted" aria-hidden="true"></i>
            <p class="text-ink-muted">{{ aircraft().length ? 'No aircraft match the filters.' : 'No aircraft yet. Register the first one.' }}</p>
          </div>
        } @else {
          <table class="w-full text-left text-sm">
            <thead class="border-b border-line text-xs uppercase tracking-wide text-ink-muted">
              <tr>
                <th class="px-4 py-3 font-semibold">Aircraft</th>
                <th class="px-4 py-3 font-semibold">Location</th>
                <th class="px-4 py-3 font-semibold">Status</th>
                <th class="px-4 py-3 font-semibold">Hours</th>
                <th class="px-4 py-3 font-semibold">Next maintenance</th>
                <th class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody>
              @for (item of filtered(); track item.id) {
                <tr class="border-b border-line last:border-0 hover:bg-surface-alt/50" [class.opacity-60]="item.status === 'Inactive'">
                  <td class="px-4 py-3">
                    <div class="flex items-center gap-3">
                      <div class="flex h-12 w-20 shrink-0 items-center justify-center overflow-hidden rounded-md bg-surface-alt">
                        @if (item.photoUrl) {
                          <img [src]="item.photoUrl" [alt]="item.registration" class="h-full w-full object-cover" />
                        } @else {
                          <i class="ti ti-plane text-xl text-ink-muted" aria-hidden="true"></i>
                        }
                      </div>
                      <div>
                        <p class="font-mono font-bold text-ink">{{ item.registration }}</p>
                        <p class="text-xs text-ink-muted">
                          {{ item.aircraftTypeIcao }}{{ item.variant ? ' · ' + item.variant : '' }}
                          @if (item.isCargo) {
                            <span class="ml-1 rounded-full bg-accent/20 px-1.5 py-0.5 text-[10px] font-semibold text-ink">Cargo</span>
                          }
                        </p>
                      </div>
                    </div>
                  </td>
                  <td class="px-4 py-3">
                    <p class="font-mono font-bold text-ink">{{ item.currentAirportIcao }}</p>
                    <p class="text-xs text-ink-muted">{{ item.currentAirportCity }}</p>
                  </td>
                  <td class="px-4 py-3">
                    <span class="rounded-full px-2.5 py-1 text-xs font-semibold" [class]="statusClasses[item.status]">{{ statusLabels[item.status] }}</span>
                    @if (item.status === 'Maintenance') {
                      <p class="mt-1 max-w-52 truncate text-xs text-ink-muted" [title]="item.maintenanceReason ?? ''">{{ item.maintenanceReason }}</p>
                      <p class="text-xs text-ink-muted">
                        {{ item.maintenanceUntil ? 'Until ' + (item.maintenanceUntil | date: 'MMM d, HH:mm') : 'Manual release' }}
                      </p>
                    }
                  </td>
                  <td class="px-4 py-3">
                    <p class="text-ink">{{ format(item.totalMinutes) }}</p>
                    <p class="text-xs text-ink-muted">{{ item.cycles }} cycles</p>
                  </td>
                  <td class="px-4 py-3">
                    <div class="w-36">
                      <div class="h-2 overflow-hidden rounded-full bg-surface-alt">
                        <div class="h-full rounded-full" [class]="item.maintenanceDue ? 'bg-danger' : 'bg-primary'"
                          [style.width.%]="maintenanceProgress(item)"></div>
                      </div>
                      <p class="mt-1 text-xs" [class]="item.maintenanceDue ? 'font-semibold text-danger' : 'text-ink-muted'">
                        {{ item.nextCheck }}-check · {{ item.maintenanceDue ? 'Due now' : format(remainingMinutes(item)) + ' left' }}
                      </p>
                    </div>
                  </td>
                  <td class="px-4 py-3">
                    @if (confirmDelete() === item.id) {
                      <div class="flex items-center justify-end gap-2">
                        <span class="text-xs text-ink-muted">Delete this aircraft?</span>
                        <button type="button" class="rounded-md bg-danger px-3 py-1 text-xs font-medium text-white hover:opacity-90"
                          (click)="remove(item)">Delete</button>
                        <button type="button" class="btn-secondary px-3 py-1 text-xs" (click)="confirmDelete.set(null)">Cancel</button>
                      </div>
                    } @else {
                      <div class="flex justify-end gap-1">
                        <button type="button" class="icon-btn h-9 w-9 text-lg" aria-label="Edit aircraft" title="Edit" (click)="openEdit(item)">
                          <i class="ti ti-pencil" aria-hidden="true"></i>
                        </button>
                        @if (item.status === 'Maintenance') {
                          <button type="button" class="icon-btn h-9 w-9 text-lg text-accent" aria-label="Release from maintenance"
                            title="Release from maintenance" [disabled]="busy() === item.id" (click)="release(item)">
                            <i class="ti ti-circle-check" aria-hidden="true"></i>
                          </button>
                        } @else {
                          <button type="button" class="icon-btn h-9 w-9 text-lg" aria-label="Send to maintenance" title="Send to maintenance"
                            [disabled]="item.status !== 'Available'" (click)="maintenanceFor.set(item)">
                            <i class="ti ti-tool" aria-hidden="true"></i>
                          </button>
                        }
                        <button type="button" class="icon-btn h-9 w-9 text-lg"
                          [attr.aria-label]="item.status === 'Inactive' ? 'Activate' : 'Deactivate'"
                          [title]="item.status === 'Inactive' ? 'Activate' : 'Deactivate'"
                          [disabled]="busy() === item.id || !['Available', 'Inactive'].includes(item.status)" (click)="toggleActive(item)">
                          <i class="ti ti-power" aria-hidden="true"></i>
                        </button>
                        <button type="button" class="icon-btn h-9 w-9 text-lg hover:text-danger" aria-label="Delete aircraft" title="Delete"
                          (click)="askDelete(item.id)">
                          <i class="ti ti-trash" aria-hidden="true"></i>
                        </button>
                      </div>
                    }
                    @if (rowError()?.id === item.id) {
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

    @if (formOpen()) {
      <app-aircraft-form-modal [aircraft]="editing()" [types]="types()" [airports]="airports()"
        (saved)="onSaved($event)" (airportSaved)="onAirportSaved($event)" (closed)="formOpen.set(false)" />
    }

    @if (maintenanceFor(); as item) {
      <app-maintenance-modal [aircraft]="item" (saved)="onMaintenanceSaved($event)" (closed)="maintenanceFor.set(null)" />
    }
  `
})
export class FleetAircraft implements OnInit {
  private readonly api = inject(AdminFleetService);

  protected readonly format = formatMinutes;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly statusClasses = STATUS_CLASSES;

  protected readonly aircraft = signal<AdminAircraft[]>([]);
  protected readonly types = signal<AdminAircraftType[]>([]);
  protected readonly airports = signal<FleetAirport[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly search = signal('');
  protected readonly typeFilter = signal('');
  protected readonly statusFilter = signal<StatusFilter>('all');

  protected readonly formOpen = signal(false);
  protected readonly editing = signal<AdminAircraft | null>(null);
  protected readonly maintenanceFor = signal<AdminAircraft | null>(null);
  protected readonly confirmDelete = signal<string | null>(null);
  protected readonly busy = signal<string | null>(null);
  protected readonly rowError = signal<{ id: string; message: string } | null>(null);

  protected readonly statusOptions: { value: StatusFilter; label: string }[] = [
    { value: 'all', label: 'All' },
    { value: 'available', label: 'Available' },
    { value: 'inUse', label: 'In use' },
    { value: 'maintenance', label: 'Maintenance' },
    { value: 'due', label: 'Due' },
    { value: 'inactive', label: 'Inactive' }
  ];

  protected readonly summary = computed(() => {
    const list = this.aircraft();
    return [
      { label: 'Fleet', value: list.length, alert: false },
      { label: 'Available', value: list.filter(a => a.status === 'Available').length, alert: false },
      { label: 'In maintenance', value: list.filter(a => a.status === 'Maintenance').length, alert: false },
      { label: 'Maintenance due', value: list.filter(a => a.maintenanceDue && a.status !== 'Maintenance').length, alert: true }
    ];
  });

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const type = this.typeFilter();
    const status = this.statusFilter();

    return this.aircraft().filter(a => {
      if (type && a.aircraftTypeId !== type) return false;
      if (status === 'available' && a.status !== 'Available') return false;
      if (status === 'inUse' && a.status !== 'Reserved' && a.status !== 'InFlight') return false;
      if (status === 'maintenance' && a.status !== 'Maintenance') return false;
      if (status === 'due' && !a.maintenanceDue) return false;
      if (status === 'inactive' && a.status !== 'Inactive') return false;

      return !term
        || a.registration.toLowerCase().includes(term)
        || a.registration.replace('-', '').toLowerCase().includes(term)
        || a.currentAirportIcao.toLowerCase().includes(term)
        || a.currentAirportCity.toLowerCase().includes(term);
    });
  });

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);

    try {
      const [aircraft, types, airports] = await Promise.all([this.api.aircraft(), this.api.types(), this.api.airports()]);
      this.aircraft.set(aircraft);
      this.types.set(types);
      this.airports.set(airports);
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected maintenanceProgress(item: AdminAircraft): number {
    return Math.min(100, (item.nextCheckUsedMinutes / (item.nextCheckIntervalHours * 60)) * 100);
  }

  protected remainingMinutes(item: AdminAircraft): number {
    return Math.max(0, item.nextCheckIntervalHours * 60 - item.nextCheckUsedMinutes);
  }

  protected openCreate(): void {
    this.editing.set(null);
    this.formOpen.set(true);
  }

  protected openEdit(item: AdminAircraft): void {
    this.editing.set(item);
    this.formOpen.set(true);
  }

  protected onSaved(item: AdminAircraft): void {
    this.upsert(item);
    this.formOpen.set(false);
  }

  protected onAirportSaved(airport: FleetAirport): void {
    this.airports.update(list =>
      [...list.filter(a => a.icao !== airport.icao), airport].sort((a, b) => a.icao.localeCompare(b.icao))
    );
  }

  protected onMaintenanceSaved(item: AdminAircraft): void {
    this.upsert(item);
    this.maintenanceFor.set(null);
  }

  protected async release(item: AdminAircraft): Promise<void> {
    await this.run(item, () => this.api.releaseFromMaintenance(item.id));
  }

  protected async toggleActive(item: AdminAircraft): Promise<void> {
    await this.run(item, () => this.api.setActive(item.id, item.status === 'Inactive'));
  }

  protected askDelete(id: string): void {
    this.rowError.set(null);
    this.confirmDelete.set(id);
  }

  protected async remove(item: AdminAircraft): Promise<void> {
    try {
      await this.api.removeAircraft(item.id);
      this.aircraft.update(list => list.filter(a => a.id !== item.id));
      this.confirmDelete.set(null);
    } catch (error) {
      this.confirmDelete.set(null);
      this.rowError.set({ id: item.id, message: apiErrorMessage(error) ?? 'Something went wrong. Try again.' });
    }
  }

  private async run(item: AdminAircraft, action: () => Promise<AdminAircraft>): Promise<void> {
    this.busy.set(item.id);
    this.rowError.set(null);

    try {
      this.upsert(await action());
    } catch (error) {
      this.rowError.set({ id: item.id, message: apiErrorMessage(error) ?? 'Something went wrong. Try again.' });
    } finally {
      this.busy.set(null);
    }
  }

  private upsert(item: AdminAircraft): void {
    this.aircraft.update(list =>
      [...list.filter(a => a.id !== item.id), item].sort((a, b) => a.registration.localeCompare(b.registration))
    );
  }
}