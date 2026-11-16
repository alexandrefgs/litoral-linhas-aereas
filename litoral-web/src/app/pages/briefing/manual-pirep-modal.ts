import { Component, OnInit, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { apiErrorMessage } from '../../core/api/api-error';
import { Booking } from '../../core/booking/booking.models';
import { FlightNetwork, NETWORK_LABELS } from '../../core/pirep/pirep.models';
import { PirepService } from '../../core/pirep/pirep.service';
import { DateTimePicker } from '../../shared/date-time-picker/date-time-picker';

function toUtcIso(value: string): string | null {
  if (!value) return null;
  const date = new Date(`${value}:00Z`);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

function utcInput(date: Date): string {
  return date.toISOString().slice(0, 16);
}

@Component({
  selector: 'app-manual-pirep-modal',
  imports: [ReactiveFormsModule, DateTimePicker],
  host: { '(document:keydown.escape)': 'close()' },
  template: `
    <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/50 p-4" (click)="close()">
      <form [formGroup]="form" (ngSubmit)="save()" novalidate (click)="$event.stopPropagation()"
        class="panel flex max-h-[90vh] w-full max-w-2xl flex-col overflow-hidden shadow-2xl">
        <div class="flex items-center justify-between border-b border-line px-6 py-4">
          <div>
            <h2 class="text-lg font-bold text-ink">Manual PIREP · {{ booking().callsign }}</h2>
            <p class="text-xs text-ink-muted">Manual PIREPs are always reviewed by the Safety team. Times in UTC.</p>
          </div>
          <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Cancel" (click)="close()">
            <i class="ti ti-x" aria-hidden="true"></i>
          </button>
        </div>

        <div class="grid gap-5 overflow-y-auto px-6 py-5 sm:grid-cols-2">
          <div>
            <label for="arrivalIcao" class="field-label">Arrival airport</label>
            <input id="arrivalIcao" formControlName="arrivalIcao" maxlength="4" class="field-input font-mono uppercase" />
            <p class="mt-1 text-xs text-ink-muted">Change it only if you diverted.</p>
          </div>

          <div>
            <label for="network" class="field-label">Network</label>
            <select id="network" formControlName="network" class="field-input">
              @for (n of networkOptions; track n) {
                <option [value]="n">{{ networks[n] }}</option>
              }
            </select>
          </div>

          <div>
            <label for="blockOff" class="field-label">Block off (UTC)</label>
            <app-date-time-picker inputId="blockOff" formControlName="blockOffAt" [utc]="true" />
          </div>
          <div>
            <label for="blockOn" class="field-label">Block on (UTC)</label>
            <app-date-time-picker inputId="blockOn" formControlName="blockOnAt" [utc]="true" />
          </div>
          <div>
            <label for="takeoff" class="field-label">Takeoff (UTC)</label>
            <app-date-time-picker inputId="takeoff" formControlName="takeoffAt" [utc]="true" />
          </div>
          <div>
            <label for="landing" class="field-label">Landing (UTC)</label>
            <app-date-time-picker inputId="landing" formControlName="landingAt" [utc]="true" />
          </div>

          <div>
            <label for="landingRate" class="field-label">Landing rate (fpm)</label>
            <input id="landingRate" type="number" formControlName="landingRateFpm" class="field-input" placeholder="e.g. 140" />
          </div>
          <div>
            <label for="fuel" class="field-label">Fuel used (kg)</label>
            <input id="fuel" type="number" formControlName="fuelUsedKg" class="field-input" />
          </div>

          <div class="sm:col-span-2">
            <label for="route" class="field-label">Route</label>
            <input id="route" formControlName="flightRoute" maxlength="1000" class="field-input font-mono" />
          </div>

          <div class="sm:col-span-2">
            <label for="comments" class="field-label">Comments</label>
            <textarea id="comments" formControlName="comments" rows="4" maxlength="1000" class="field-input"
              placeholder="Why the ACARS could not file this flight, and anything the reviewer should know."></textarea>
            @if (form.controls.comments.invalid && form.controls.comments.touched) {
              <p class="field-error">Explain why the flight is being filed manually (at least 10 characters).</p>
            }
          </div>
        </div>

        @if (formError(); as message) {
          <p class="border-t border-danger/30 bg-danger/10 px-6 py-3 text-sm text-danger">
            <i class="ti ti-alert-circle mr-1" aria-hidden="true"></i>{{ message }}
          </p>
        }

        <div class="flex justify-end gap-2 border-t border-line px-6 py-4">
          <button type="button" class="btn-secondary" (click)="close()">Cancel</button>
          <button type="submit" class="btn-primary px-5" [disabled]="saving()">{{ saving() ? 'Loading...' : 'File PIREP' }}</button>
        </div>
      </form>
    </div>
  `
})
export class ManualPirepModal implements OnInit {
  private readonly api = inject(PirepService);

  readonly booking = input.required<Booking>();
  readonly filed = output<string>();
  readonly closed = output<void>();

  protected readonly networks = NETWORK_LABELS;
  protected readonly networkOptions: FlightNetwork[] = ['Vatsim', 'Ivao', 'Offline'];
  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    arrivalIcao: ['', [Validators.required, Validators.pattern(/^[A-Za-z0-9]{4}$/)]],
    network: ['Vatsim' as FlightNetwork],
    blockOffAt: ['', Validators.required],
    blockOnAt: [utcInput(new Date()), Validators.required],
    takeoffAt: [''],
    landingAt: [''],
    landingRateFpm: [null as number | null],
    fuelUsedKg: [null as number | null],
    flightRoute: [''],
    comments: ['', [Validators.required, Validators.minLength(10)]]
  });

  ngOnInit(): void {
    const booking = this.booking();
    this.form.patchValue({
      arrivalIcao: booking.arrival.icao,
      blockOffAt: utcInput(new Date(Date.now() - (booking.estimatedMinutes + 10) * 60_000))
    });
  }

  protected close(): void {
    if (!this.saving()) this.closed.emit();
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const blockOffAt = toUtcIso(v.blockOffAt);
    const blockOnAt = toUtcIso(v.blockOnAt);

    if (!blockOffAt || !blockOnAt) {
      this.formError.set('Enter the block off and block on times.');
      return;
    }

    this.saving.set(true);
    this.formError.set(null);

    try {
      const pirep = await this.api.fileManual({
        arrivalIcao: v.arrivalIcao.trim().toUpperCase(),
        network: v.network,
        blockOffAt,
        blockOnAt,
        takeoffAt: toUtcIso(v.takeoffAt),
        landingAt: toUtcIso(v.landingAt),
        landingRateFpm: v.landingRateFpm === null || `${v.landingRateFpm}` === '' ? null : Math.abs(Number(v.landingRateFpm)),
        fuelUsedKg: v.fuelUsedKg === null || `${v.fuelUsedKg}` === '' ? null : Number(v.fuelUsedKg),
        flightRoute: v.flightRoute.trim() || null,
        comments: v.comments.trim()
      });
      this.saving.set(false);
      this.filed.emit(pirep.summary.id);
    } catch (error) {
      this.formError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.saving.set(false);
    }
  }
}