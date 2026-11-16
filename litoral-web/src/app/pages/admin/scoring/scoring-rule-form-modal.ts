import { Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminScoringService } from '../../../core/admin/admin-scoring.service';
import { apiErrorMessage } from '../../../core/api/api-error';
import { ScoringCatalog, ScoringMode, ScoringOutcome, ScoringRule, ScoringRulePayload } from '../../../core/scoring/scoring.models';

const MODE_LABELS: Record<ScoringMode, string> = {
  Once: 'Once per flight',
  PerOccurrence: 'Per occurrence',
  PerSecond: 'Per second',
  PerDay: 'Per day'
};

const SOURCE_LABELS: Record<string, string> = {
  Acars: 'Detected by the ACARS',
  Review: 'Applied in the review',
  Flight: 'Flight type',
  Pilot: 'Pilot account'
};

function toNumber(value: string | number | null): number | null {
  if (value === null || value === '') return null;
  const n = Number(value);
  return Number.isFinite(n) ? n : null;
}

@Component({
  selector: 'app-scoring-rule-form-modal',
  imports: [ReactiveFormsModule],
  host: { '(document:keydown.escape)': 'close()' },
  template: `
    <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/50 p-4" (click)="close()">
      <form [formGroup]="form" (ngSubmit)="save()" novalidate (click)="$event.stopPropagation()"
        class="panel flex max-h-[90vh] w-full max-w-2xl flex-col overflow-hidden shadow-2xl">
        <div class="flex items-center justify-between border-b border-line px-6 py-4">
          <h2 class="text-lg font-bold text-ink">{{ rule() ? 'Edit rule' : 'New rule' }}</h2>
          <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Cancel" (click)="close()">
            <i class="ti ti-x" aria-hidden="true"></i>
          </button>
        </div>

        <div class="grid gap-5 overflow-y-auto px-6 py-5 sm:grid-cols-2">
          <div>
            <label for="category" class="field-label">Category</label>
            <select id="category" formControlName="category" class="field-input">
              @for (c of catalog().categories; track c.category) {
                <option [value]="c.category">{{ c.title }}</option>
              }
            </select>
          </div>

          <div>
            <label for="check" class="field-label">Check</label>
            <select id="check" formControlName="check" class="field-input" (change)="onCheckChange()">
              @for (group of checkGroups(); track group.source) {
                <optgroup [label]="group.label">
                  @for (c of group.checks; track c.check) {
                    <option [value]="c.check">{{ c.label }}</option>
                  }
                </optgroup>
              }
            </select>
          </div>

          <div class="sm:col-span-2">
            <label for="description" class="field-label">Description</label>
            <input id="description" formControlName="description" maxlength="200" class="field-input"
              placeholder="Shown to the pilots, e.g. Flaps 1 above 250 kt" />
            @if (form.controls.description.invalid && form.controls.description.touched) {
              <p class="field-error">Enter the rule description.</p>
            }
          </div>

          <fieldset class="sm:col-span-2">
            <legend class="field-label">Outcome</legend>
            <div class="grid gap-2 sm:grid-cols-2">
              @for (option of outcomes; track option.value) {
                <label class="flex cursor-pointer items-start gap-3 rounded-lg border px-4 py-3"
                  [class]="outcome() === option.value ? 'border-primary bg-primary/5' : 'border-line hover:bg-surface-alt'">
                  <input type="radio" formControlName="outcome" [value]="option.value" class="mt-1 accent-primary" />
                  <span>
                    <span class="block font-medium text-ink">{{ option.label }}</span>
                    <span class="block text-xs text-ink-muted">{{ option.hint }}</span>
                  </span>
                </label>
              }
            </div>
          </fieldset>

          @if (outcome() === 'Points') {
            <div>
              <label for="points" class="field-label">Points</label>
              <input id="points" type="number" formControlName="points" class="field-input" step="1" />
              <p class="mt-1 text-xs text-ink-muted">Use a negative value for penalties.</p>
            </div>
          }

          <div>
            <label for="mode" class="field-label">Applied</label>
            <select id="mode" formControlName="mode" class="field-input">
              @for (mode of modes(); track mode) {
                <option [value]="mode">{{ modeLabel(mode) }}</option>
              }
            </select>
          </div>

          @if (check()?.minLabel; as label) {
            <div>
              <label for="min" class="field-label">{{ label }}</label>
              <input id="min" type="number" formControlName="min" class="field-input" />
            </div>
          }

          @if (check()?.maxLabel; as label) {
            <div>
              <label for="max" class="field-label">{{ label }}</label>
              <input id="max" type="number" formControlName="max" class="field-input" />
            </div>
          }

          @if (check()?.textLabel; as label) {
            <div class="sm:col-span-2">
              <label for="text" class="field-label">{{ label }}</label>
              <input id="text" formControlName="text" maxlength="100" class="field-input" />
              <p class="mt-1 text-xs text-ink-muted">Separate multiple values with commas.</p>
            </div>
          }

          <div>
            <label for="aircraftTypes" class="field-label">Aircraft types</label>
            <input id="aircraftTypes" formControlName="aircraftTypes" class="field-input uppercase" placeholder="All types" />
            <p class="mt-1 text-xs text-ink-muted">ICAO codes, e.g. B738,B739. Empty applies to all.</p>
          </div>

          <div>
            <label for="graceSeconds" class="field-label">Grace period (s)</label>
            <input id="graceSeconds" type="number" min="0" max="600" formControlName="graceSeconds" class="field-input" />
            <p class="mt-1 text-xs text-ink-muted">Applied only after this time. 0 applies immediately.</p>
          </div>

          <div>
            <label for="order" class="field-label">Order</label>
            <input id="order" type="number" formControlName="order" class="field-input" />
          </div>

          <label class="flex cursor-pointer items-center gap-3 self-end pb-3">
            <input type="checkbox" formControlName="isActive" class="h-4 w-4 accent-primary" />
            <span class="font-medium text-ink">Active</span>
          </label>
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
export class ScoringRuleFormModal implements OnInit {
  private readonly api = inject(AdminScoringService);

  readonly catalog = input.required<ScoringCatalog>();
  readonly rule = input<ScoringRule | null>(null);
  readonly category = input<string | null>(null);
  readonly saved = output<ScoringRule>();
  readonly closed = output<void>();

  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected readonly outcomes: { value: ScoringOutcome; label: string; hint: string }[] = [
    { value: 'Points', label: 'Points', hint: 'Adds or removes points from the flight.' },
    { value: 'Reject', label: 'Rejection', hint: 'The flight is rejected. Hours still count.' }
  ];

  protected readonly form = inject(FormBuilder).group({
    category: ['', Validators.required],
    check: ['', Validators.required],
    description: ['', [Validators.required, Validators.maxLength(200)]],
    outcome: ['Points' as ScoringOutcome],
    points: [0 as number | null],
    mode: ['Once' as ScoringMode],
    min: [null as number | null],
    max: [null as number | null],
    text: [''],
    aircraftTypes: [''],
    graceSeconds: [0 as number | null],
    order: [0 as number | null],
    isActive: [true]
  });

  private readonly checkValue = toSignal(this.form.controls.check.valueChanges, { initialValue: '' });
  protected readonly outcome = toSignal(this.form.controls.outcome.valueChanges, { initialValue: 'Points' as ScoringOutcome });

  protected readonly check = computed(() => this.catalog().checks.find(c => c.check === this.checkValue()) ?? null);
  protected readonly modes = computed<ScoringMode[]>(() => this.check()?.modes ?? ['Once']);

  protected readonly checkGroups = computed(() =>
    Object.entries(SOURCE_LABELS)
      .map(([source, label]) => ({ source, label, checks: this.catalog().checks.filter(c => c.source === source) }))
      .filter(g => g.checks.length > 0));

  ngOnInit(): void {
    const rule = this.rule();

    if (rule) {
      this.form.setValue({
        category: rule.category,
        check: rule.check,
        description: rule.description,
        outcome: rule.outcome,
        points: rule.points,
        mode: rule.mode,
        min: rule.min,
        max: rule.max,
        text: rule.text ?? '',
        aircraftTypes: rule.aircraftTypes ?? '',
        graceSeconds: rule.graceSeconds,
        order: rule.order,
        isActive: rule.isActive
      });
    } else {
      this.form.patchValue({
        category: this.category() ?? this.catalog().categories[0]?.category ?? '',
        check: this.catalog().checks[0]?.check ?? ''
      });
    }
  }

  protected modeLabel(mode: ScoringMode): string {
    return MODE_LABELS[mode];
  }

  protected onCheckChange(): void {
    const modes = this.modes();
    if (!modes.includes(this.form.controls.mode.value as ScoringMode)) {
      this.form.controls.mode.setValue(modes[0]);
    }
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
    const check = this.check();
    const payload: ScoringRulePayload = {
      category: v.category ?? '',
      check: v.check ?? '',
      description: (v.description ?? '').trim(),
      outcome: v.outcome ?? 'Points',
      points: v.outcome === 'Reject' ? 0 : toNumber(v.points) ?? 0,
      mode: v.mode ?? 'Once',
      min: check?.minLabel ? toNumber(v.min) : null,
      max: check?.maxLabel ? toNumber(v.max) : null,
      text: check?.textLabel ? (v.text ?? '').trim() || null : null,
      aircraftTypes: (v.aircraftTypes ?? '').trim().toUpperCase() || null,
      graceSeconds: toNumber(v.graceSeconds) ?? 0,
      order: toNumber(v.order) ?? 0,
      isActive: v.isActive ?? true
    };

    this.saving.set(true);
    this.formError.set(null);

    try {
      const rule = this.rule();
      const result = rule ? await this.api.update(rule.id, payload) : await this.api.create(payload);
      this.saving.set(false);
      this.saved.emit(result);
    } catch (error) {
      this.formError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.saving.set(false);
    }
  }
}