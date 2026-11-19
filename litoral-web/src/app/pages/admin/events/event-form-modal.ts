import { Component, OnDestroy, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { apiErrorMessage } from '../../../core/api/api-error';
import { AdminEvent } from '../../../core/events/events.models';
import { EventsService } from '../../../core/events/events.service';
import { DateTimePicker } from '../../../shared/date-time-picker/date-time-picker';

const IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
const MAX_IMAGE_BYTES = 5 * 1024 * 1024;

function utcInput(date: Date): string {
  return date.toISOString().slice(0, 16);
}

function toUtcIso(value: string): string | null {
  if (!value) return null;
  const date = new Date(`${value}:00Z`);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

@Component({
  selector: 'app-event-form-modal',
  imports: [ReactiveFormsModule, DateTimePicker],
  host: { '(document:keydown.escape)': 'close()' },
  template: `
    <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/50 p-4" (click)="close()">
      <form [formGroup]="form" (ngSubmit)="save()" novalidate (click)="$event.stopPropagation()"
        class="panel flex max-h-[92vh] w-full max-w-2xl flex-col overflow-hidden shadow-2xl">
        <div class="flex items-center justify-between border-b border-line px-6 py-4">
          <h2 class="text-lg font-bold text-ink">{{ item() ? 'Edit event' : 'New event' }}</h2>
          <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Cancel" (click)="close()">
            <i class="ti ti-x" aria-hidden="true"></i>
          </button>
        </div>

        <div class="grid gap-5 overflow-y-auto px-6 py-5 sm:grid-cols-2">
          <div class="sm:col-span-2 flex flex-col gap-2">
            <span class="field-label">Banner</span>
            <div class="relative flex h-32 items-center justify-center overflow-hidden rounded-xl bg-surface-alt bg-cover bg-center"
              [style.background-image]="previewUrl() ? 'url(' + previewUrl() + ')' : null">
              @if (!previewUrl()) {
                <i class="ti ti-photo text-4xl text-ink-muted" aria-hidden="true"></i>
              }
            </div>
            <div class="flex flex-wrap gap-2">
              <label class="btn-secondary flex cursor-pointer items-center gap-2">
                <i class="ti ti-upload" aria-hidden="true"></i>{{ previewUrl() ? 'Change banner' : 'Upload banner' }}
                <input type="file" accept="image/jpeg,image/png,image/webp" class="hidden" (change)="onFileSelected($event)" />
              </label>
              @if (previewUrl()) {
                <button type="button" class="btn-secondary flex items-center gap-2" (click)="removeImage()">
                  <i class="ti ti-trash" aria-hidden="true"></i>Remove
                </button>
              }
            </div>
            @if (imageError(); as message) {
              <p class="field-error">{{ message }}</p>
            }
          </div>

          <div class="sm:col-span-2">
            <label for="eTitle" class="field-label">Title</label>
            <input id="eTitle" formControlName="title" maxlength="100" class="field-input" placeholder="e.g. Florianópolis Fly-in" />
          </div>

          <div class="sm:col-span-2">
            <label for="eDescription" class="field-label">Description</label>
            <textarea id="eDescription" formControlName="description" rows="4" maxlength="2000" class="field-input"
              placeholder="Network event, ATC coverage, suggested routes..."></textarea>
          </div>

          <div>
            <label for="eStart" class="field-label">Start (UTC)</label>
            <app-date-time-picker inputId="eStart" formControlName="startsAt" [utc]="true" />
          </div>
          <div>
            <label for="eEnd" class="field-label">End (UTC)</label>
            <app-date-time-picker inputId="eEnd" formControlName="endsAt" [utc]="true" [min]="form.controls.startsAt.value || null" />
          </div>

          <div>
            <label for="eAirports" class="field-label">Airports</label>
            <input id="eAirports" formControlName="airports" class="field-input font-mono uppercase" placeholder="SBFL, SBGR" />
            <p class="mt-1 text-xs text-ink-muted">Flights departing from or arriving at one of them count. Empty means any airport.</p>
          </div>

          <div>
            <label for="eBonus" class="field-label">Bonus per flight (points)</label>
            <input id="eBonus" type="number" min="0" step="50" formControlName="bonusPoints" class="field-input" />
            <p class="mt-1 text-xs text-ink-muted">Paid when the PIREP is approved. The block-off time must be inside the window.</p>
          </div>

          <label class="sm:col-span-2 flex cursor-pointer items-center gap-3">
            <input type="checkbox" formControlName="isPublished" class="h-4 w-4 accent-primary" />
            <span>
              <span class="block font-medium text-ink">Published</span>
              <span class="block text-xs text-ink-muted">Only published events pay the bonus. The first time it's published, every pilot gets a notification.</span>
            </span>
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
export class EventFormModal implements OnInit, OnDestroy {
  private readonly api = inject(EventsService);

  readonly item = input<AdminEvent | null>(null);
  readonly saved = output<AdminEvent>();
  readonly closed = output<void>();

  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly imageError = signal<string | null>(null);
  private readonly imageFile = signal<File | null>(null);
  private readonly imageRemoved = signal(false);
  private readonly localPreview = signal<string | null>(null);

  protected readonly previewUrl = computed(() => {
    if (this.localPreview()) return this.localPreview();
    if (this.imageRemoved()) return null;
    return this.item()?.imageUrl ?? null;
  });

  protected readonly form = inject(FormBuilder).nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.required, Validators.maxLength(2000)]],
    startsAt: ['', Validators.required],
    endsAt: ['', Validators.required],
    airports: [''],
    bonusPoints: [300, [Validators.required, Validators.min(0)]],
    isPublished: [true]
  });

  ngOnInit(): void {
    const item = this.item();
    if (item) {
      this.form.setValue({
        title: item.title,
        description: item.description,
        startsAt: item.startsAt.slice(0, 16),
        endsAt: item.endsAt.slice(0, 16),
        airports: item.airports.join(', '),
        bonusPoints: item.bonusPoints,
        isPublished: item.isPublished
      });
    } else {
      const start = new Date();
      start.setUTCDate(start.getUTCDate() + 7);
      start.setUTCHours(21, 0, 0, 0);
      const end = new Date(start.getTime() + 3 * 60 * 60 * 1000);
      this.form.patchValue({ startsAt: utcInput(start), endsAt: utcInput(end) });
    }
  }

  ngOnDestroy(): void {
    this.revokePreview();
  }

  protected onFileSelected(event: Event): void {
    const element = event.target as HTMLInputElement;
    const file = element.files?.[0];
    element.value = '';
    if (!file) return;

    if (!IMAGE_TYPES.includes(file.type)) {
      this.imageError.set('Use a JPG, PNG or WEBP image.');
      return;
    }
    if (file.size > MAX_IMAGE_BYTES) {
      this.imageError.set('The image must be 5 MB or smaller.');
      return;
    }

    this.imageError.set(null);
    this.revokePreview();
    this.imageFile.set(file);
    this.imageRemoved.set(false);
    this.localPreview.set(URL.createObjectURL(file));
  }

  protected removeImage(): void {
    this.revokePreview();
    this.imageFile.set(null);
    this.imageRemoved.set(true);
  }

  protected close(): void {
    if (!this.saving()) this.closed.emit();
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError.set('Fill in the title, description, window and bonus.');
      return;
    }

    const v = this.form.getRawValue();
    const startsAt = toUtcIso(v.startsAt);
    const endsAt = toUtcIso(v.endsAt);
    if (!startsAt || !endsAt || endsAt <= startsAt) {
      this.formError.set('The end must be after the start.');
      return;
    }

    const airports = v.airports.split(/[\s,;]+/).map(a => a.trim().toUpperCase()).filter(Boolean);
    if (airports.some(a => a.length !== 4)) {
      this.formError.set('Use 4-letter ICAO codes separated by commas.');
      return;
    }

    this.saving.set(true);
    this.formError.set(null);

    try {
      const payload = {
        title: v.title.trim(),
        description: v.description.trim(),
        startsAt,
        endsAt,
        airports,
        bonusPoints: Number(v.bonusPoints) || 0,
        isPublished: v.isPublished
      };
      const current = this.item();
      let result = current ? await this.api.update(current.id, payload) : await this.api.create(payload);

      const file = this.imageFile();
      if (file) result = await this.api.uploadImage(result.id, file);
      else if (this.imageRemoved() && result.imageUrl) result = await this.api.removeImage(result.id);

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