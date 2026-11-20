import { Component, OnDestroy, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminAirport, AdminAirportsService } from '../../../core/admin/admin-airports.service';
import {
  AdminAircraft,
  AdminAircraftType,
  AdminFleetService,
  AircraftPayload,
  FleetAirport,
  RegistrationSuggestion
} from '../../../core/admin/admin-fleet.service';
import { apiErrorMessage } from '../../../core/api/api-error';
import { AuthService } from '../../../core/auth/auth.service';
import { hasStaffArea } from '../../../core/staff/staff-positions';
import { AirportFormModal } from '../airports/airport-form-modal';
import { prohibitedReason } from '../../../shared/utils/registration';

const MAX_PHOTO_BYTES = 5 * 1024 * 1024;
const PHOTO_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

type AirportState = 'empty' | 'typing' | 'ok' | 'missing' | 'noOperation';

@Component({
  selector: 'app-aircraft-form-modal',
  imports: [ReactiveFormsModule, AirportFormModal],
  host: { '(document:keydown.escape)': 'close()' },
  template: `
    <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/50 p-4" (click)="close()">
      <form [formGroup]="form" (ngSubmit)="save()" novalidate (click)="$event.stopPropagation()"
        class="panel flex max-h-[90vh] w-full max-w-2xl flex-col overflow-hidden shadow-2xl">
        <div class="flex items-center justify-between border-b border-line px-6 py-4">
          <h2 class="text-lg font-bold text-ink">{{ aircraft() ? 'Edit ' + aircraft()!.registration : 'New aircraft' }}</h2>
          <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Cancel" (click)="close()">
            <i class="ti ti-x" aria-hidden="true"></i>
          </button>
        </div>

        <div class="grid gap-5 overflow-y-auto px-6 py-5">
          @if (inUse()) {
            <p class="rounded-lg bg-surface-alt px-4 py-3 text-sm text-ink-muted">
              <i class="ti ti-info-circle mr-1" aria-hidden="true"></i>This aircraft is in use. Type and location can't be changed right now.
            </p>
          }

          <div>
            <label for="aircraftType" class="field-label">Aircraft type</label>
            <select id="aircraftType" formControlName="aircraftTypeId" class="field-input" (change)="onTypeChange()">
              <option value="" disabled>Select a type</option>
              @for (type of types(); track type.id) {
                <option [value]="type.id">{{ type.icaoCode }} · {{ type.name }}{{ type.isCargo ? ' (Cargo)' : '' }}</option>
              }
            </select>
            @if (showError('aircraftTypeId')) {
              <p class="field-error">Select the aircraft type.</p>
            }
          </div>

          <div class="grid gap-4 sm:grid-cols-2">
            <div>
              <label for="registration" class="field-label">Registration</label>
              <input id="registration" formControlName="registration" maxlength="6" [placeholder]="blockFull() ? 'PT-MXA' : 'PS-LAA'"
                autocomplete="off" class="field-input font-mono uppercase" (input)="onRegistrationInput()" />
              @if (showError('registration')) {
                <p class="field-error">Use PP, PR, PS, PT or PU followed by 3 letters, e.g. PT-MXA.</p>
              } @else if (registrationProblem(); as problem) {
                <p class="field-error">{{ problem }}</p>
              } @else if (blockFull()) {
                <p class="mt-1 text-xs text-ink-muted">Custom registration: PP, PR, PS, PT or PU + 3 letters.</p>
              } @else if (!aircraft() && suggestion(); as next) {
                <p class="mt-1 text-xs text-ink-muted">
                  Next free: <span class="font-mono font-semibold text-ink">{{ next.registration }}</span> · {{ next.free }} left in {{ next.block }}
                  @if (value().registration !== next.registration) {
                    <button type="button" class="ml-1 font-medium text-primary hover:underline" (click)="useSuggestion()">Use</button>
                  }
                </p>
              } @else {
                <p class="mt-1 text-xs text-ink-muted">Choose the type to get the next free registration.</p>
              }
            </div>
            <div>
              <label for="variant" class="field-label">Variant <span class="font-normal text-ink-muted">(optional)</span></label>
              <input id="variant" formControlName="variant" placeholder="e.g. Winglets, SFP" class="field-input" />
            </div>
          </div>

          <div>
            <label for="simBriefAirframeId" class="field-label">SimBrief airframe ID <span class="font-normal text-ink-muted">(optional)</span></label>
            <input id="simBriefAirframeId" formControlName="simBriefAirframeId" placeholder="123456_1582090020"
              autocomplete="off" class="field-input font-mono sm:w-1/2" />
            @if (showError('simBriefAirframeId')) {
              <p class="field-error">Use the Internal ID from SimBrief, e.g. 123456_1582090020.</p>
            } @else {
              <p class="mt-1 text-xs text-ink-muted">SimBrief › Saved Airframes › Edit › Internal ID. Empty uses the generic {{ selectedTypeIcao() || 'type' }} profile.</p>
            }
          </div>
          @if (blockFull(); as block) {
            <div class="flex items-start gap-3 rounded-lg border border-highlight bg-highlight/15 px-4 py-3 text-sm text-ink">
              <i class="ti ti-alert-triangle mt-0.5 text-lg" aria-hidden="true"></i>
              <p>
                The <span class="font-mono font-bold">{{ block }}</span> block is full.
                Choose another registration for this aircraft, e.g. <span class="font-mono font-bold">PT-MXA</span>.
              </p>
            </div>
          }

          <div>
            <label for="currentAirport" class="field-label">Current airport</label>
            <input id="currentAirport" formControlName="currentAirportIcao" list="fleet-airports" maxlength="4" placeholder="ICAO"
              autocomplete="off" class="field-input font-mono uppercase sm:w-1/2" (input)="onAirportInput()" />
            <datalist id="fleet-airports">
              @for (airport of operationAirports(); track airport.icao) {
                <option [value]="airport.icao">{{ airport.city }} · {{ airport.name }}</option>
              }
            </datalist>
            <p class="mt-1 h-4 truncate text-xs text-ink-muted">{{ airportCheck().airport?.city ?? '' }}</p>

            @if (airportCheck().state === 'missing' || airportCheck().state === 'noOperation') {
              <div class="mt-2 flex flex-wrap items-center justify-between gap-3 rounded-lg border border-highlight bg-highlight/15 px-4 py-3">
                <p class="flex items-center gap-2 text-sm text-ink">
                  <i class="ti ti-alert-triangle text-lg" aria-hidden="true"></i>
                  @if (airportCheck().state === 'missing') {
                    <span><span class="font-mono font-bold">{{ airportCheck().icao }}</span> is not registered yet.</span>
                  } @else {
                    <span><span class="font-mono font-bold">{{ airportCheck().icao }}</span> doesn't have {{ cargo() ? 'cargo' : 'passenger' }} operations.</span>
                  }
                </p>
                @if (canManageAirports()) {
                  <button type="button" class="btn-secondary px-3 py-1.5 text-sm" (click)="openAirportForm()">
                    {{ airportCheck().state === 'missing' ? 'Register airport' : 'Edit airport' }}
                  </button>
                } @else {
                  <span class="text-xs text-ink-muted">Ask the Route Manager to configure it.</span>
                }
              </div>
            }
          </div>

          <fieldset>
            <legend class="field-label">Photo</legend>
            <div class="grid gap-4 sm:grid-cols-[220px_1fr]">
              <div class="flex aspect-video items-center justify-center overflow-hidden rounded-lg border border-line bg-surface-alt">
                @if (previewUrl(); as url) {
                  <img [src]="url" alt="Aircraft photo" class="h-full w-full object-cover" />
                } @else {
                  <i class="ti ti-plane text-4xl text-ink-muted" aria-hidden="true"></i>
                }
              </div>
              <div class="flex flex-col gap-3">
                <div class="flex flex-wrap gap-2">
                  <label class="btn-secondary cursor-pointer gap-2 px-3 py-2 text-sm">
                    <i class="ti ti-upload" aria-hidden="true"></i>{{ previewUrl() ? 'Change photo' : 'Upload photo' }}
                    <input type="file" accept="image/jpeg,image/png,image/webp" class="hidden" (change)="onFileSelected($event)" />
                  </label>
                  @if (previewUrl()) {
                    <button type="button" class="btn-secondary px-3 py-2 text-sm hover:text-danger" (click)="removePhoto()">Remove</button>
                  }
                </div>
                <div>
                  <label for="photoCredit" class="field-label">Photo credit</label>
                  <input id="photoCredit" formControlName="photoCredit" placeholder="Photographer name" class="field-input" />
                </div>
                @if (photoError(); as message) {
                  <p class="field-error">{{ message }}</p>
                } @else {
                  <p class="text-xs text-ink-muted">JPG, PNG or WEBP up to 5 MB. Use only photos you own or have permission to use.</p>
                }
              </div>
            </div>
          </fieldset>
        </div>

        @if (formError(); as message) {
          <p class="border-t border-danger/30 bg-danger/10 px-6 py-3 text-sm text-danger">
            <i class="ti ti-alert-circle mr-1" aria-hidden="true"></i>{{ message }}
          </p>
        }

        <div class="flex justify-end gap-2 border-t border-line px-6 py-4">
          <button type="button" class="btn-secondary" (click)="close()">Cancel</button>
          <button type="submit" class="btn-primary px-5" [disabled]="saving()">{{ saving() ? 'Loading...' : 'Save' }}</button>
        </div>
      </form>
    </div>

    @if (airportModal(); as modal) {
      <app-airport-form-modal [airport]="modal.airport" [initialIcao]="modal.icao" [requireCargo]="cargo()"
        [registeredIcaos]="registeredIcaos()" (saved)="onAirportSaved($event)" (closed)="airportModal.set(null)" />
    }
  `
})
export class AircraftFormModal implements OnInit, OnDestroy {
  private readonly api = inject(AdminFleetService);
  private readonly airportsApi = inject(AdminAirportsService);
  private readonly auth = inject(AuthService);

  readonly aircraft = input<AdminAircraft | null>(null);
  readonly types = input<AdminAircraftType[]>([]);
  readonly airports = input<FleetAirport[]>([]);

  readonly saved = output<AdminAircraft>();
  readonly airportSaved = output<FleetAirport>();
  readonly closed = output<void>();

  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly photoError = signal<string | null>(null);
  protected readonly photoFile = signal<File | null>(null);
  protected readonly photoRemoved = signal(false);
  protected readonly localPreview = signal<string | null>(null);
  protected readonly airportModal = signal<{ airport: AdminAirport | null; icao: string } | null>(null);
  private readonly extraAirports = signal<FleetAirport[]>([]);
  private readonly created = signal<AdminAircraft | null>(null);
  protected readonly suggestion = signal<RegistrationSuggestion | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    registration: ['', [Validators.required, Validators.pattern(/^P[PRSTU]-[A-Z]{3}$/)]],
    aircraftTypeId: ['', Validators.required],
    variant: [''],
    simBriefAirframeId: ['', Validators.pattern(/^\s*(\d{1,12}_\d{6,16})?\s*$/)],
    currentAirportIcao: ['', [Validators.required, Validators.pattern(/^[A-Z0-9]{4}$/)]],
    photoCredit: ['']
  });

  protected readonly value = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  protected readonly canManageAirports = computed(() => hasStaffArea(this.auth.pilot()?.staffPositions ?? [], 'routes'));
  protected readonly inUse = computed(() => ['Reserved', 'InFlight'].includes(this.aircraft()?.status ?? ''));

  private readonly allAirports = computed(() => {
    const extra = this.extraAirports();
    return [...this.airports().filter(a => !extra.some(e => e.icao === a.icao)), ...extra];
  });

  protected readonly registeredIcaos = computed(() => this.allAirports().map(a => a.icao));

  protected readonly blockFull = computed(() => {
    const next = this.suggestion();
    return !this.aircraft() && next && next.free === 0 ? next.block : null;
  });

  protected readonly registrationProblem = computed(() => {
    const registration = this.value().registration ?? '';
    if (registration.length < 6 || registration === this.aircraft()?.registration) return null;
    
    const prohibited = prohibitedReason(registration.slice(3));
    if (prohibited) return `${registration} is prohibited by ANAC: ${prohibited}.`;

    const typeId = this.value().aircraftTypeId;
    const reserved = this.types().find(t => t.id !== typeId && t.registrationPrefix && registration.startsWith(`PS-${t.registrationPrefix}`));
    if (reserved) return `PS-${reserved.registrationPrefix}_ is reserved for the ${reserved.name}.`;

    const next = this.suggestion();
    const type = this.types().find(t => t.id === typeId);
    if (next && next.free > 0 && type?.registrationPrefix && !registration.startsWith(`PS-${type.registrationPrefix}`)) {
      return `The ${next.block} block still has free registrations. Use ${next.registration}.`;
    }

    return null;
  });

  protected readonly selectedTypeIcao = computed(() => this.types().find(t => t.id === this.value().aircraftTypeId)?.icaoCode ?? '');

  protected readonly cargo = computed(() => this.types().find(t => t.id === this.value().aircraftTypeId)?.isCargo ?? false);

  protected readonly operationAirports = computed(() => {
    const cargo = this.cargo();
    return this.allAirports().filter(a => (cargo ? a.cargo : a.passenger));
  });

  protected readonly airportCheck = computed(() => {
    const icao = this.value().currentAirportIcao ?? '';
    const state = (s: AirportState, airport?: FleetAirport) => ({ icao, state: s, airport });

    if (!icao) return state('empty');
    if (icao.length < 4) return state('typing');

    const airport = this.allAirports().find(a => a.icao === icao);
    if (!airport) return state('missing');

    return state((this.cargo() ? airport.cargo : airport.passenger) ? 'ok' : 'noOperation', airport);
  });

  protected readonly previewUrl = computed(() => {
    if (this.localPreview()) return this.localPreview();
    if (this.photoRemoved()) return null;
    return this.aircraft()?.photoUrl ?? null;
  });

  ngOnInit(): void {
    const aircraft = this.aircraft();
    if (!aircraft) return;

    this.form.reset({
      registration: aircraft.registration,
      aircraftTypeId: aircraft.aircraftTypeId,
      variant: aircraft.variant ?? '',
      simBriefAirframeId: aircraft.simBriefAirframeId ?? '',
      currentAirportIcao: aircraft.currentAirportIcao,
      photoCredit: aircraft.photoCredit ?? ''
    });

    if (this.inUse()) {
      this.form.controls.aircraftTypeId.disable();
      this.form.controls.currentAirportIcao.disable();
    }
  }

  ngOnDestroy(): void {
    this.revokePreview();
  }

  protected close(): void {
    if (this.airportModal() || this.saving()) return;
    this.closed.emit();
  }

  protected showError(control: keyof typeof this.form.controls): boolean {
    const c = this.form.controls[control];
    return c.invalid && (c.touched || c.dirty);
  }

  protected onRegistrationInput(): void {
    const control = this.form.controls.registration;
    const letters = control.value.toUpperCase().replace(/[^A-Z]/g, '').slice(0, 5);
    const formatted = letters.length > 2 ? `${letters.slice(0, 2)}-${letters.slice(2)}` : letters;
    if (formatted !== control.value) control.setValue(formatted);
  }

  protected async onTypeChange(): Promise<void> {
    if (this.aircraft()) return;

    const typeId = this.form.controls.aircraftTypeId.value;
    const previous = this.suggestion();
    this.suggestion.set(null);
    if (!typeId) return;

    try {
      const next = await this.api.suggestRegistration(typeId);
      if (this.form.controls.aircraftTypeId.value !== typeId) return;

      const registration = this.form.controls.registration;
      const replace = !registration.value || registration.value === previous?.registration;
      this.suggestion.set(next);

      if (next.registration && (replace || this.registrationProblem())) {
        registration.setValue(next.registration);
      } else if (!next.registration && replace) {
        registration.setValue('');
      }
    } catch (error) {
      this.formError.set(apiErrorMessage(error) ?? 'Could not suggest a registration.');
    }
  }

  protected useSuggestion(): void {
    const next = this.suggestion();
    if (next?.registration) this.form.controls.registration.setValue(next.registration);
  }

  protected onAirportInput(): void {
    const control = this.form.controls.currentAirportIcao;
    const normalized = control.value.toUpperCase().trim();
    if (normalized !== control.value) control.setValue(normalized);
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    if (!PHOTO_TYPES.includes(file.type)) {
      this.photoError.set('Use a JPG, PNG or WEBP photo.');
      return;
    }
    if (file.size > MAX_PHOTO_BYTES) {
      this.photoError.set('The photo must be 5 MB or smaller.');
      return;
    }

    this.photoError.set(null);
    this.revokePreview();
    this.photoFile.set(file);
    this.photoRemoved.set(false);
    this.localPreview.set(URL.createObjectURL(file));
  }

  protected removePhoto(): void {
    this.revokePreview();
    this.photoFile.set(null);
    this.photoRemoved.set(true);
    this.form.controls.photoCredit.setValue('');
  }

  protected async openAirportForm(): Promise<void> {
    const { icao, state } = this.airportCheck();

    if (state === 'missing') {
      this.airportModal.set({ airport: null, icao });
      return;
    }

    try {
      const airport = (await this.airportsApi.list()).find(a => a.icao === icao) ?? null;
      this.airportModal.set({ airport, icao });
    } catch (error) {
      this.formError.set(apiErrorMessage(error) ?? 'Could not load the airport. Try again.');
    }
  }

  protected onAirportSaved(airport: AdminAirport): void {
    const fleetAirport: FleetAirport = {
      icao: airport.icao,
      name: airport.name,
      city: airport.city,
      passenger: airport.passenger,
      cargo: airport.cargo
    };

    this.extraAirports.update(list => [...list.filter(a => a.icao !== airport.icao), fleetAirport]);
    this.airportSaved.emit(fleetAirport);
    this.airportModal.set(null);
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.registrationProblem()) {
      this.form.controls.registration.markAsTouched();
      return;
    }

    if (this.airportCheck().state !== 'ok') {
      this.formError.set('Register and configure the current airport before saving.');
      return;
    }

    const value = this.form.getRawValue();
    const payload: AircraftPayload = {
      registration: value.registration,
      aircraftTypeId: value.aircraftTypeId,
      variant: value.variant.trim() || null,
      currentAirportIcao: value.currentAirportIcao,
      simBriefAirframeId: value.simBriefAirframeId.trim() || null
    };    
    const credit = value.photoCredit.trim() || null;

    this.saving.set(true);
    this.formError.set(null);

    try {
      const current = this.aircraft() ?? this.created();
      let result = current ? await this.api.updateAircraft(current.id, payload) : await this.api.createAircraft(payload);
      if (!current) this.created.set(result);

      const file = this.photoFile();
      if (file) {
        result = await this.api.uploadPhoto(result.id, file, credit);
      } else if (this.photoRemoved() && result.photoUrl) {
        result = await this.api.removePhoto(result.id);
      } else if (result.photoUrl && credit !== result.photoCredit) {
        result = await this.api.updatePhotoCredit(result.id, credit);
      }

      this.saving.set(false);
      this.saved.emit(result);
    } catch (error) {
      this.formError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.saving.set(false);
    }
  }

  private revokePreview(): void {
    const url = this.localPreview();
    if (url) URL.revokeObjectURL(url);
    this.localPreview.set(null);
  }
}