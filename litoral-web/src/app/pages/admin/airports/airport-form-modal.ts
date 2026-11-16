import { Component, OnDestroy, OnInit, computed, inject, input, output, signal } from '@angular/core';
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

type LookupState = 'idle' | 'loading' | 'found' | 'notFound' | 'exists';

const MAX_PHOTO_BYTES = 5 * 1024 * 1024;
const PHOTO_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

function atLeastOneOperation(group: AbstractControl): ValidationErrors | null {
  const { passenger, cargo } = group.value as { passenger?: boolean; cargo?: boolean };
  return passenger || cargo ? null : { operations: true };
}

@Component({
  selector: 'app-airport-form-modal',
  imports: [ReactiveFormsModule],
  host: { '(document:keydown.escape)': 'close()' },
  template: `
    <div class="fixed inset-0 z-[1300] flex items-center justify-center bg-black/50 p-4" (click)="close()">
      <form [formGroup]="form" (ngSubmit)="save()" novalidate (click)="$event.stopPropagation()"
        class="panel flex max-h-[90vh] w-full max-w-2xl flex-col overflow-hidden shadow-2xl">
        <div class="flex items-center justify-between border-b border-line px-6 py-4">
          <h2 class="text-lg font-bold text-ink">{{ airport() ? 'Edit airport' : 'New airport' }}</h2>
          <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Cancel" (click)="close()">
            <i class="ti ti-x" aria-hidden="true"></i>
          </button>
        </div>

        <div class="grid gap-4 overflow-y-auto px-6 py-5 sm:grid-cols-2">
          <div>
            <label for="icao" class="field-label">ICAO</label>
            <div class="relative">
              <input id="icao" formControlName="icao" maxlength="4" class="field-input pr-9 font-mono uppercase" (input)="lookup()" />
              @if (lookupState() === 'loading') {
                <i class="ti ti-loader-2 absolute right-3 top-1/2 -translate-y-1/2 animate-spin text-ink-muted" aria-hidden="true"></i>
              } @else if (lookupState() === 'found') {
                <i class="ti ti-circle-check absolute right-3 top-1/2 -translate-y-1/2 text-accent" aria-hidden="true"></i>
              }
            </div>
            @if (showError('icao')) {
              <p class="field-error">Enter 4 letters or numbers.</p>
            } @else if (lookupState() === 'exists') {
              <p class="field-error">This airport is already registered.</p>
            } @else if (lookupState() === 'found') {
              <p class="mt-1 text-xs text-ink-muted">Filled from the airport catalog. Review before saving.</p>
            } @else if (lookupState() === 'notFound') {
              <p class="mt-1 text-xs text-ink-muted">Not found in the catalog. Fill in the fields manually.</p>
            } @else if (!airport()) {
              <p class="mt-1 text-xs text-ink-muted">Type the ICAO to fill in the other fields.</p>
            }
          </div>
          <div>
            <label for="iata" class="field-label">IATA</label>
            <input id="iata" formControlName="iata" maxlength="3" class="field-input font-mono uppercase" placeholder="FLN" />
            @if (showError('iata')) {
              <p class="field-error">Enter 3 letters or numbers.</p>
            }
          </div>
          <div class="sm:col-span-2">
            <label for="name" class="field-label">Name</label>
            <input id="name" formControlName="name" class="field-input" />
            @if (showError('name')) {
              <p class="field-error">Required field.</p>
            }
          </div>
          <div>
            <label for="city" class="field-label">City</label>
            <input id="city" formControlName="city" class="field-input" />
            @if (showError('city')) {
              <p class="field-error">Required field.</p>
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

          <fieldset class="sm:col-span-2">
            <legend class="field-label">Photo</legend>
            <div class="grid gap-4 sm:grid-cols-[220px_1fr]">
              <div class="flex aspect-video items-center justify-center overflow-hidden rounded-lg border border-line bg-surface-alt">
                @if (previewUrl(); as url) {
                  <img [src]="url" alt="Airport photo" class="h-full w-full object-cover" />
                } @else {
                  <i class="ti ti-building-airport text-4xl text-ink-muted" aria-hidden="true"></i>
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
                  <input id="photoCredit" formControlName="photoCredit" maxlength="150" placeholder="Author / Wikimedia Commons" class="field-input" />
                </div>
                @if (photoError(); as message) {
                  <p class="field-error">{{ message }}</p>
                } @else {
                  <p class="text-xs text-ink-muted">Wide image, JPG, PNG or WEBP up to 5 MB. Use only photos you own or with a free license, and credit the author.</p>
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
          <button type="submit" class="btn-primary px-5" [disabled]="saving()">
            {{ saving() ? 'Loading...' : 'Save' }}
          </button>
        </div>
      </form>
    </div>
  `
})
export class AirportFormModal implements OnInit, OnDestroy {
  private readonly api = inject(AdminAirportsService);
  private readonly reference = inject(ReferenceService);

  readonly airport = input<AdminAirport | null>(null);
  readonly initialIcao = input('');
  readonly requireCargo = input(false);
  readonly registeredIcaos = input<string[]>([]);

  readonly saved = output<AdminAirport>();
  readonly closed = output<void>();

  protected readonly ranks = signal<RankInfo[]>([]);
  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly lookupState = signal<LookupState>('idle');
  protected readonly photoError = signal<string | null>(null);
  protected readonly photoFile = signal<File | null>(null);
  protected readonly photoRemoved = signal(false);
  protected readonly localPreview = signal<string | null>(null);

  protected readonly previewUrl = computed(() => {
    if (this.localPreview()) return this.localPreview();
    if (this.photoRemoved()) return null;
    return this.airport()?.photoUrl ?? null;
  });
  private lastLookup = '';

  private readonly baseRank = computed(() => this.ranks()[0]?.code ?? 'FO');

  protected readonly form = inject(FormBuilder).nonNullable.group({
    icao: ['', [Validators.required, Validators.pattern(/^[A-Za-z0-9]{4}$/)]],
    iata: ['', Validators.pattern(/^[A-Za-z0-9]{3}$/)],
    name: ['', [Validators.required, Validators.maxLength(120)]],
    city: ['', [Validators.required, Validators.maxLength(80)]],
    country: ['Brazil', [Validators.required, Validators.maxLength(60)]],
    latitude: ['', [Validators.required, latitudeValidator]],
    longitude: ['', [Validators.required, longitudeValidator]],
    minimumRank: ['FO', Validators.required],
    passenger: [true],
    cargo: [false],
    isHub: [false],
    photoCredit: ['', Validators.maxLength(150)]
  }, { validators: atLeastOneOperation });

  constructor() {
    this.form.controls.minimumRank.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(rank => this.syncHubControl(rank));
  }

  async ngOnInit(): Promise<void> {
    const airport = this.airport();

    if (airport) {
      this.form.reset({
        icao: airport.icao,
        iata: airport.iata ?? '',
        name: airport.name,
        city: airport.city,
        country: airport.country,
        latitude: formatLatitude(airport.latitude),
        longitude: formatLongitude(airport.longitude),
        minimumRank: airport.minimumRank,
        passenger: airport.passenger,
        cargo: airport.cargo || this.requireCargo(),
        isHub: airport.isHub,
        photoCredit: airport.photoCredit ?? ''
      });
      this.form.controls.icao.disable();
    } else {
      this.form.patchValue({
        icao: this.initialIcao().toUpperCase(),
        passenger: !this.requireCargo(),
        cargo: this.requireCargo()
      });
      if (this.initialIcao()) this.lookup();
    }

    try {
      const ranks = await this.reference.getRanks();
      this.ranks.set([...ranks].sort((a, b) => a.level - b.level));
      this.syncHubControl(this.form.controls.minimumRank.value);
    } catch {
      this.formError.set('Could not load the ranks. Try again.');
    }
  }

  ngOnDestroy(): void {
    this.revokePreview();
  }

  protected close(): void {
    if (!this.saving()) this.closed.emit();
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

  protected async lookup(): Promise<void> {
    const icao = this.form.controls.icao.value.trim().toUpperCase();

    if (this.airport() || !/^[A-Z0-9]{4}$/.test(icao)) {
      this.lookupState.set('idle');
      this.lastLookup = '';
      return;
    }

    if (icao === this.lastLookup) return;
    this.lastLookup = icao;

    if (this.registeredIcaos().includes(icao)) {
      this.lookupState.set('exists');
      return;
    }

    this.lookupState.set('loading');

    try {
      const airport = await this.api.lookup(icao);
      if (this.lastLookup !== icao) return;

      this.form.patchValue({
        iata: airport.iata ?? '',
        name: airport.name,
        city: airport.city,
        country: airport.country,
        latitude: formatLatitude(airport.latitude),
        longitude: formatLongitude(airport.longitude)
      });
      this.lookupState.set('found');
    } catch {
      if (this.lastLookup === icao) this.lookupState.set('notFound');
    }
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.lookupState() === 'exists') {
      this.formError.set('This airport is already registered. Edit it from the list instead.');
      return;
    }

    const value = this.form.getRawValue();
    const payload: AirportPayload = {
      iata: value.iata.trim().toUpperCase() || null,
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
      const current = this.airport();
      let result = current
        ? await this.api.update(current.icao, payload)
        : await this.api.create(value.icao.trim().toUpperCase(), payload);

      const credit = value.photoCredit.trim() || null;
      const file = this.photoFile();
      if (file) {
        result = await this.api.uploadPhoto(result.icao, file, credit);
      } else if (this.photoRemoved() && result.photoUrl) {
        result = await this.api.removePhoto(result.icao);
      } else if (result.photoUrl && credit !== result.photoCredit) {
        result = await this.api.updatePhotoCredit(result.icao, credit);
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

  private syncHubControl(rank: string): void {
    const control = this.form.controls.isHub;

    if (rank === this.baseRank()) {
      control.enable({ emitEvent: false });
    } else {
      control.setValue(false, { emitEvent: false });
      control.disable({ emitEvent: false });
    }
  }
}