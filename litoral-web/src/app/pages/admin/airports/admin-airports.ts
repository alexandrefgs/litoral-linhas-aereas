import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { AdminAirport, AdminAirportsService, AirportPayload } from '../../../core/admin/admin-airports.service';
import { apiErrorMessage } from '../../../core/api/api-error';
import { RankInfo } from '../../../core/reference/reference.models';
import { ReferenceService } from '../../../core/reference/reference.service';
import {
  formatLatitude,
  formatLongitude,
  latitudeValidator,
  longitudeValidator,
  parseLatitude,
  parseLongitude
} from '../../../shared/utils/coordinates';

type AirportFilter = 'all' | 'hubs' | 'base' | 'restricted';

function atLeastOneOperation(group: AbstractControl): ValidationErrors | null {
  const { passenger, cargo } = group.value as { passenger?: boolean; cargo?: boolean };
  return passenger || cargo ? null : { operations: true };
}

@Component({
  selector: 'app-admin-airports',
  imports: [ReactiveFormsModule],
  host: { '(document:keydown.escape)': 'closeModal()' },
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
      <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/50 p-4" (click)="closeModal()">
        <form [formGroup]="form" (ngSubmit)="save()" novalidate (click)="$event.stopPropagation()"
          class="panel flex max-h-[90vh] w-full max-w-2xl flex-col overflow-hidden shadow-2xl">
          <div class="flex items-center justify-between border-b border-line px-6 py-4">
            <h2 class="text-lg font-bold text-ink">{{ editing() ? 'Edit airport' : 'New airport' }}</h2>
            <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Cancel" (click)="closeModal()">
              <i class="ti ti-x" aria-hidden="true"></i>
            </button>
          </div>

          <div class="grid gap-4 overflow-y-auto px-6 py-5 sm:grid-cols-2">
            <div>
              <label for="icao" class="field-label">ICAO</label>
              <input id="icao" formControlName="icao" maxlength="4" class="field-input font-mono uppercase" />
              @if (showError('icao')) {
                <p class="field-error">Enter 4 letters or numbers.</p>
              }
            </div>
            <div>
              <label for="country" class="field-label">Country</label>
              <input id="country" formControlName="country" class="field-input" />
              @if (showError('country')) {
                <p class="field-error">Required field.</p>
              }
            </div>
            <div class="sm:col-span-2">
              <label for="name" class="field-label">Name</label>
              <input id="name" formControlName="name" class="field-input" />
              @if (showError('name')) {
                <p class="field-error">Required field.</p>
              }
            </div>
            <div class="sm:col-span-2">
              <label for="city" class="field-label">City</label>
              <input id="city" formControlName="city" class="field-input" />
              @if (showError('city')) {
                <p class="field-error">Required field.</p>
              }
            </div>
            <div class="sm:col-span-2">
              <span class="field-label">Coordinates</span>
              <div class="flex items-center gap-2">
                <input id="latitude" formControlName="latitude" placeholder="12 54 31S" aria-label="Latitude"
                  class="field-input font-mono uppercase" (input)="splitPastedCoordinates()" />
                <span class="text-xl font-semibold text-ink-muted" aria-hidden="true">/</span>
                <input id="longitude" formControlName="longitude" placeholder="038 19 21W" aria-label="Longitude"
                  class="field-input font-mono uppercase" />
              </div>
              @if (showError('latitude') || showError('longitude')) {
                <p class="field-error">Invalid format. Use 12 54 31S/038 19 21W.</p>
              } @else {
                <p class="mt-1 text-xs text-ink-muted">AISWEB/ROTAER format, with or without spaces. E.g. 12 54 31S / 038 19 21W or 125431S / 0381921W</p>
              }
            </div>

            <fieldset class="sm:col-span-2">
              <legend class="field-label">Minimum rank</legend>
              <div class="grid gap-2 sm:grid-cols-2">
                @for (rank of ranks(); track rank.code) {
                  <label class="flex cursor-pointer items-center gap-3 rounded-lg border px-4 py-3"
                    [class]="form.controls.minimumRank.value === rank.code ? 'border-primary bg-primary/5' : 'border-line hover:bg-surface-alt'">
                    <input type="radio" formControlName="minimumRank" [value]="rank.code" class="accent-primary" />
                    <span>
                      <span class="block font-bold text-ink">{{ rank.code }}</span>
                      <span class="block text-xs text-ink-muted">{{ rank.name }}</span>
                    </span>
                  </label>
                }
              </div>
              <p class="mt-1 text-xs text-ink-muted">FO: all pilots can operate. CMD: captains only.</p>
            </fieldset>

            <fieldset class="sm:col-span-2">
              <legend class="field-label">Operation</legend>
              <div class="grid gap-2 sm:grid-cols-2">
                <label class="flex cursor-pointer items-center gap-3 rounded-lg border border-line px-4 py-3 hover:bg-surface-alt">
                  <input type="checkbox" formControlName="passenger" class="h-4 w-4 accent-primary" />
                  <i class="ti ti-users text-xl text-primary" aria-hidden="true"></i>
                  <span class="font-medium text-ink">Passenger</span>
                </label>
                <label class="flex cursor-pointer items-center gap-3 rounded-lg border border-line px-4 py-3 hover:bg-surface-alt">
                  <input type="checkbox" formControlName="cargo" class="h-4 w-4 accent-primary" />
                  <i class="ti ti-package text-xl text-primary" aria-hidden="true"></i>
                  <span class="font-medium text-ink">Cargo</span>
                </label>
              </div>
              @if (form.hasError('operations') && (form.controls.passenger.touched || form.controls.cargo.touched)) {
                <p class="field-error">Select at least one operation.</p>
              } @else {
                <p class="mt-1 text-xs text-ink-muted">Litoral operations at this airport. Cargo routes require cargo operation at both airports.</p>
              }
            </fieldset>

            <div class="sm:col-span-2">
              <label class="flex items-center justify-between gap-4 rounded-lg border border-line px-4 py-3"
                [class.opacity-50]="form.controls.isHub.disabled"
                [class.cursor-pointer]="form.controls.isHub.enabled">
                <span class="flex items-center gap-3">
                  <i class="ti ti-star text-xl text-primary" aria-hidden="true"></i>
                  <span>
                    <span class="block font-medium text-ink">Make it a hub</span>
                    <span class="block text-xs text-ink-muted">Hubs must be FO. Hubs with passenger operation can be chosen as base by pilots.</span>
                  </span>
                </span>
                <input type="checkbox" formControlName="isHub" class="h-5 w-5 shrink-0 accent-primary" />
              </label>
            </div>

            @if (formError(); as message) {
              <p class="rounded-md bg-danger/10 px-3 py-2 text-sm text-danger sm:col-span-2">{{ message }}</p>
            }
          </div>

          <div class="flex justify-end gap-2 border-t border-line px-6 py-4">
            <button type="button" class="btn-secondary" (click)="closeModal()">Cancel</button>
            <button type="submit" class="btn-primary px-5" [disabled]="saving()">
              {{ saving() ? 'Loading...' : 'Save' }}
            </button>
          </div>
        </form>
      </div>
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
  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly confirmDelete = signal<string | null>(null);
  protected readonly rowError = signal<{ icao: string; message: string } | null>(null);

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

  protected readonly form = inject(FormBuilder).nonNullable.group({
    icao: ['', [Validators.required, Validators.pattern(/^[A-Za-z0-9]{4}$/)]],
    name: ['', [Validators.required, Validators.maxLength(120)]],
    city: ['', [Validators.required, Validators.maxLength(80)]],
    country: ['Brazil', [Validators.required, Validators.maxLength(60)]],
    latitude: ['', [Validators.required, latitudeValidator]],
    longitude: ['', [Validators.required, longitudeValidator]],
    minimumRank: ['FO', Validators.required],
    passenger: [true],
    cargo: [false],
    isHub: [false]
  }, { validators: atLeastOneOperation });

  constructor() {
    this.form.controls.minimumRank.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(rank => this.syncHubControls(rank));
  }

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
    this.formError.set(null);
    this.form.reset({
      icao: '',
      name: '',
      city: '',
      country: 'Brazil',
      latitude: '',
      longitude: '',
      minimumRank: this.baseRank(),
      passenger: true,
      cargo: false,
      isHub: false
    });
    this.form.controls.icao.enable();
    this.syncHubControls(this.baseRank());
    this.modalOpen.set(true);
  }

  protected openEdit(airport: AdminAirport): void {
    this.editing.set(airport);
    this.formError.set(null);
    this.form.reset({
      icao: airport.icao,
      name: airport.name,
      city: airport.city,
      country: airport.country,
      latitude: formatLatitude(airport.latitude),
      longitude: formatLongitude(airport.longitude),
      minimumRank: airport.minimumRank,
      passenger: airport.passenger,
      cargo: airport.cargo,
      isHub: airport.isHub
    });
    this.form.controls.icao.disable();
    this.syncHubControls(airport.minimumRank);
    this.modalOpen.set(true);
  }

  protected closeModal(): void {
    if (!this.saving()) this.modalOpen.set(false);
  }

  protected showError(control: keyof typeof this.form.controls): boolean {
    const c = this.form.controls[control];
    return c.invalid && (c.touched || c.dirty);
  }

  protected splitPastedCoordinates(): void {
    const { latitude, longitude } = this.form.controls;
    const [lat, lon] = latitude.value.split('/');

    if (lon === undefined) return;

    latitude.setValue(lat.trim());
    longitude.setValue(lon.trim());
    longitude.markAsDirty();
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const payload: AirportPayload = {
      name: value.name.trim(),
      city: value.city.trim(),
      country: value.country.trim(),
      latitude: parseLatitude(value.latitude)!,
      longitude: parseLongitude(value.longitude)!,
      minimumRank: value.minimumRank,
      passenger: value.passenger,
      cargo: value.cargo,
      isHub: value.isHub
    };

    this.saving.set(true);
    this.formError.set(null);

    try {
      const current = this.editing();
      const result = current
        ? await this.api.update(current.icao, payload)
        : await this.api.create(value.icao.trim().toUpperCase(), payload);

      this.upsert(result);
      this.saving.set(false);
      this.closeModal();
    } catch (error) {
      this.formError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.saving.set(false);
    }
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

  private syncHubControls(rank: string): void {
    const control = this.form.controls.isHub;

    if (rank === this.baseRank()) {
      control.enable({ emitEvent: false });
    } else {
      control.setValue(false, { emitEvent: false });
      control.disable({ emitEvent: false });
    }
  }

  private upsert(airport: AdminAirport): void {
    this.airports.update(list =>
      [...list.filter(a => a.icao !== airport.icao), airport].sort((a, b) => a.icao.localeCompare(b.icao))
    );
  }
}