import { Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { apiErrorMessage } from '../../../core/api/api-error';
import { MEL_ACARS_CHECKS, MelAcarsCheck, MelCategory, MelItem, MelKind, MelVerification } from '../../../core/mel/mel.models';
import { MelService } from '../../../core/mel/mel.service';

@Component({
  selector: 'app-mel-item-form-modal',
  imports: [ReactiveFormsModule],
  host: { '(document:keydown.escape)': 'close()' },
  template: `
    <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/50 p-4" (click)="close()">
      <form [formGroup]="form" (ngSubmit)="save()" novalidate (click)="$event.stopPropagation()"
        class="panel flex max-h-[92vh] w-full max-w-3xl flex-col overflow-hidden shadow-2xl">
        <div class="flex items-center justify-between border-b border-line px-6 py-4">
          <h2 class="text-lg font-bold text-ink">{{ item() ? 'Edit item' : 'New item' }}</h2>
          <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Cancel" (click)="close()">
            <i class="ti ti-x" aria-hidden="true"></i>
          </button>
        </div>

        <div class="grid gap-5 overflow-y-auto px-6 py-5 sm:grid-cols-4">
          <fieldset class="sm:col-span-2">
            <legend class="field-label">Type</legend>
            <div class="flex rounded-lg border border-line p-0.5 text-sm font-semibold">
              @for (k of kinds; track k.value) {
                <button type="button" class="flex-1 rounded-md px-3 py-1.5"
                  [class]="kind() === k.value ? 'bg-primary text-on-primary' : 'text-ink-muted hover:text-ink'" (click)="form.controls.kind.setValue(k.value)">
                  {{ k.label }}
                </button>
              }
            </div>
          </fieldset>

          <div>
            <label for="mCode" class="field-label">Code</label>
            <input id="mCode" formControlName="code" maxlength="12" class="field-input font-mono uppercase" placeholder="21-01" />
          </div>

          <div>
            <label for="mCategory" class="field-label">Category</label>
            <select id="mCategory" formControlName="category" class="field-input">
              @for (c of categories; track c.value) {
                <option [value]="c.value">{{ c.label }}</option>
              }
            </select>
          </div>

          <div class="sm:col-span-3">
            <label for="mTitle" class="field-label">Title</label>
            <input id="mTitle" formControlName="title" maxlength="120" class="field-input" placeholder="e.g. One air conditioning pack inoperative" />
          </div>

          @if (category() === 'A') {
            <div>
              <label for="mDays" class="field-label">Deadline (days)</label>
              <input id="mDays" type="number" min="1" max="30" formControlName="categoryADays" class="field-input" />
            </div>
          }

          <div class="sm:col-span-4">
            <label for="mRestriction" class="field-label">Restriction</label>
            <textarea id="mRestriction" formControlName="restriction" rows="2" maxlength="600" class="field-input"
              placeholder="What the crew can not do, e.g. Maximum altitude FL250."></textarea>
          </div>

          <div class="sm:col-span-4">
            <label for="mProcedure" class="field-label">Procedure</label>
            <textarea id="mProcedure" formControlName="procedure" rows="3" maxlength="600" class="field-input"
              placeholder="What the crew must do, in Litoral's own words."></textarea>
          </div>

          <div class="sm:col-span-2">
            <label for="mVerification" class="field-label">Verification</label>
            <select id="mVerification" formControlName="verification" class="field-input">
              <option value="Acars">ACARS</option>
              <option value="Safety">Safety review</option>
              <option value="Info">Informative (no check)</option>
            </select>
          </div>

          @if (verification() === 'Acars') {
            <div class="sm:col-span-2">
              <label for="mCheck" class="field-label">ACARS checks</label>
              <select id="mCheck" formControlName="acarsCheck" class="field-input">
                @for (c of checks; track c.value) {
                  <option [value]="c.value">{{ c.label }}{{ c.live ? '' : ' (Safety until the ACARS sends it)' }}</option>
                }
              </select>
            </div>
            @if (acarsCheck() === 'MaxAltitude') {
              <div class="sm:col-span-2">
                <label for="mValue" class="field-label">Maximum altitude (ft)</label>
                <input id="mValue" type="number" min="5000" max="45000" step="1000" formControlName="checkValue" class="field-input" />
              </div>
            }
            @if (acarsCheck() === 'RemarksContain') {
              <div class="sm:col-span-2">
                <label for="mText" class="field-label">Required remarks</label>
                <input id="mText" formControlName="checkText" maxlength="40" class="field-input font-mono uppercase" placeholder="TCAS INOP" />
              </div>
            }
          }

          <div class="sm:col-span-2">
            <label for="mFuel" class="field-label">Fuel penalty (%)</label>
            <input id="mFuel" type="number" min="0" max="20" step="0.5" formControlName="fuelPenaltyPercent" class="field-input" placeholder="0" />
          </div>

          <fieldset class="sm:col-span-4">
            <legend class="field-label">Aircraft types</legend>
            <div class="flex flex-wrap gap-2">
              @for (code of aircraftTypes(); track code) {
                <label class="flex cursor-pointer items-center gap-2 rounded-lg border px-3 py-2"
                  [class]="types().includes(code) ? 'border-primary bg-primary/5' : 'border-line hover:bg-surface-alt'">
                  <input type="checkbox" class="accent-primary" [checked]="types().includes(code)" (change)="toggleType(code)" />
                  <span class="font-mono text-sm font-bold text-ink">{{ code }}</span>
                </label>
              }
            </div>
            <p class="mt-1 text-xs text-ink-muted">None selected means every type. The 800BCF uses the B738 code.</p>
          </fieldset>

          <div class="sm:col-span-4 flex flex-col gap-3">
            <label class="flex cursor-pointer items-center gap-3" [class.opacity-50]="!canAuto()">
              <input type="checkbox" formControlName="autoEligible" class="h-4 w-4 accent-primary" [attr.disabled]="canAuto() ? null : true" />
              <span>
                <span class="block font-medium text-ink">Can be opened by the system</span>
                <span class="block text-xs text-ink-muted">Only MEL items of category C or D. The system opens them at random after flights.</span>
              </span>
            </label>
            <label class="flex cursor-pointer items-center gap-3">
              <input type="checkbox" formControlName="isActive" class="h-4 w-4 accent-primary" />
              <span class="font-medium text-ink">Active</span>
            </label>
          </div>
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
export class MelItemFormModal implements OnInit {
  private readonly api = inject(MelService);

  readonly item = input<MelItem | null>(null);
  readonly aircraftTypes = input.required<string[]>();
  readonly saved = output<MelItem>();
  readonly closed = output<void>();

  protected readonly kinds: { value: MelKind; label: string }[] = [
    { value: 'Mel', label: 'MEL' },
    { value: 'Cdl', label: 'CDL' }
  ];
  protected readonly categories: { value: MelCategory; label: string }[] = [
    { value: 'A', label: 'A · custom' },
    { value: 'B', label: 'B · 3 days' },
    { value: 'C', label: 'C · 10 days' },
    { value: 'D', label: 'D · 120 days' }
  ];
  protected readonly checks = MEL_ACARS_CHECKS;

  protected readonly types = signal<string[]>([]);
  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    kind: ['Mel' as MelKind],
    code: ['', [Validators.required, Validators.maxLength(12)]],
    category: ['C' as MelCategory],
    categoryADays: [1 as number | null],
    title: ['', [Validators.required, Validators.maxLength(120)]],
    restriction: ['', [Validators.required, Validators.maxLength(600)]],
    procedure: ['', [Validators.required, Validators.maxLength(600)]],
    verification: ['Safety' as MelVerification],
    acarsCheck: ['MaxAltitude' as MelAcarsCheck],
    checkValue: [25000 as number | null],
    checkText: [''],
    fuelPenaltyPercent: [null as number | null],
    autoEligible: [false],
    isActive: [true]
  });

  protected readonly kind = toSignal(this.form.controls.kind.valueChanges, { initialValue: 'Mel' as MelKind });
  protected readonly category = toSignal(this.form.controls.category.valueChanges, { initialValue: 'C' as MelCategory });
  protected readonly verification = toSignal(this.form.controls.verification.valueChanges, { initialValue: 'Safety' as MelVerification });
  protected readonly acarsCheck = toSignal(this.form.controls.acarsCheck.valueChanges, { initialValue: 'MaxAltitude' as MelAcarsCheck });
  protected readonly canAuto = computed(() => this.kind() === 'Mel' && (this.category() === 'C' || this.category() === 'D'));

  ngOnInit(): void {
    const item = this.item();
    if (!item) return;

    this.form.setValue({
      kind: item.kind,
      code: item.code,
      category: item.category,
      categoryADays: item.categoryADays ?? 1,
      title: item.title,
      restriction: item.restriction,
      procedure: item.procedure,
      verification: item.verification,
      acarsCheck: item.acarsCheck === 'None' ? 'MaxAltitude' : item.acarsCheck,
      checkValue: item.checkValue ?? 25000,
      checkText: item.checkText ?? '',
      fuelPenaltyPercent: item.fuelPenaltyPercent,
      autoEligible: item.autoEligible,
      isActive: item.isActive
    });
    this.types.set([...item.aircraftTypes]);
  }

  protected toggleType(code: string): void {
    this.types.update(list => (list.includes(code) ? list.filter(c => c !== code) : [...list, code]));
  }

  protected close(): void {
    if (!this.saving()) this.closed.emit();
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError.set('Fill in the code, title, restriction and procedure.');
      return;
    }

    const v = this.form.getRawValue();
    const acars = v.verification === 'Acars';

    this.saving.set(true);
    this.formError.set(null);

    try {
      const payload = {
        kind: v.kind,
        code: v.code.trim().toUpperCase(),
        category: v.category,
        categoryADays: v.category === 'A' ? Number(v.categoryADays) || null : null,
        title: v.title.trim(),
        restriction: v.restriction.trim(),
        procedure: v.procedure.trim(),
        verification: v.verification,
        acarsCheck: acars ? v.acarsCheck : 'None' as MelAcarsCheck,
        checkValue: acars && v.acarsCheck === 'MaxAltitude' ? Number(v.checkValue) || null : null,
        checkText: acars && v.acarsCheck === 'RemarksContain' ? v.checkText.trim().toUpperCase() || null : null,
        fuelPenaltyPercent: v.fuelPenaltyPercent ? Number(v.fuelPenaltyPercent) : null,
        aircraftTypes: this.types(),
        autoEligible: this.canAuto() && v.autoEligible,
        isActive: v.isActive
      };
      const current = this.item();
      this.saved.emit(current ? await this.api.update(current.id, payload) : await this.api.create(payload));
    } catch (error) {
      this.formError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.saving.set(false);
    }
  }
}