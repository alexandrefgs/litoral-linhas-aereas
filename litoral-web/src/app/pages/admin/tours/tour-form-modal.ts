import { Component, OnDestroy, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { apiErrorMessage } from '../../../core/api/api-error';
import { AdminTour, TourOptions } from '../../../core/tours/tours.models';
import { ToursService } from '../../../core/tours/tours.service';
import { DateTimePicker } from '../../../shared/date-time-picker/date-time-picker';

const IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
const MAX_IMAGE_BYTES = 5 * 1024 * 1024;
const MAX_LEGS = 30;

function toUtcIso(value: string): string | null {
  if (!value) return null;
  const date = new Date(`${value}:00Z`);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

@Component({
  selector: 'app-tour-form-modal',
  imports: [ReactiveFormsModule, DateTimePicker],
  host: { '(document:keydown.escape)': 'close()' },
  template: `
    <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/50 p-4" (click)="close()">
      <form [formGroup]="form" (ngSubmit)="save()" novalidate (click)="$event.stopPropagation()"
        class="panel flex max-h-[92vh] w-full max-w-3xl flex-col overflow-hidden shadow-2xl">
        <div class="flex items-center justify-between border-b border-line px-6 py-4">
          <h2 class="text-lg font-bold text-ink">{{ tour() ? 'Edit tour' : 'New tour' }}</h2>
          <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Cancel" (click)="close()">
            <i class="ti ti-x" aria-hidden="true"></i>
          </button>
        </div>

        <div class="grid gap-5 overflow-y-auto px-6 py-5 sm:grid-cols-2">
          <div class="sm:col-span-2 flex flex-col gap-2">
            <span class="field-label">Banner</span>
            <div class="relative flex h-36 items-center justify-center overflow-hidden rounded-xl bg-surface-alt bg-cover bg-center"
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
            <label for="tTitle" class="field-label">Title</label>
            <input id="tTitle" formControlName="title" maxlength="80" class="field-input" placeholder="e.g. Brazilian Coast Tour" />
            <p class="mt-1 text-xs text-ink-muted">The tour award gets the same name. Upload its medal in Admin › Awards.</p>
          </div>

          <div class="sm:col-span-2">
            <label for="tDescription" class="field-label">Description</label>
            <textarea id="tDescription" formControlName="description" rows="4" maxlength="2000" class="field-input"
              placeholder="What the pilots will see along the way"></textarea>
          </div>

          <div>
            <label for="tStart" class="field-label">Start (UTC, optional)</label>
            <app-date-time-picker inputId="tStart" formControlName="startsAt" [utc]="true" />
          </div>
          <div>
            <label for="tEnd" class="field-label">End (UTC, optional)</label>
            <app-date-time-picker inputId="tEnd" formControlName="endsAt" [utc]="true" [min]="form.controls.startsAt.value || null" />
          </div>

          <fieldset>
            <legend class="field-label">Minimum rank</legend>
            <div class="flex gap-2">
              @for (rank of options().ranks; track rank.code) {
                <label class="flex flex-1 cursor-pointer items-center gap-2 rounded-lg border px-3 py-2"
                  [class]="form.controls.minimumRank.value === rank.code ? 'border-primary bg-primary/5' : 'border-line hover:bg-surface-alt'">
                  <input type="radio" formControlName="minimumRank" [value]="rank.code" class="accent-primary" />
                  <span class="text-sm font-bold text-ink">{{ rank.name }}</span>
                </label>
              }
            </div>
          </fieldset>

          <div>
            <label for="tBonus" class="field-label">Completion bonus (points)</label>
            <input id="tBonus" type="number" min="0" step="50" formControlName="bonusPoints" class="field-input" />
            <p class="mt-1 text-xs text-ink-muted">Paid once when every leg is approved. Each leg also scores like a normal flight.</p>
          </div>

          <fieldset class="sm:col-span-2" [disabled]="locked()">
            <legend class="field-label">Operation</legend>
            <div class="flex rounded-lg border border-line p-0.5 text-sm font-semibold">
              @for (k of kinds; track k.cargo) {
                <button type="button" class="flex flex-1 items-center justify-center gap-2 rounded-md px-3 py-1.5"
                  [class]="cargo() === k.cargo ? 'bg-primary text-on-primary' : 'text-ink-muted hover:text-ink'" (click)="setCargo(k.cargo)">
                  <i class="ti {{ k.icon }}" aria-hidden="true"></i>{{ k.label }}
                </button>
              }
            </div>
            <p class="mt-1 text-xs text-ink-muted">{{ cargo() ? 'Cargo legs use the 9XXX block and every stop needs cargo operations.' : 'Passenger legs use the 7XXX block.' }}</p>
          </fieldset>

          <fieldset class="sm:col-span-2" [disabled]="locked()">
            <legend class="field-label">Aircraft types</legend>
            <div class="flex flex-wrap gap-2">
              @for (type of typeOptions(); track type.icaoCode) {
                <label class="flex cursor-pointer items-center gap-2 rounded-lg border px-3 py-2"
                  [class]="types().includes(type.icaoCode) ? 'border-primary bg-primary/5' : 'border-line hover:bg-surface-alt'">
                  <input type="checkbox" class="accent-primary" [checked]="types().includes(type.icaoCode)" (change)="toggleType(type.icaoCode)" />
                  <span class="font-mono text-sm font-bold text-ink">{{ type.icaoCode }}</span>
                  <span class="text-xs text-ink-muted">{{ type.maxRangeNm }} nm</span>
                </label>
              }
            </div>
          </fieldset>

          <fieldset class="sm:col-span-2" [disabled]="locked()">
            <legend class="field-label">Stops ({{ stops().length - 1 }} legs)</legend>
            @if (locked()) {
              <p class="mb-2 rounded-lg bg-surface-alt px-3 py-2 text-xs text-ink-muted">Pilots already booked legs, so the stops and aircraft can't change.</p>
            }
            <div class="flex flex-col gap-2">
              @for (stop of stops(); track $index; let i = $index) {
                <div class="flex items-center gap-2">
                  <span class="w-16 shrink-0 text-xs font-semibold uppercase text-ink-muted">{{ i === 0 ? 'Start' : 'Leg ' + i }}</span>
                  <input class="field-input font-mono uppercase" maxlength="4" list="tourAirports" placeholder="ICAO"
                    [value]="stop" (input)="setStop(i, $any($event.target).value)" />
                  <span class="hidden min-w-0 flex-1 truncate text-sm text-ink-muted sm:block">{{ airportName(stop) }}</span>
                  <button type="button" class="icon-btn h-9 w-9 shrink-0 text-lg hover:text-danger" aria-label="Remove stop"
                    [disabled]="stops().length <= 3" (click)="removeStop(i)">
                    <i class="ti ti-x" aria-hidden="true"></i>
                  </button>
                </div>
              }
              <datalist id="tourAirports">
                @for (a of options().airports; track a.icao) {
                  <option [value]="a.icao">{{ a.name }} · {{ a.city }}</option>
                }
              </datalist>
              <button type="button" class="btn-secondary flex items-center gap-2 self-start" [disabled]="stops().length > maxLegs" (click)="addStop()">
                <i class="ti ti-plus" aria-hidden="true"></i>Add stop
              </button>
            </div>
          </fieldset>

          <label class="sm:col-span-2 flex cursor-pointer items-center gap-3">
            <input type="checkbox" formControlName="isPublished" class="h-4 w-4 accent-primary" />
            <span>
              <span class="block font-medium text-ink">Published</span>
              <span class="block text-xs text-ink-muted">Pilots see published tours. The first time it's published, eligible pilots get a notification.</span>
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
export class TourFormModal implements OnInit, OnDestroy {
  private readonly api = inject(ToursService);

  readonly tour = input<AdminTour | null>(null);
  readonly options = input.required<TourOptions>();
  readonly saved = output<AdminTour>();
  readonly closed = output<void>();

  protected readonly maxLegs = MAX_LEGS;
  protected readonly stops = signal<string[]>(['', '', '']);
  protected readonly types = signal<string[]>([]);
  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly imageError = signal<string | null>(null);
  private readonly imageFile = signal<File | null>(null);
  private readonly imageRemoved = signal(false);
  private readonly localPreview = signal<string | null>(null);

  protected readonly locked = computed(() => this.tour()?.legsLocked ?? false);
  protected readonly cargo = signal(false);
  protected readonly typeOptions = computed(() => this.options().aircraftTypes.filter(t => t.isCargo === this.cargo()));
  protected readonly kinds = [
    { cargo: false, label: 'Passenger', icon: 'ti-users' },
    { cargo: true, label: 'Cargo', icon: 'ti-package' }
  ];

  protected readonly previewUrl = computed(() => {
    if (this.localPreview()) return this.localPreview();
    if (this.imageRemoved()) return null;
    return this.tour()?.imageUrl ?? null;
  });

  protected readonly form = inject(FormBuilder).nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(80)]],
    description: ['', [Validators.required, Validators.maxLength(2000)]],
    startsAt: [''],
    endsAt: [''],
    minimumRank: ['', Validators.required],
    bonusPoints: [1000, [Validators.required, Validators.min(0)]],
    isPublished: [false]
  });

  ngOnInit(): void {
    const tour = this.tour();
    const options = this.options();

    if (tour) {
      this.form.setValue({
        title: tour.title,
        description: tour.description,
        startsAt: tour.startsAt?.slice(0, 16) ?? '',
        endsAt: tour.endsAt?.slice(0, 16) ?? '',
        minimumRank: options.ranks.find(r => r.level === tour.minimumRankLevel)?.code ?? options.ranks[0]?.code ?? '',
        bonusPoints: tour.bonusPoints,
        isPublished: tour.isPublished
      });
      this.cargo.set(tour.isCargo);
      this.types.set([...tour.aircraftTypes]);
      this.stops.set([tour.legs[0].departure.icao, ...tour.legs.map(l => l.arrival.icao)]);
    } else {
      this.form.controls.minimumRank.setValue(options.ranks[0]?.code ?? '');
      this.types.set(options.aircraftTypes.filter(t => !t.isCargo).map(t => t.icaoCode));
    }
  }

  ngOnDestroy(): void {
    this.revokePreview();
  }

  protected airportName(icao: string): string {
    const a = this.options().airports.find(x => x.icao === icao.toUpperCase());
    return a ? `${a.name} · ${a.city}` : '';
  }

  protected setStop(index: number, value: string): void {
    this.stops.update(list => list.map((s, i) => (i === index ? value.trim().toUpperCase() : s)));
  }

  protected addStop(): void {
    this.stops.update(list => [...list, '']);
  }

  protected removeStop(index: number): void {
    this.stops.update(list => list.filter((_, i) => i !== index));
  }

  protected setCargo(cargo: boolean): void {
    if (this.cargo() === cargo) return;
    this.cargo.set(cargo);
    this.types.set(this.typeOptions().map(t => t.icaoCode));
  }

  protected toggleType(code: string): void {
    this.types.update(list => (list.includes(code) ? list.filter(c => c !== code) : [...list, code]));
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
      this.formError.set('Fill in the title, description, rank and bonus.');
      return;
    }

    const stops = this.stops().map(s => s.trim().toUpperCase());
    if (stops.some(s => s.length !== 4)) {
      this.formError.set('Every stop needs a 4-letter ICAO code.');
      return;
    }
    if (this.types().length === 0) {
      this.formError.set('Select at least one aircraft type.');
      return;
    }

    const v = this.form.getRawValue();
    const startsAt = toUtcIso(v.startsAt);
    const endsAt = toUtcIso(v.endsAt);
    if (startsAt && endsAt && endsAt <= startsAt) {
      this.formError.set('The end must be after the start.');
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
        minimumRank: v.minimumRank,
        isCargo: this.cargo(),
        aircraftTypes: this.types(),
        bonusPoints: Number(v.bonusPoints) || 0,
        stops,
        isPublished: v.isPublished
      };
      const current = this.tour();
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