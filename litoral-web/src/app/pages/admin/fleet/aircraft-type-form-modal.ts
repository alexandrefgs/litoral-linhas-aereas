import { Component, OnInit, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminAircraftType, AdminFleetService, AircraftTypePayload } from '../../../core/admin/admin-fleet.service';
import { apiErrorMessage } from '../../../core/api/api-error';
import { prohibitedReason } from '../../../shared/utils/registration';

const DEFAULT_INTERVAL_HOURS = 500;

@Component({
  selector: 'app-aircraft-type-form-modal',
  imports: [ReactiveFormsModule],
  host: { '(document:keydown.escape)': 'close()' },
  template: `
    <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/50 p-4" (click)="close()">
      <form [formGroup]="form" (ngSubmit)="save()" novalidate (click)="$event.stopPropagation()"
        class="panel flex max-h-[90vh] w-full max-w-lg flex-col overflow-hidden shadow-2xl">
        <div class="flex items-center justify-between border-b border-line px-6 py-4">
          <h2 class="text-lg font-bold text-ink">{{ type() ? 'Edit aircraft type' : 'New aircraft type' }}</h2>
          <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Cancel" (click)="close()">
            <i class="ti ti-x" aria-hidden="true"></i>
          </button>
        </div>

        <div class="grid gap-4 overflow-y-auto px-6 py-5 sm:grid-cols-2">
          <div>
            <label for="typeIcao" class="field-label">ICAO code</label>
            <input id="typeIcao" formControlName="icaoCode" maxlength="4" placeholder="B738" class="field-input font-mono uppercase" />
            @if (showError('icaoCode')) {
              <p class="field-error">Enter 4 letters or numbers.</p>
            }
          </div>
          <div>
            <label for="typeRange" class="field-label">Range (NM)</label>
            <input id="typeRange" type="number" formControlName="maxRangeNm" class="field-input" />
            @if (showError('maxRangeNm')) {
              <p class="field-error">Enter a value greater than zero.</p>
            }
          </div>
          <div>
            <label for="typePrefix" class="field-label">Registration prefix</label>
            <div class="relative">
              <span class="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 font-mono text-ink-muted">PS-</span>
              <input id="typePrefix" formControlName="registrationPrefix" maxlength="2" placeholder="LA"
                class="field-input pl-10 font-mono uppercase" (input)="onPrefixInput()" />
            </div>
            @if (showError('registrationPrefix')) {
              <p class="field-error">Enter 2 letters, e.g. LA.</p>
            } @else if (prefixProblem(); as problem) {
              <p class="field-error">{{ problem }}</p>
            } @else if (form.controls.registrationPrefix.disabled) {
              <p class="mt-1 text-xs text-ink-muted">Locked: this type already has aircraft.</p>
            } @else {
              <p class="mt-1 text-xs text-ink-muted">Aircraft will be PS-{{ form.controls.registrationPrefix.value || '__' }}A, B, C...</p>
            }
          </div>
          <div>
            <label for="typeInterval" class="field-label">Maintenance interval (hours)</label>
            <input id="typeInterval" type="number" formControlName="maintenanceIntervalHours" class="field-input" />
            @if (showError('maintenanceIntervalHours')) {
              <p class="field-error">Enter a value greater than zero.</p>
            } @else {
              <button type="button" class="mt-1 text-xs font-medium text-primary hover:underline" (click)="resetInterval()">Use default ({{ defaultInterval }}h)</button>
            }
          </div>
          <div class="sm:col-span-2">
            <label for="typeName" class="field-label">Name</label>
            <input id="typeName" formControlName="name" placeholder="Boeing 737-800" class="field-input" />
            @if (showError('name')) {
              <p class="field-error">Required field.</p>
            }
          </div>
          <div class="sm:col-span-2">
            <label for="typeDelivery" class="field-label">Delivery airport</label>
            <input id="typeDelivery" formControlName="deliveryAirportIcao" maxlength="4" placeholder="KBFI" class="field-input font-mono uppercase sm:w-1/2" />
            @if (showError('deliveryAirportIcao')) {
              <p class="field-error">Enter a 4-character ICAO code.</p>
            } @else {
              <p class="mt-1 text-xs text-ink-muted">Where redeliveries of this type depart from (Boeing: KBFI).</p>
            }
          </div>

          <fieldset class="sm:col-span-2">
            <legend class="field-label">Operation</legend>
            <div class="grid gap-2 sm:grid-cols-2">
              @for (option of operations; track option.label) {
                <label class="flex items-center gap-3 rounded-lg border px-4 py-3"
                  [class]="form.controls.isCargo.value === option.cargo ? 'border-primary bg-primary/5' : 'border-line hover:bg-surface-alt'"
                  [class.opacity-50]="form.controls.isCargo.disabled"
                  [class.cursor-pointer]="form.controls.isCargo.enabled">
                  <input type="radio" formControlName="isCargo" [value]="option.cargo" class="accent-primary" />
                  <i class="ti {{ option.icon }} text-xl text-primary" aria-hidden="true"></i>
                  <span class="font-medium text-ink">{{ option.label }}</span>
                </label>
              }
            </div>
            @if (form.controls.isCargo.disabled) {
              <p class="mt-1 text-xs text-ink-muted">This type already has aircraft or routes, so the operation is locked.</p>
            }
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
  `
})
export class AircraftTypeFormModal implements OnInit {
  private readonly api = inject(AdminFleetService);

  readonly type = input<AdminAircraftType | null>(null);
  readonly saved = output<AdminAircraftType>();
  readonly closed = output<void>();

  protected readonly defaultInterval = DEFAULT_INTERVAL_HOURS;
  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected readonly operations = [
    { cargo: false, label: 'Passenger', icon: 'ti-users' },
    { cargo: true, label: 'Cargo', icon: 'ti-package' }
  ];

  protected readonly form = inject(FormBuilder).nonNullable.group({
    icaoCode: ['', [Validators.required, Validators.pattern(/^[A-Za-z0-9]{4}$/)]],
    name: ['', [Validators.required, Validators.maxLength(60)]],
    isCargo: [false],
    maxRangeNm: [null as number | null, [Validators.required, Validators.min(1)]],
    registrationPrefix: ['', [Validators.required, Validators.pattern(/^[A-Z]{2}$/)]],
    maintenanceIntervalHours: [DEFAULT_INTERVAL_HOURS as number | null, [Validators.required, Validators.min(1)]],
    deliveryAirportIcao: ['', Validators.pattern(/^[A-Za-z0-9]{4}$/)]
  });

  protected prefixProblem(): string | null {
    const prefix = this.form.controls.registrationPrefix.value;
    const reason = prefix.length === 2 ? prohibitedReason(prefix) : null;
    return reason ? `Prohibited by ANAC: ${reason}.` : null;
  }

  ngOnInit(): void {
    const type = this.type();
    if (!type) return;

    this.form.reset({
      icaoCode: type.icaoCode,
      name: type.name,
      isCargo: type.isCargo,
      maxRangeNm: type.maxRangeNm,
      registrationPrefix: type.registrationPrefix ?? '',
      maintenanceIntervalHours: type.maintenanceIntervalHours,
      deliveryAirportIcao: type.deliveryAirportIcao ?? ''
    });

    if (type.aircraftCount > 0 || type.routeCount > 0) this.form.controls.isCargo.disable();
    if (type.aircraftCount > 0 && type.registrationPrefix) this.form.controls.registrationPrefix.disable();
  }

  protected close(): void {
    if (!this.saving()) this.closed.emit();
  }

  protected showError(control: keyof typeof this.form.controls): boolean {
    const c = this.form.controls[control];
    return c.invalid && (c.touched || c.dirty);
  }

  protected onPrefixInput(): void {
    const control = this.form.controls.registrationPrefix;
    const normalized = control.value.toUpperCase().replace(/[^A-Z]/g, '').slice(0, 2);
    if (normalized !== control.value) control.setValue(normalized);
  }

  protected resetInterval(): void {
    this.form.controls.maintenanceIntervalHours.setValue(DEFAULT_INTERVAL_HOURS);
  }

  protected async save(): Promise<void> {
    if (this.form.invalid || this.prefixProblem()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const payload: AircraftTypePayload = {
      icaoCode: value.icaoCode.trim().toUpperCase(),
      name: value.name.trim(),
      isCargo: value.isCargo,
      maxRangeNm: value.maxRangeNm!,
      registrationPrefix: value.registrationPrefix,
      maintenanceIntervalHours: value.maintenanceIntervalHours!,
      deliveryAirportIcao: value.deliveryAirportIcao.trim().toUpperCase() || null
    };

    this.saving.set(true);
    this.formError.set(null);

    try {
      const current = this.type();
      const result = current ? await this.api.updateType(current.id, payload) : await this.api.createType(payload);
      this.saving.set(false);
      this.saved.emit(result);
    } catch (error) {
      this.formError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.saving.set(false);
    }
  }
}