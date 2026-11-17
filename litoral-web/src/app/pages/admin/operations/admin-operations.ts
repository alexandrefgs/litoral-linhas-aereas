import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { AdminSpecialFlightsService } from '../../../core/admin/admin-special-flights.service';
import { apiErrorMessage } from '../../../core/api/api-error';
import { ConfirmService } from '../../../shared/confirm-dialog/confirm.service';
import {
  SPECIAL_FLIGHT_STATE,
  SPECIAL_FLIGHT_TYPE_LABEL,
  SpecialFlight,
  specialFlightRoute
} from '../../../core/special-flights/special-flights.models';
import { SpecialFlightFormModal } from './special-flight-form-modal';

type Tab = 'active' | 'closed' | 'all';

const ACTIVE_STATES = ['ComingSoon', 'Available', 'Taken'];

@Component({
  selector: 'app-admin-operations',
  imports: [DatePipe, SpecialFlightFormModal],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-ink">Operations</h1>
          <p class="text-ink-muted">Special flights: ferries, redeliveries, charters and maintenance ferries.</p>
        </div>
        <button type="button" class="btn-primary gap-2 px-4 py-2.5" (click)="openCreate()">
          <i class="ti ti-plus text-lg" aria-hidden="true"></i>New special flight
        </button>
      </div>

      <div class="flex gap-1 border-b border-line">
        @for (option of tabs; track option.value) {
          <button type="button" (click)="tab.set(option.value)"
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
        } @else if (filtered().length === 0) {
          <p class="px-6 py-12 text-center text-ink-muted">No special flights here.</p>
        } @else {
          <table class="w-full text-left text-sm">
            <thead class="border-b border-line text-xs uppercase tracking-wide text-ink-muted">
              <tr>
                <th class="px-4 py-3 font-semibold">Flight</th>
                <th class="px-4 py-3 font-semibold">Route</th>
                <th class="px-4 py-3 font-semibold">Window (UTC)</th>
                <th class="px-4 py-3 font-semibold">Rank</th>
                <th class="px-4 py-3 font-semibold">Status</th>
                <th class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody>
              @for (f of filtered(); track f.id) {
                <tr class="border-b border-line last:border-0 hover:bg-surface-alt/50">
                  <td class="px-4 py-3">
                    <div class="flex items-center gap-3">
                      <div class="flex h-10 w-16 shrink-0 items-center justify-center overflow-hidden rounded-md bg-surface-alt">
                        @if (f.photoUrl ?? f.aircraftPhotoUrl; as url) {
                          <img [src]="url" alt="" class="h-full w-full object-cover" />
                        } @else {
                          <i class="ti ti-plane text-ink-muted" aria-hidden="true"></i>
                        }
                      </div>
                      <div>
                        <p class="font-semibold text-ink">{{ f.title }}</p>
                        <p class="text-xs text-ink-muted">{{ typeLabel[f.type] }} · {{ f.registration }} ({{ f.aircraftTypeIcao }})</p>
                      </div>
                    </div>
                  </td>
                  <td class="px-4 py-3">
                    <p class="font-mono font-semibold text-ink">{{ route(f) }}</p>
                    <p class="font-mono text-xs text-ink-muted">{{ callsigns(f) }}</p>
                  </td>
                  <td class="px-4 py-3 text-ink-muted">
                    <p>{{ f.startsAt | date: 'dd/MM HH:mm' : 'UTC' }}</p>
                    <p>{{ f.endsAt | date: 'dd/MM HH:mm' : 'UTC' }}</p>
                  </td>
                  <td class="px-4 py-3">
                    <img [src]="'images/ranks/' + f.minimumRankCode.toLowerCase() + '.png'" [alt]="f.minimumRankName" [title]="f.minimumRankName" class="h-6 w-auto" />
                  </td>
                  <td class="px-4 py-3">
                    <span class="rounded-md px-2.5 py-1 text-xs font-semibold" [class]="state[f.state].class">{{ state[f.state].label }}</span>
                    @if (f.pilotDisplayId) {
                      <p class="mt-1 text-xs text-ink-muted">{{ f.pilotDisplayId }} · {{ f.pilotName }}</p>
                    }
                  </td>
                  <td class="px-4 py-3">
                    <div class="flex justify-end gap-1">
                      @if (canEdit(f)) {
                        <button type="button" class="icon-btn h-9 w-9 text-lg" aria-label="Edit" (click)="openEdit(f)">
                          <i class="ti ti-pencil" aria-hidden="true"></i>
                        </button>
                      }
                      @if (isActive(f)) {
                        <button type="button" class="icon-btn h-9 w-9 text-lg hover:text-danger" aria-label="Cancel flight" title="Cancel flight"
                          (click)="run(f, 'cancel')">
                          <i class="ti ti-ban" aria-hidden="true"></i>
                        </button>
                      }
                      @if (!f.pilotDisplayId) {
                        <button type="button" class="icon-btn h-9 w-9 text-lg hover:text-danger" aria-label="Delete" title="Delete"
                          (click)="run(f, 'delete')">
                          <i class="ti ti-trash" aria-hidden="true"></i>
                        </button>
                      }
                    </div>
                    @if (rowError()?.id === f.id) {
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
      <app-special-flight-form-modal [flight]="editing()" (saved)="onSaved()" (closed)="modalOpen.set(false)" />
    }
  `
})
export class AdminOperations implements OnInit {
  private readonly api = inject(AdminSpecialFlightsService);
  private readonly confirm = inject(ConfirmService);

  protected readonly tabs: { value: Tab; label: string }[] = [
    { value: 'active', label: 'Active' },
    { value: 'closed', label: 'Closed' },
    { value: 'all', label: 'All' }
  ];
  protected readonly state = SPECIAL_FLIGHT_STATE;
  protected readonly typeLabel = SPECIAL_FLIGHT_TYPE_LABEL;
  protected readonly route = specialFlightRoute;

  protected readonly flights = signal<SpecialFlight[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly tab = signal<Tab>('active');
  protected readonly modalOpen = signal(false);
  protected readonly editing = signal<SpecialFlight | null>(null);
  protected readonly rowError = signal<{ id: string; message: string } | null>(null);

  protected readonly filtered = computed(() => {
    const tab = this.tab();
    return this.flights().filter(f => tab === 'all' || (tab === 'active') === ACTIVE_STATES.includes(f.state));
  });

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);
    try {
      this.flights.set(await this.api.list());
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected isActive(flight: SpecialFlight): boolean {
    return ACTIVE_STATES.includes(flight.state);
  }

  protected canEdit(flight: SpecialFlight): boolean {
    return flight.status === 'Open' && !flight.pilotDisplayId;
  }

  protected callsigns(flight: SpecialFlight): string {
    return flight.legs.map(l => l.callsign).join(' · ');
  }

  protected openCreate(): void {
    this.editing.set(null);
    this.modalOpen.set(true);
  }

  protected openEdit(flight: SpecialFlight): void {
    this.editing.set(flight);
    this.modalOpen.set(true);
  }

  protected async onSaved(): Promise<void> {
    this.modalOpen.set(false);
    await this.load();
  }

  protected async run(flight: SpecialFlight, action: 'cancel' | 'delete'): Promise<void> {
    this.rowError.set(null);
    const ok = await this.confirm.ask(action === 'delete'
      ? {
          title: 'Delete this special flight?',
          message: `"${flight.title}" will be removed. Nobody has booked it yet.`,
          confirmLabel: 'Yes, delete',
          icon: 'ti-trash'
        }
      : {
          title: 'Cancel this special flight?',
          message: flight.pilotDisplayId
            ? `"${flight.title}" is booked by ${flight.pilotDisplayId}. The booking will be cancelled and the flight closed.`
            : `"${flight.title}" will be closed and can no longer be booked.`,
          confirmLabel: 'Yes, cancel flight',
          cancelLabel: 'Keep flight',
          tone: 'warning',
          icon: 'ti-ban'
        });
    if (!ok) return;

    try {
      if (action === 'delete') await this.api.remove(flight.id);
      else await this.api.cancel(flight.id);
      await this.load();
    } catch (error) {
      this.rowError.set({ id: flight.id, message: apiErrorMessage(error) ?? 'Something went wrong. Try again.' });
    }
  }
}