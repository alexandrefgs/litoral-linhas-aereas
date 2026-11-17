import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminAirport, AdminAirportsService } from '../../../core/admin/admin-airports.service';
import { AdminRoute, AdminRoutesService, AircraftTypeOption, FlightNumberSuggestion } from '../../../core/admin/admin-routes.service';
import { apiErrorMessage } from '../../../core/api/api-error';
import { ConfirmService } from '../../../shared/confirm-dialog/confirm.service';
import {
  FLIGHT_BLOCK_LABELS,
  blockFromNumber,
  distanceNm,
  estimateBlockMinutes,
  formatMinutes
} from '../../../shared/utils/flight';
import { AirportFormModal } from '../airports/airport-form-modal';

type OperationFilter = 'all' | 'passenger' | 'cargo';
type StatusFilter = 'all' | 'active' | 'inactive';
type AirportField = 'departureIcao' | 'arrivalIcao';

interface AirportCheck {
  icao: string;
  state: 'empty' | 'typing' | 'ok' | 'missing' | 'noOperation';
  airport?: AdminAirport;
}

@Component({
  selector: 'app-admin-routes',
  imports: [ReactiveFormsModule, AirportFormModal],
  host: { '(document:keydown.escape)': 'closeModal()' },
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-ink">Routes</h1>
          <p class="text-ink-muted">Create routes, choose the allowed aircraft and manage flight numbers.</p>
        </div>
        <button type="button" class="btn-primary gap-2 px-4 py-2.5" (click)="openCreate()" [disabled]="loading()">
          <i class="ti ti-plus text-lg" aria-hidden="true"></i>New route
        </button>
      </div>

      <div class="flex flex-wrap items-center gap-3">
        <div class="relative min-w-60 flex-1">
          <i class="ti ti-search pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-ink-muted" aria-hidden="true"></i>
          <input type="search" class="field-input pl-9" placeholder="Search by flight, ICAO or city"
            [value]="search()" (input)="search.set($any($event.target).value)" />
        </div>
        <div class="flex rounded-lg border border-line p-1">
          @for (option of operationOptions; track option.value) {
            <button type="button" (click)="operationFilter.set(option.value)"
              class="rounded-md px-3 py-1.5 text-sm font-medium"
              [class]="operationFilter() === option.value ? 'bg-primary text-on-primary' : 'text-ink hover:bg-surface-alt'">
              {{ option.label }}
            </button>
          }
        </div>
        <div class="flex rounded-lg border border-line p-1">
          @for (option of statusOptions; track option.value) {
            <button type="button" (click)="statusFilter.set(option.value)"
              class="rounded-md px-3 py-1.5 text-sm font-medium"
              [class]="statusFilter() === option.value ? 'bg-primary text-on-primary' : 'text-ink hover:bg-surface-alt'">
              {{ option.label }}
            </button>
          }
        </div>
        <span class="text-sm text-ink-muted">{{ filtered().length }} / {{ routes().length }}</span>
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
            <i class="ti ti-route-off text-4xl text-ink-muted" aria-hidden="true"></i>
            <p class="text-ink-muted">{{ routes().length ? 'No routes match the filters.' : 'No routes yet. Create the first one.' }}</p>
          </div>
        } @else {
          <table class="w-full text-left text-sm">
            <thead class="border-b border-line text-xs uppercase tracking-wide text-ink-muted">
              <tr>
                <th class="px-4 py-3 font-semibold">Flight</th>
                <th class="px-4 py-3 font-semibold">Route</th>
                <th class="px-4 py-3 font-semibold">Aircraft</th>
                <th class="px-4 py-3 font-semibold">Distance</th>
                <th class="px-4 py-3 font-semibold">Status</th>
                <th class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody>
              @for (route of filtered(); track route.id) {
                <tr class="border-b border-line last:border-0 hover:bg-surface-alt/50" [class.opacity-60]="!route.isActive">
                  <td class="px-4 py-3">
                    <p class="font-mono font-bold text-ink">{{ route.callsign }}</p>
                    <p class="text-xs text-ink-muted">{{ blockLabels[route.block] ?? route.block }}</p>
                  </td>
                  <td class="px-4 py-3">
                    <p class="flex items-center gap-2 font-mono font-bold text-ink">
                      {{ route.departureIcao }}
                      <i class="ti ti-arrow-right text-primary" aria-hidden="true"></i>
                      {{ route.arrivalIcao }}
                      @if (route.isCargo) {
                        <span class="rounded-full bg-accent/20 px-2 py-0.5 font-sans text-[11px] font-semibold text-ink">Cargo</span>
                      }
                    </p>
                    <p class="text-xs text-ink-muted">{{ route.departureCity }} – {{ route.arrivalCity }}</p>
                  </td>
                  <td class="px-4 py-3">
                    <div class="flex flex-wrap gap-1">
                      @for (type of route.aircraftTypes; track type) {
                        <span class="rounded-md bg-surface-alt px-2 py-0.5 font-mono text-xs font-semibold text-ink">{{ type }}</span>
                      }
                    </div>
                  </td>
                  <td class="px-4 py-3">
                    <p class="text-ink">{{ route.distanceNm }} NM</p>
                    <p class="text-xs text-ink-muted">{{ format(route.estimatedMinutes) }}</p>
                  </td>
                  <td class="px-4 py-3">
                    @if (route.isActive) {
                      <span class="rounded-full bg-accent/20 px-2.5 py-1 text-xs font-semibold text-ink">Active</span>
                    } @else {
                      <span class="rounded-full bg-surface-alt px-2.5 py-1 text-xs font-semibold text-ink-muted">Inactive</span>
                    }
                  </td>
                  <td class="px-4 py-3">
                    <div class="flex justify-end gap-1">
                      <button type="button" class="icon-btn h-9 w-9 text-lg" aria-label="Edit route" (click)="openEdit(route)">
                        <i class="ti ti-pencil" aria-hidden="true"></i>
                      </button>
                      <button type="button" class="icon-btn h-9 w-9 text-lg hover:text-danger" aria-label="Delete route"
                        (click)="remove(route)">
                        <i class="ti ti-trash" aria-hidden="true"></i>
                      </button>
                    </div>
                    @if (rowError()?.id === route.id) {
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
      <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/50 p-4" (click)="closeModal()">
        <form [formGroup]="form" (ngSubmit)="save()" novalidate (click)="$event.stopPropagation()"
          class="panel flex max-h-[90vh] w-full max-w-2xl flex-col overflow-hidden shadow-2xl">
          <div class="flex items-center justify-between border-b border-line px-6 py-4">
            <h2 class="text-lg font-bold text-ink">{{ editing() ? 'Edit route ' + editing()!.callsign : 'New route' }}</h2>
            <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Cancel" (click)="closeModal()">
              <i class="ti ti-x" aria-hidden="true"></i>
            </button>
          </div>

          <div class="grid gap-5 overflow-y-auto px-6 py-5">
            <fieldset>
              <legend class="field-label">Operation</legend>
              <div class="grid gap-2 sm:grid-cols-2">
                @for (option of [{ cargo: false, label: 'Passenger', icon: 'ti-users' }, { cargo: true, label: 'Cargo', icon: 'ti-package' }]; track option.label) {
                  <label class="flex cursor-pointer items-center gap-3 rounded-lg border px-4 py-3"
                    [class]="value().cargo === option.cargo ? 'border-primary bg-primary/5' : 'border-line hover:bg-surface-alt'">
                    <input type="radio" formControlName="cargo" [value]="option.cargo" class="accent-primary" (change)="onOperationChange()" />
                    <i class="ti {{ option.icon }} text-xl text-primary" aria-hidden="true"></i>
                    <span class="font-medium text-ink">{{ option.label }}</span>
                  </label>
                }
              </div>
            </fieldset>

            <div>
              <div class="grid items-start gap-3 sm:grid-cols-[1fr_auto_1fr]">
                <div>
                  <label for="departure" class="field-label">Departure</label>
                  <input id="departure" formControlName="departureIcao" list="route-airports" maxlength="4" placeholder="ICAO"
                    autocomplete="off" class="field-input font-mono uppercase" (input)="onAirportInput('departureIcao')" />
                  <p class="mt-1 h-4 truncate text-xs text-ink-muted">{{ departureCheck().airport?.city ?? '' }}</p>
                </div>
                <button type="button" class="icon-btn mt-6 h-10 w-10 text-xl" aria-label="Swap airports" (click)="swapAirports()">
                  <i class="ti ti-arrows-exchange" aria-hidden="true"></i>
                </button>
                <div>
                  <label for="arrival" class="field-label">Arrival</label>
                  <input id="arrival" formControlName="arrivalIcao" list="route-airports" maxlength="4" placeholder="ICAO"
                    autocomplete="off" class="field-input font-mono uppercase" (input)="onAirportInput('arrivalIcao')" />
                  <p class="mt-1 h-4 truncate text-xs text-ink-muted">{{ arrivalCheck().airport?.city ?? '' }}</p>
                </div>
              </div>
              <datalist id="route-airports">
                @for (airport of operationAirports(); track airport.icao) {
                  <option [value]="airport.icao">{{ airport.city }} · {{ airport.name }}</option>
                }
              </datalist>

              @for (check of airportAlerts(); track check.icao) {
                <div class="mt-3 flex flex-wrap items-center justify-between gap-3 rounded-lg border border-highlight bg-highlight/15 px-4 py-3">
                  <p class="flex items-center gap-2 text-sm text-ink">
                    <i class="ti ti-alert-triangle text-lg" aria-hidden="true"></i>
                    @if (check.state === 'missing') {
                      <span><span class="font-mono font-bold">{{ check.icao }}</span> is not registered yet.</span>
                    } @else {
                      <span><span class="font-mono font-bold">{{ check.icao }}</span> doesn't have {{ value().cargo ? 'cargo' : 'passenger' }} operations.</span>
                    }
                  </p>
                  <button type="button" class="btn-secondary px-3 py-1.5 text-sm" (click)="openAirportForm(check)">
                    {{ check.state === 'missing' ? 'Register airport' : 'Edit airport' }}
                  </button>
                </div>
              }
            </div>

            @if (distance(); as nm) {
              <div class="grid grid-cols-3 gap-3 rounded-lg bg-surface-alt p-4 text-center">
                <div>
                  <p class="text-xs uppercase tracking-wide text-ink-muted">Distance</p>
                  <p class="text-lg font-bold text-ink">{{ nm }} NM</p>
                </div>
                <div>
                  <p class="text-xs uppercase tracking-wide text-ink-muted">Estimated time</p>
                  <p class="text-lg font-bold text-ink">{{ format(value().estimatedMinutes || defaultMinutes() || 0) }}</p>
                </div>
                <div>
                  <p class="text-xs uppercase tracking-wide text-ink-muted">Block</p>
                  <p class="text-lg font-bold text-ink">{{ blockLabel() }}</p>
                </div>
              </div>
            } @else if (value().departureIcao && value().departureIcao === value().arrivalIcao) {
              <p class="field-error">Departure and arrival cannot be the same.</p>
            }

            <fieldset>
              <legend class="field-label">Aircraft types</legend>
              <div class="grid gap-2 sm:grid-cols-2">
                @for (type of operationTypes(); track type.id) {
                  <label class="flex items-center gap-3 rounded-lg border px-4 py-3"
                    [class]="isSelected(type.id) ? 'border-primary bg-primary/5' : 'border-line'"
                    [class.opacity-50]="outOfRange(type)"
                    [class.cursor-pointer]="!outOfRange(type)">
                    <input type="checkbox" class="h-4 w-4 accent-primary"
                      [checked]="isSelected(type.id)" [disabled]="outOfRange(type)" (change)="toggleType(type.id)" />
                    <span class="min-w-0 flex-1">
                      <span class="block font-mono font-bold text-ink">{{ type.icaoCode }}</span>
                      <span class="block truncate text-xs text-ink-muted">{{ type.name }}</span>
                    </span>
                    <span class="text-xs" [class]="outOfRange(type) ? 'font-semibold text-danger' : 'text-ink-muted'">
                      {{ outOfRange(type) ? 'Out of range' : type.maxRangeNm + ' NM' }}
                    </span>
                  </label>
                }
              </div>
              @if (typesTouched() && !value().aircraftTypeIds?.length) {
                <p class="field-error">Select at least one aircraft type.</p>
              }
            </fieldset>

            <div class="grid gap-4 sm:grid-cols-2">
              <div>
                <label for="flightNumber" class="field-label">Flight number</label>
                <div class="relative">
                  <span class="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 font-mono text-ink-muted">LTL</span>
                  <input id="flightNumber" type="number" formControlName="flightNumber" class="field-input pl-12 font-mono" />
                  @if (suggesting()) {
                    <i class="ti ti-loader-2 absolute right-3 top-1/2 -translate-y-1/2 animate-spin text-ink-muted" aria-hidden="true"></i>
                  }
                </div>
                @if (showError('flightNumber')) {
                  <p class="field-error">Enter a number between 1000 and 9999.</p>
                } @else if (!editing() && suggestion(); as next) {
                  <p class="mt-1 text-xs text-ink-muted">
                    Next in sequence: <span class="font-mono font-semibold text-ink">LTL{{ next.flightNumber }}</span>
                    · {{ blockLabels[next.block] ?? next.block }}
                    @if (value().flightNumber !== next.flightNumber) {
                      <button type="button" class="ml-1 font-medium text-primary hover:underline" (click)="useSuggestion()">Use</button>
                    }
                  </p>
                }
              </div>
              <div>
                <label for="estimatedMinutes" class="field-label">Estimated time (minutes)</label>
                <input id="estimatedMinutes" type="number" formControlName="estimatedMinutes" class="field-input"
                  [placeholder]="defaultMinutes() ? defaultMinutes() + ' (auto)' : 'Auto'" />
                <p class="mt-1 text-xs text-ink-muted">Leave empty to calculate from the distance.</p>
              </div>
            </div>

            @if (!editing()) {
              <div class="rounded-lg border border-line p-4">
                <label class="flex cursor-pointer items-center justify-between gap-4">
                  <span class="flex items-center gap-3">
                    <i class="ti ti-arrows-left-right text-xl text-primary" aria-hidden="true"></i>
                    <span>
                      <span class="block font-medium text-ink">Create return flight</span>
                      <span class="block text-xs text-ink-muted">
                        {{ value().arrivalIcao || '????' }} → {{ value().departureIcao || '????' }} with the same aircraft.
                      </span>
                    </span>
                  </span>
                  <input type="checkbox" formControlName="createReturn" class="h-5 w-5 shrink-0 accent-primary" />
                </label>
                @if (value().createReturn) {
                  <div class="mt-4">
                    <label for="returnFlightNumber" class="field-label">Return flight number</label>
                    <div class="relative sm:w-1/2">
                      <span class="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 font-mono text-ink-muted">LTL</span>
                      <input id="returnFlightNumber" type="number" formControlName="returnFlightNumber" class="field-input pl-12 font-mono" />
                    </div>
                    @if (suggestion()?.returnFlightNumber; as nextReturn) {
                      <p class="mt-1 text-xs text-ink-muted">
                        Next in sequence: <span class="font-mono font-semibold text-ink">LTL{{ nextReturn }}</span>
                      </p>
                    }
                  </div>
                }
              </div>
            } @else {
              <label class="flex cursor-pointer items-center justify-between gap-4 rounded-lg border border-line px-4 py-3">
                <span class="flex items-center gap-3">
                  <i class="ti ti-power text-xl text-primary" aria-hidden="true"></i>
                  <span>
                    <span class="block font-medium text-ink">Active</span>
                    <span class="block text-xs text-ink-muted">Inactive routes can't be booked.</span>
                  </span>
                </span>
                <input type="checkbox" formControlName="isActive" class="h-5 w-5 shrink-0 accent-primary" />
              </label>
            }
          </div>

          @if (formError(); as message) {
            <p class="border-t border-danger/30 bg-danger/10 px-6 py-3 text-sm text-danger">
              <i class="ti ti-alert-circle mr-1" aria-hidden="true"></i>{{ message }}
            </p>
          }

          <div class="flex justify-end gap-2 border-t border-line px-6 py-4">
            <button type="button" class="btn-secondary" (click)="closeModal()">Cancel</button>
            <button type="submit" class="btn-primary px-5" [disabled]="saving()">
              {{ saving() ? 'Loading...' : 'Save' }}
            </button>
          </div>
        </form>
      </div>
    }

    @if (airportModal(); as modal) {
      <app-airport-form-modal [airport]="modal.airport" [initialIcao]="modal.icao" [requireCargo]="!!value().cargo"
        [registeredIcaos]="registeredIcaos()" (saved)="onAirportSaved($event)" (closed)="airportModal.set(null)" />
    }
  `
})
export class AdminRoutes implements OnInit {
  private readonly api = inject(AdminRoutesService);
  private readonly confirm = inject(ConfirmService);
  private readonly airportsApi = inject(AdminAirportsService);

  protected readonly blockLabels = FLIGHT_BLOCK_LABELS;
  protected readonly format = formatMinutes;

  protected readonly routes = signal<AdminRoute[]>([]);
  protected readonly airports = signal<AdminAirport[]>([]);
  protected readonly aircraftTypes = signal<AircraftTypeOption[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly search = signal('');
  protected readonly operationFilter = signal<OperationFilter>('all');
  protected readonly statusFilter = signal<StatusFilter>('all');

  protected readonly modalOpen = signal(false);
  protected readonly editing = signal<AdminRoute | null>(null);
  protected readonly saving = signal(false);
  protected readonly suggesting = signal(false);
  protected readonly typesTouched = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly rowError = signal<{ id: string; message: string } | null>(null);
  protected readonly suggestion = signal<FlightNumberSuggestion | null>(null);
  protected readonly airportModal = signal<{ airport: AdminAirport | null; icao: string } | null>(null);
  private suggestionKey = '';

  protected readonly operationOptions: { value: OperationFilter; label: string }[] = [
    { value: 'all', label: 'All' },
    { value: 'passenger', label: 'Passenger' },
    { value: 'cargo', label: 'Cargo' }
  ];

  protected readonly statusOptions: { value: StatusFilter; label: string }[] = [
    { value: 'all', label: 'All' },
    { value: 'active', label: 'Active' },
    { value: 'inactive', label: 'Inactive' }
  ];

  protected readonly form = inject(FormBuilder).nonNullable.group({
    cargo: [false],
    departureIcao: ['', Validators.required],
    arrivalIcao: ['', Validators.required],
    aircraftTypeIds: [[] as string[]],
    flightNumber: [null as number | null, [Validators.required, Validators.min(1000), Validators.max(9999)]],
    estimatedMinutes: [null as number | null, Validators.min(1)],
    createReturn: [true],
    returnFlightNumber: [null as number | null, [Validators.min(1000), Validators.max(9999)]],
    isActive: [true]
  });

  protected readonly value = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const operation = this.operationFilter();
    const status = this.statusFilter();

    return this.routes().filter(r => {
      if (operation === 'passenger' && r.isCargo) return false;
      if (operation === 'cargo' && !r.isCargo) return false;
      if (status === 'active' && !r.isActive) return false;
      if (status === 'inactive' && r.isActive) return false;

      return !term
        || r.callsign.toLowerCase().includes(term)
        || String(r.flightNumber).includes(term)
        || r.departureIcao.toLowerCase().includes(term)
        || r.arrivalIcao.toLowerCase().includes(term)
        || r.departureCity.toLowerCase().includes(term)
        || r.arrivalCity.toLowerCase().includes(term);
    });
  });

  protected readonly operationAirports = computed(() => {
    const cargo = !!this.value().cargo;
    return this.airports().filter(a => (cargo ? a.cargo : a.passenger));
  });

  protected readonly registeredIcaos = computed(() => this.airports().map(a => a.icao));

  protected readonly departureCheck = computed(() => this.checkAirport(this.value().departureIcao ?? '', !!this.value().cargo));
  protected readonly arrivalCheck = computed(() => this.checkAirport(this.value().arrivalIcao ?? '', !!this.value().cargo));

  protected readonly airportAlerts = computed(() => {
    const alerts = [this.departureCheck(), this.arrivalCheck()]
      .filter(c => c.state === 'missing' || c.state === 'noOperation');
    return alerts.filter((c, i) => alerts.findIndex(o => o.icao === c.icao) === i);
  });

  protected readonly operationTypes = computed(() => {
    const cargo = !!this.value().cargo;
    return this.aircraftTypes().filter(t => t.isCargo === cargo);
  });

  protected readonly distance = computed(() => {
    const { departureIcao, arrivalIcao } = this.value();
    if (!departureIcao || !arrivalIcao || departureIcao === arrivalIcao) return null;

    const departure = this.airports().find(a => a.icao === departureIcao);
    const arrival = this.airports().find(a => a.icao === arrivalIcao);
    if (!departure || !arrival) return null;

    return distanceNm(departure.latitude, departure.longitude, arrival.latitude, arrival.longitude);
  });

  protected readonly defaultMinutes = computed(() => {
    const nm = this.distance();
    return nm ? estimateBlockMinutes(nm) : null;
  });

  protected readonly blockLabel = computed(() => {
    const block = blockFromNumber(this.value().flightNumber);
    return block ? this.blockLabels[block] : '—';
  });

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);

    try {
      const [routes, airports, types] = await Promise.all([
        this.api.list(),
        this.airportsApi.list(),
        this.api.aircraftTypes()
      ]);
      this.routes.set(routes);
      this.airports.set(airports);
      this.aircraftTypes.set(types);
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected openCreate(): void {
    this.editing.set(null);
    this.resetModalState();
    this.form.reset({
      cargo: false,
      departureIcao: '',
      arrivalIcao: '',
      aircraftTypeIds: [],
      flightNumber: null,
      estimatedMinutes: null,
      createReturn: true,
      returnFlightNumber: null,
      isActive: true
    });
    this.modalOpen.set(true);
    this.refreshSuggestion();
  }

  protected openEdit(route: AdminRoute): void {
    this.editing.set(route);
    this.resetModalState();
    this.form.reset({
      cargo: route.isCargo,
      departureIcao: route.departureIcao,
      arrivalIcao: route.arrivalIcao,
      aircraftTypeIds: [...route.aircraftTypeIds],
      flightNumber: route.flightNumber,
      estimatedMinutes: route.estimatedMinutes,
      createReturn: false,
      returnFlightNumber: null,
      isActive: route.isActive
    });
    this.modalOpen.set(true);
  }

  protected closeModal(): void {
    if (this.airportModal() || this.saving()) return;
    this.modalOpen.set(false);
  }

  protected showError(control: keyof typeof this.form.controls): boolean {
    const c = this.form.controls[control];
    return c.invalid && (c.touched || c.dirty);
  }

  protected isSelected(id: string): boolean {
    return this.value().aircraftTypeIds?.includes(id) ?? false;
  }

  protected outOfRange(type: AircraftTypeOption): boolean {
    const nm = this.distance();
    return nm !== null && nm > type.maxRangeNm;
  }

  protected toggleType(id: string): void {
    const current = this.form.controls.aircraftTypeIds.value;
    this.form.controls.aircraftTypeIds.setValue(
      current.includes(id) ? current.filter(t => t !== id) : [...current, id]
    );
    this.typesTouched.set(true);
  }

  protected onAirportInput(field: AirportField): void {
    const control = this.form.controls[field];
    const normalized = control.value.toUpperCase().trim();
    if (normalized !== control.value) control.setValue(normalized);

    if (normalized.length === 0 || normalized.length === 4) this.onRouteChange();
  }

  protected openAirportForm(check: AirportCheck): void {
    this.airportModal.set({ airport: check.airport ?? null, icao: check.icao });
  }

  protected onAirportSaved(airport: AdminAirport): void {
    this.airports.update(list =>
      [...list.filter(a => a.icao !== airport.icao), airport].sort((a, b) => a.icao.localeCompare(b.icao))
    );
    this.airportModal.set(null);
    this.onRouteChange();
  }

  protected useSuggestion(): void {
    const next = this.suggestion();
    if (!next) return;

    this.form.patchValue({ flightNumber: next.flightNumber, returnFlightNumber: next.returnFlightNumber });
  }

  protected swapAirports(): void {
    const { departureIcao, arrivalIcao } = this.form.getRawValue();
    this.form.patchValue({ departureIcao: arrivalIcao, arrivalIcao: departureIcao });
    this.onRouteChange();
  }

  protected onOperationChange(): void {
    this.form.controls.aircraftTypeIds.setValue([]);
    this.onRouteChange();
  }

  protected onRouteChange(): void {
    this.dropOutOfRangeTypes();
    this.refreshSuggestion();
  }

  private async refreshSuggestion(): Promise<void> {
    if (this.editing()) return;

    const { departureIcao, arrivalIcao, cargo } = this.form.getRawValue();
    const ready = this.departureCheck().state === 'ok' && this.arrivalCheck().state === 'ok' && departureIcao !== arrivalIcao;
    const departure = ready ? departureIcao : '';
    const arrival = ready ? arrivalIcao : '';

    const key = `${departure}|${arrival}|${cargo}`;
    if (key === this.suggestionKey) return;
    this.suggestionKey = key;

    this.suggesting.set(true);
    try {
      const next = await this.api.suggestNumber(departure, arrival, cargo);
      if (key !== this.suggestionKey) return;

      const previous = this.suggestion();
      const { flightNumber, returnFlightNumber } = this.form.getRawValue();

      this.form.patchValue({
        flightNumber: flightNumber == null || flightNumber === previous?.flightNumber ? next.flightNumber : flightNumber,
        returnFlightNumber: returnFlightNumber == null || returnFlightNumber === previous?.returnFlightNumber
          ? next.returnFlightNumber
          : returnFlightNumber
      });
      this.suggestion.set(next);
    } catch {
      this.suggestion.set(null);
    } finally {
      if (key === this.suggestionKey) this.suggesting.set(false);
    }
  }

  protected async save(): Promise<void> {
    this.typesTouched.set(true);

    if (this.form.invalid || !this.form.controls.aircraftTypeIds.value.length) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    if (value.departureIcao === value.arrivalIcao) {
      this.formError.set('Departure and arrival cannot be the same.');
      return;
    }

    if (this.departureCheck().state !== 'ok' || this.arrivalCheck().state !== 'ok') {
      this.formError.set('Register and configure both airports before saving.');
      return;
    }

    if (!this.editing() && value.createReturn && !value.returnFlightNumber) {
      this.formError.set('Enter the return flight number.');
      return;
    }

    this.saving.set(true);
    this.formError.set(null);

    try {
      const current = this.editing();

      if (current) {
        const updated = await this.api.update(current.id, {
          flightNumber: value.flightNumber!,
          departureIcao: value.departureIcao,
          arrivalIcao: value.arrivalIcao,
          aircraftTypeIds: value.aircraftTypeIds,
          estimatedMinutes: value.estimatedMinutes || null,
          isActive: value.isActive
        });
        this.upsert([updated]);
      } else {
        const created = await this.api.create({
          flightNumber: value.flightNumber!,
          departureIcao: value.departureIcao,
          arrivalIcao: value.arrivalIcao,
          aircraftTypeIds: value.aircraftTypeIds,
          estimatedMinutes: value.estimatedMinutes || null,
          createReturn: value.createReturn,
          returnFlightNumber: value.createReturn ? value.returnFlightNumber : null
        });
        this.upsert(created);
      }

      this.saving.set(false);
      this.closeModal();
    } catch (error) {
      this.formError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.saving.set(false);
    }
  }

  protected async remove(route: AdminRoute): Promise<void> {
    this.rowError.set(null);
    const ok = await this.confirm.ask({
      title: `Delete ${route.callsign}?`,
      message: 'The route will be removed and can no longer be booked. This can\'t be undone.',
      confirmLabel: 'Yes, delete',
      icon: 'ti-trash'
    });
    if (!ok) return;

    try {
      await this.api.remove(route.id);
      this.routes.update(list => list.filter(r => r.id !== route.id));
    } catch (error) {
      this.rowError.set({ id: route.id, message: apiErrorMessage(error) ?? 'Something went wrong. Try again.' });
    }
  }

  private dropOutOfRangeTypes(): void {
    const ids = this.form.controls.aircraftTypeIds.value;
    const kept = ids.filter(id => {
      const type = this.aircraftTypes().find(t => t.id === id);
      return type && !this.outOfRange(type);
    });

    if (kept.length !== ids.length) this.form.controls.aircraftTypeIds.setValue(kept);
  }

  private checkAirport(icao: string, cargo: boolean): AirportCheck {
    if (!icao) return { icao, state: 'empty' };
    if (icao.length < 4) return { icao, state: 'typing' };

    const airport = this.airports().find(a => a.icao === icao);
    if (!airport) return { icao, state: 'missing' };

    return { icao, airport, state: (cargo ? airport.cargo : airport.passenger) ? 'ok' : 'noOperation' };
  }

  private resetModalState(): void {
    this.formError.set(null);
    this.typesTouched.set(false);
    this.suggesting.set(false);
    this.suggestion.set(null);
    this.suggestionKey = '';
  }

  private upsert(routes: AdminRoute[]): void {
    const ids = new Set(routes.map(r => r.id));
    this.routes.update(list =>
      [...list.filter(r => !ids.has(r.id)), ...routes].sort((a, b) => a.flightNumber - b.flightNumber)
    );
  }
}