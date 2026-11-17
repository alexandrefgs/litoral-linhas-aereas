import { Component, OnDestroy, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminSpecialFlightsService, SpecialFlightOptions, SpecialFlightPayload } from '../../../core/admin/admin-special-flights.service';
import { apiErrorMessage } from '../../../core/api/api-error';
import { SPECIAL_FLIGHT_TYPES, SpecialFlight, SpecialFlightType } from '../../../core/special-flights/special-flights.models';
import { DateTimePicker } from '../../../shared/date-time-picker/date-time-picker';

const MAX_PHOTO_BYTES = 5 * 1024 * 1024;
const PHOTO_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

function utcInput(date: Date): string {
  return date.toISOString().slice(0, 16);
}

function toUtcIso(value: string): string | null {
  if (!value) return null;
  const date = new Date(`${value}:00Z`);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

@Component({
  selector: 'app-special-flight-form-modal',
  imports: [ReactiveFormsModule, DateTimePicker],
  host: { '(document:keydown.escape)': 'close()' },
  template: `
    <div class="fixed inset-0 z-[1300] flex items-center justify-center bg-black/50 p-4" (click)="close()">
      <form [formGroup]="form" (ngSubmit)="save()" novalidate (click)="$event.stopPropagation()"
        class="panel flex max-h-[92vh] w-full max-w-3xl flex-col overflow-hidden shadow-2xl">
        <div class="flex items-center justify-between border-b border-line px-6 py-4">
          <h2 class="text-lg font-bold text-ink">{{ flight() ? 'Edit special flight' : 'New special flight' }}</h2>
          <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Cancel" (click)="close()">
            <i class="ti ti-x" aria-hidden="true"></i>
          </button>
        </div>

        <div class="grid gap-5 overflow-y-auto px-6 py-5 sm:grid-cols-2">
          <fieldset class="sm:col-span-2">
            <legend class="field-label">Type</legend>
            <div class="grid grid-cols-2 gap-2 sm:grid-cols-4">
              @for (t of types; track t.value) {
                <label class="flex cursor-pointer flex-col items-center gap-1.5 rounded-lg border px-3 py-3 text-center text-sm font-medium"
                  [class]="type() === t.value ? 'border-primary bg-primary/10 text-primary' : 'border-line text-ink hover:bg-surface-alt'">
                  <input type="radio" formControlName="type" [value]="t.value" class="sr-only" />
                  <i class="ti {{ t.icon }} text-2xl" aria-hidden="true"></i>
                  {{ t.label }}
                </label>
              }
            </div>
          </fieldset>

          <div class="sm:col-span-2">
            <label for="sfAircraft" class="field-label">Aircraft</label>
            <select id="sfAircraft" formControlName="aircraftId" class="field-input">
              <option value="">Select an aircraft</option>
              @for (a of aircraftOptions(); track a.id) {
                <option [value]="a.id" [disabled]="a.disabled">{{ a.label }}</option>
              }
            </select>
            @if (type() === 'Redelivery') {
              <p class="mt-1 text-xs text-ink-muted">The aircraft must be at its delivery airport (Boeing: KBFI). Register it in the Fleet at that airport first.</p>
            } @else {
              <p class="mt-1 text-xs text-ink-muted">The first leg departs from where the aircraft is.</p>
            }
          </div>

          <div class="sm:col-span-2">
            <span class="field-label">Legs</span>
            <div class="flex flex-wrap items-center gap-2">
              <span class="rounded-md border border-line bg-surface-alt px-3 py-2 font-mono font-bold text-ink">{{ departure() || '----' }}</span>
              <i class="ti ti-arrow-right text-ink-muted" aria-hidden="true"></i>
              <input formControlName="firstArrival" list="sfAirports" maxlength="4" placeholder="ICAO"
                class="field-input w-28 font-mono uppercase" [readonly]="type() === 'MaintenanceFerry'" />
              @if (hasSecondLeg()) {
                <i class="ti ti-arrow-right text-ink-muted" aria-hidden="true"></i>
                <input formControlName="secondArrival" list="sfAirports" maxlength="4" placeholder="ICAO" class="field-input w-28 font-mono uppercase" />
                <button type="button" class="icon-btn h-9 w-9 text-lg hover:text-danger" aria-label="Remove second leg" (click)="setSecondLeg(false)">
                  <i class="ti ti-x" aria-hidden="true"></i>
                </button>
              } @else if (type() !== 'MaintenanceFerry') {
                <button type="button" class="btn-secondary gap-1.5 px-3 py-2 text-sm" (click)="setSecondLeg(true)">
                  <i class="ti ti-plus" aria-hidden="true"></i> Second leg
                </button>
              }
            </div>
            <datalist id="sfAirports">
              @for (a of options()?.airports ?? []; track a.icao) {
                <option [value]="a.icao">{{ a.city }} · {{ a.name }}</option>
              }
            </datalist>
            @if (type() === 'MaintenanceFerry') {
              <p class="mt-1 text-xs text-ink-muted">Maintenance ferries always go to the maintenance hub ({{ options()?.maintenanceHubIcao }}).</p>
            }
          </div>

          <div class="sm:col-span-2">
            <label for="sfTitle" class="field-label">Title</label>
            <input id="sfTitle" formControlName="title" maxlength="80" class="field-input" [placeholder]="suggestedTitle()" />
            <p class="mt-1 text-xs text-ink-muted">Leave empty to use "{{ suggestedTitle() }}".</p>
          </div>

          <div class="sm:col-span-2">
            <label for="sfDescription" class="field-label">Description</label>
            <textarea id="sfDescription" formControlName="description" rows="3" maxlength="1000" class="field-input"
              placeholder="Briefing for the pilot (optional)"></textarea>
          </div>

          <div>
            <span class="field-label">Difficulty</span>
            <div class="flex gap-1">
              @for (star of stars; track star) {
                <button type="button" class="text-3xl leading-none" [attr.aria-label]="star + ' stars'" (click)="form.controls.difficulty.setValue(star)"
                  [class]="star <= form.controls.difficulty.value ? 'text-sol-500' : 'text-line'">★</button>
              }
            </div>
            <p class="mt-1 text-xs text-ink-muted">Informative only: difficulty, distance and deadline.</p>
          </div>

          <fieldset>
            <legend class="field-label">Minimum rank</legend>
            <div class="flex gap-2">
              @for (rank of options()?.ranks ?? []; track rank.code) {
                <label class="flex flex-1 cursor-pointer items-center gap-2 rounded-lg border px-3 py-2"
                  [class]="form.controls.minimumRank.value === rank.code ? 'border-primary bg-primary/5' : 'border-line hover:bg-surface-alt'">
                  <input type="radio" formControlName="minimumRank" [value]="rank.code" class="accent-primary" />
                  <img [src]="'images/ranks/' + rank.code.toLowerCase() + '.png'" [alt]="rank.name" class="h-5 w-auto" />
                  <span class="text-sm font-bold text-ink">{{ rank.code }}</span>
                </label>
              }
            </div>
          </fieldset>

          <div>
            <label for="sfStart" class="field-label">Start (UTC)</label>
            <app-date-time-picker inputId="sfStart" formControlName="startsAt" [utc]="true" />
            <p class="mt-1 text-xs text-ink-muted">Before this, it shows as Coming soon.</p>
          </div>
          <div>
            <label for="sfEnd" class="field-label">End (UTC)</label>
            <app-date-time-picker inputId="sfEnd" formControlName="endsAt" [utc]="true" [min]="form.controls.startsAt.value || null" />
            <p class="mt-1 text-xs text-ink-muted">Expires here, unless the last leg is already in flight.</p>
          </div>

          <fieldset class="sm:col-span-2">
            <legend class="field-label">Photo</legend>
            <div class="grid gap-4 sm:grid-cols-[220px_1fr]">
              <div class="flex aspect-video items-center justify-center overflow-hidden rounded-lg border border-line bg-surface-alt">
                @if (previewUrl(); as url) {
                  <img [src]="url" alt="Special flight photo" class="h-full w-full object-cover" />
                } @else {
                  <i class="ti ti-photo text-4xl text-ink-muted" aria-hidden="true"></i>
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
                  <label for="sfCredit" class="field-label">Photo credit</label>
                  <input id="sfCredit" formControlName="photoCredit" maxlength="150" class="field-input" />
                </div>
                @if (photoError(); as message) {
                  <p class="field-error">{{ message }}</p>
                } @else {
                  <p class="text-xs text-ink-muted">Optional. Without a photo, the card uses the aircraft photo.</p>
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
          <button type="submit" class="btn-primary px-5" [disabled]="saving() || !options()">
            {{ saving() ? 'Saving...' : 'Save' }}
          </button>
        </div>
      </form>
    </div>
  `
})
export class SpecialFlightFormModal implements OnInit, OnDestroy {
  private readonly api = inject(AdminSpecialFlightsService);

  readonly flight = input<SpecialFlight | null>(null);
  readonly saved = output<SpecialFlight>();
  readonly closed = output<void>();

  protected readonly types = SPECIAL_FLIGHT_TYPES;
  protected readonly stars = [1, 2, 3, 4, 5];

  protected readonly options = signal<SpecialFlightOptions | null>(null);
  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly hasSecondLeg = signal(false);
  protected readonly photoError = signal<string | null>(null);
  protected readonly photoFile = signal<File | null>(null);
  protected readonly photoRemoved = signal(false);
  protected readonly localPreview = signal<string | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    type: ['Ferry' as SpecialFlightType, Validators.required],
    aircraftId: ['', Validators.required],
    firstArrival: ['', [Validators.required, Validators.pattern(/^[A-Za-z0-9]{4}$/)]],
    secondArrival: [''],
    title: ['', Validators.maxLength(80)],
    description: ['', Validators.maxLength(1000)],
    difficulty: [3],
    minimumRank: ['FO', Validators.required],
    startsAt: ['', Validators.required],
    endsAt: ['', Validators.required],
    photoCredit: ['', Validators.maxLength(150)]
  });

  protected readonly type = toSignal(this.form.controls.type.valueChanges, { initialValue: this.form.controls.type.value });
  private readonly aircraftId = toSignal(this.form.controls.aircraftId.valueChanges, { initialValue: '' });

  protected readonly selectedAircraft = computed(() => this.options()?.aircraft.find(a => a.id === this.aircraftId()) ?? null);
  protected readonly departure = computed(() => this.selectedAircraft()?.locationIcao ?? '');

  protected readonly aircraftOptions = computed(() => {
    const current = this.flight()?.aircraftId;
    return (this.options()?.aircraft ?? []).map(a => {
      const busy = a.inSpecialFlight && a.id !== current;
      const unavailable = a.status === 'InFlight' || a.status === 'Reserved';
      return {
        id: a.id,
        label: `${a.registration} · ${a.typeIcao} · at ${a.locationIcao}${busy ? ' (in another special flight)' : unavailable ? ` (${a.status})` : ''}`,
        disabled: busy || unavailable
      };
    });
  });

  protected readonly suggestedTitle = computed(() => {
    const label = SPECIAL_FLIGHT_TYPES.find(t => t.value === this.type())?.label.toUpperCase() ?? '';
    const registration = this.selectedAircraft()?.registration;
    return registration ? `${label} - ${registration}` : label;
  });

  protected readonly previewUrl = computed(() => {
    if (this.localPreview()) return this.localPreview();
    if (this.photoRemoved()) return null;
    return this.flight()?.photoUrl ?? null;
  });

  constructor() {
    this.form.controls.type.valueChanges.subscribe(type => this.applyType(type));
  }

  async ngOnInit(): Promise<void> {
    const flight = this.flight();
    const now = new Date();

    if (flight) {
      const legs = flight.legs;
      this.form.reset({
        type: flight.type,
        aircraftId: flight.aircraftId,
        firstArrival: legs[0]?.arrivalIcao ?? '',
        secondArrival: legs[1]?.arrivalIcao ?? '',
        title: flight.title,
        description: flight.description ?? '',
        difficulty: flight.difficulty,
        minimumRank: flight.minimumRankCode,
        startsAt: flight.startsAt.slice(0, 16),
        endsAt: flight.endsAt.slice(0, 16),
        photoCredit: flight.photoCredit ?? ''
      });
      this.hasSecondLeg.set(legs.length > 1);
    } else {
      this.form.patchValue({
        startsAt: utcInput(now),
        endsAt: utcInput(new Date(now.getTime() + 48 * 3_600_000))
      });
    }

    try {
      this.options.set(await this.api.options());
      this.applyType(this.form.controls.type.value);
    } catch {
      this.formError.set('Could not load the aircraft and airports. Try again.');
    }
  }

  ngOnDestroy(): void {
    this.revokePreview();
  }

  protected close(): void {
    if (!this.saving()) this.closed.emit();
  }

  protected setSecondLeg(enabled: boolean): void {
    this.hasSecondLeg.set(enabled);
    const control = this.form.controls.secondArrival;
    control.setValue('');
    control.setValidators(enabled ? [Validators.required, Validators.pattern(/^[A-Za-z0-9]{4}$/)] : []);
    control.updateValueAndValidity();
  }

  protected onFileSelected(event: Event): void {
    const element = event.target as HTMLInputElement;
    const file = element.files?.[0];
    element.value = '';
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

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError.set('Fill in the aircraft, the destinations (ICAO) and the start and end.');
      return;
    }

    const v = this.form.getRawValue();
    const startsAt = toUtcIso(v.startsAt);
    const endsAt = toUtcIso(v.endsAt);
    if (!startsAt || !endsAt || endsAt <= startsAt) {
      this.formError.set('The end must be after the start.');
      return;
    }

    const stops = [this.departure(), v.firstArrival.trim().toUpperCase()];
    if (this.hasSecondLeg()) stops.push(v.secondArrival.trim().toUpperCase());

    const payload: SpecialFlightPayload = {
      title: v.title.trim() || this.suggestedTitle(),
      description: v.description.trim() || null,
      type: v.type,
      difficulty: v.difficulty,
      aircraftId: v.aircraftId,
      minimumRank: v.minimumRank,
      startsAt,
      endsAt,
      stops
    };

    this.saving.set(true);
    this.formError.set(null);

    try {
      const current = this.flight();
      let result = current ? await this.api.update(current.id, payload) : await this.api.create(payload);

      const credit = v.photoCredit.trim() || null;
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

  private applyType(type: SpecialFlightType): void {
    const hub = this.options()?.maintenanceHubIcao;
    if (type === 'MaintenanceFerry' && hub) {
      if (this.hasSecondLeg()) this.setSecondLeg(false);
      this.form.controls.firstArrival.setValue(hub);
    }
  }

  private revokePreview(): void {
    const url = this.localPreview();
    if (url) URL.revokeObjectURL(url);
    this.localPreview.set(null);
  }
}