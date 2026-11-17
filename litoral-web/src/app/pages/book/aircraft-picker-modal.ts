import { DatePipe } from '@angular/common';
import { Component, computed, input, output, signal } from '@angular/core';
import { AircraftOptions, BookingAircraft } from '../../core/booking/booking.models';

@Component({
  selector: 'app-aircraft-picker-modal',
  imports: [DatePipe],
  host: { '(document:keydown.escape)': 'close()' },
  template: `
    <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/60 p-4" (click)="close()">
      <div class="panel flex max-h-[92vh] w-full max-w-5xl flex-col overflow-hidden shadow-2xl" (click)="$event.stopPropagation()">
        <div class="flex items-center justify-between gap-4 border-b border-line px-6 py-4">
          <div>
            <h2 class="text-lg font-bold text-ink">Select aircraft</h2>
            <p class="text-sm text-ink-muted">
              <span class="font-mono font-semibold text-ink">{{ title() }}</span>
              @if (options(); as data) {
                · Validity: <span class="font-semibold text-ink">{{ data.validityHours }} hours</span>
              }
            </p>
          </div>
          <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Close" (click)="close()">
            <i class="ti ti-x" aria-hidden="true"></i>
          </button>
        </div>

        <div class="flex-1 overflow-y-auto px-6 py-5">
          @if (!options()) {
            <p class="py-16 text-center text-ink-muted">Loading...</p>
          } @else if (!options()!.aircraft.length) {
            <div class="flex flex-col items-center gap-3 py-16 text-center">
              <i class="ti ti-plane-off text-4xl text-ink-muted" aria-hidden="true"></i>
              <p class="text-ink-muted">There is no aircraft available here for this flight.</p>
              <p class="text-sm text-ink-muted">Go to the Fleet page and bring one to your airport.</p>
            </div>
          } @else {
            <label class="mb-6 flex cursor-pointer items-start gap-3 rounded-xl border px-5 py-4"
              [class]="selected() === null ? 'border-primary bg-primary/5' : 'border-line hover:bg-surface-alt'">
              <input type="radio" name="aircraft" class="mt-1 accent-primary" [checked]="selected() === null" (change)="selected.set(null)" />
              <span class="flex-1">
                <span class="block font-semibold text-primary">Random selection</span>
                <span class="block text-sm text-ink-muted">Litoral will select an aircraft from all available below.</span>
              </span>
              <i class="ti ti-arrows-shuffle text-xl text-primary" aria-hidden="true"></i>
            </label>

            @for (group of groups(); track group.name) {
              <div class="mb-6">
                <div class="mb-4 flex items-center gap-4">
                  <span class="h-px flex-1 bg-line"></span>
                  <h3 class="text-lg font-bold text-ink">{{ group.name }}</h3>
                  <span class="h-px flex-1 bg-line"></span>
                </div>
                <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                  @for (item of group.aircraft; track item.id) {
                    <label class="card-hover flex cursor-pointer flex-col overflow-hidden rounded-xl border"
                      [class]="selected() === item.id ? 'border-primary ring-2 ring-primary/30' : 'border-line'">
                      <span class="flex items-center justify-center gap-2 px-4 py-3">
                        <input type="radio" name="aircraft" class="accent-primary" [checked]="selected() === item.id" (change)="selected.set(item.id)" />
                        <span class="font-mono text-xl font-bold text-ink">{{ item.registration }}</span>
                      </span>
                      <span class="relative mx-4 flex aspect-[16/9] items-center justify-center overflow-hidden rounded-lg bg-surface-alt">
                        @if (item.photoUrl) {
                          <img [src]="item.photoUrl" [alt]="item.registration" class="h-full w-full object-cover" loading="lazy" />
                        } @else {
                          <i class="ti ti-plane text-5xl text-ink-muted" aria-hidden="true"></i>
                        }
                      </span>
                      <span class="px-4 pb-4 pt-3 text-center">
                        <span class="block font-semibold text-ink">{{ item.variant || item.typeIcao }}</span>
                        <span class="block text-xs text-ink-muted">
                          {{ item.lastFlightAt ? 'Last flight: ' + (item.lastFlightAt | date: 'd MMM y') : 'Never flown' }}
                        </span>
                        @if (item.photoCredit) {
                          <span class="mt-1 block text-[11px] text-ink-muted">Photo: {{ item.photoCredit }}</span>
                        }
                      </span>
                    </label>
                  }
                </div>
              </div>
            }
          }
        </div>

        @if (error(); as message) {
          <p class="border-t border-danger/30 bg-danger/10 px-6 py-3 text-sm text-danger">
            <i class="ti ti-alert-circle mr-1" aria-hidden="true"></i>{{ message }}
          </p>
        }

        <div class="flex flex-wrap items-center justify-between gap-3 border-t border-line px-6 py-4">
          <p class="text-sm text-ink-muted">
            @if (cost() > 0) {
              This flight costs <span class="font-semibold text-ink">{{ cost() }} points</span>, not refunded if cancelled or expired.
            } @else {
              This flight is free.
            }
          </p>
          <div class="flex gap-2">
            <button type="button" class="btn-secondary" (click)="close()">Cancel</button>
            <button type="button" class="btn-primary px-5" [disabled]="saving() || !options()?.aircraft?.length" (click)="confirm.emit(selected())">
              {{ saving() ? 'Booking...' : 'Book flight' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class AircraftPickerModal {
  readonly title = input('');
  readonly options = input<AircraftOptions | null>(null);
  readonly cost = input(0);
  readonly saving = input(false);
  readonly error = input<string | null>(null);

  readonly confirm = output<string | null>();
  readonly closed = output<void>();

  protected readonly selected = signal<string | null>(null);

  protected readonly groups = computed(() => {
    const map = new Map<string, BookingAircraft[]>();
    for (const item of this.options()?.aircraft ?? []) {
      map.set(item.typeName, [...(map.get(item.typeName) ?? []), item]);
    }
    return [...map.entries()].map(([name, aircraft]) => ({ name, aircraft }));
  });

  protected close(): void {
    if (!this.saving()) this.closed.emit();
  }
}