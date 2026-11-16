import { Component, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminAircraft, AdminFleetService } from '../../../core/admin/admin-fleet.service';
import { apiErrorMessage } from '../../../core/api/api-error';
import { DateTimePicker } from '../../../shared/date-time-picker/date-time-picker';

const DEFAULT_DAYS = 2;

type ReleaseMode = 'manual' | 'date';

function toLocalInput(date: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

@Component({
  selector: 'app-maintenance-modal',
  imports: [ReactiveFormsModule, DateTimePicker],
  host: { '(document:keydown.escape)': 'close()' },
  template: `
    <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/50 p-4" (click)="close()">
      <form [formGroup]="form" (ngSubmit)="save()" novalidate (click)="$event.stopPropagation()"
        class="panel flex max-h-[90vh] w-full max-w-lg flex-col overflow-hidden shadow-2xl">
        <div class="flex items-center justify-between border-b border-line px-6 py-4">
          <h2 class="text-lg font-bold text-ink">Send {{ aircraft().registration }} to maintenance</h2>
          <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Cancel" (click)="close()">
            <i class="ti ti-x" aria-hidden="true"></i>
          </button>
        </div>

        <div class="grid gap-5 overflow-y-auto px-6 py-5">
          <div>
            <label for="reason" class="field-label">Reason</label>
            <textarea id="reason" formControlName="reason" rows="3" maxlength="300" class="field-input"
              placeholder="e.g. A-check, bird strike inspection"></textarea>
            @if (form.controls.reason.invalid && form.controls.reason.touched) {
              <p class="field-error">Enter the maintenance reason.</p>
            }
          </div>

          <fieldset>
            <legend class="field-label">Return to service</legend>
            <div class="grid gap-2">
              @for (option of modes; track option.value) {
                <label class="flex cursor-pointer items-start gap-3 rounded-lg border px-4 py-3"
                  [class]="form.controls.mode.value === option.value ? 'border-primary bg-primary/5' : 'border-line hover:bg-surface-alt'">
                  <input type="radio" formControlName="mode" [value]="option.value" class="mt-1 accent-primary" />
                  <span>
                    <span class="block font-medium text-ink">{{ option.label }}</span>
                    <span class="block text-xs text-ink-muted">{{ option.hint }}</span>
                  </span>
                </label>
              }
            </div>
          </fieldset>

          @if (form.controls.mode.value === 'date') {
            <div>
              <label for="until" class="field-label">Back in service at</label>
              <app-date-time-picker inputId="until" formControlName="until" [min]="minDate" class="sm:w-2/3" />
              <p class="mt-1 text-xs text-ink-muted">Your local time. The aircraft is released automatically.</p>
            </div>
          }
        </div>

        @if (formError(); as message) {
          <p class="border-t border-danger/30 bg-danger/10 px-6 py-3 text-sm text-danger">
            <i class="ti ti-alert-circle mr-1" aria-hidden="true"></i>{{ message }}
          </p>
        }

        <div class="flex justify-end gap-2 border-t border-line px-6 py-4">
          <button type="button" class="btn-secondary" (click)="close()">Cancel</button>
          <button type="submit" class="btn-primary px-5" [disabled]="saving()">{{ saving() ? 'Loading...' : 'Send to maintenance' }}</button>
        </div>
      </form>
    </div>
  `
})
export class MaintenanceModal {
  private readonly api = inject(AdminFleetService);

  readonly aircraft = input.required<AdminAircraft>();
  readonly saved = output<AdminAircraft>();
  readonly closed = output<void>();

  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly minDate = toLocalInput(new Date());

  protected readonly modes: { value: ReleaseMode; label: string; hint: string }[] = [
    { value: 'date', label: 'On a date', hint: 'The aircraft returns to service by itself at the chosen time.' },
    { value: 'manual', label: 'Manually', hint: 'The aircraft stays in maintenance until the staff releases it.' }
  ];

  protected readonly form = inject(FormBuilder).nonNullable.group({
    reason: ['', [Validators.required, Validators.maxLength(300)]],
    mode: ['date' as ReleaseMode],
    until: [toLocalInput(new Date(Date.now() + DEFAULT_DAYS * 24 * 60 * 60 * 1000))]
  });

  protected close(): void {
    if (!this.saving()) this.closed.emit();
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { reason, mode, until } = this.form.getRawValue();
    let untilIso: string | null = null;

    if (mode === 'date') {
      const date = new Date(until);
      if (!until || Number.isNaN(date.getTime()) || date.getTime() <= Date.now()) {
        this.formError.set('Choose a date in the future.');
        return;
      }
      untilIso = date.toISOString();
    }

    this.saving.set(true);
    this.formError.set(null);

    try {
      const result = await this.api.sendToMaintenance(this.aircraft().id, reason.trim(), untilIso);
      this.saving.set(false);
      this.saved.emit(result);
    } catch (error) {
      this.formError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.saving.set(false);
    }
  }
}