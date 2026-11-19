import { Component, computed, inject, input, output, signal } from '@angular/core';
import { apiErrorMessage } from '../../../core/api/api-error';
import { MelItem, MelOverview, melLabel } from '../../../core/mel/mel.models';
import { MelService } from '../../../core/mel/mel.service';

@Component({
  selector: 'app-mel-open-modal',
  host: { '(document:keydown.escape)': 'close()' },
  template: `
    <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/50 p-4" (click)="close()">
      <div (click)="$event.stopPropagation()" class="panel flex max-h-[92vh] w-full max-w-2xl flex-col overflow-hidden shadow-2xl">
        <div class="flex items-center justify-between border-b border-line px-6 py-4">
          <h2 class="text-lg font-bold text-ink">Open MEL / CDL item</h2>
          <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Cancel" (click)="close()">
            <i class="ti ti-x" aria-hidden="true"></i>
          </button>
        </div>

        <div class="flex flex-col gap-4 overflow-y-auto px-6 py-5">
          <div>
            <label for="oAircraft" class="field-label">Aircraft</label>
            <select id="oAircraft" class="field-input" [value]="aircraftId()" (change)="pickAircraft($any($event.target).value)">
              <option value="">Select the aircraft</option>
              @for (a of overview().aircraft; track a.id) {
                <option [value]="a.id">{{ a.registration }} · {{ a.aircraftTypeIcao }} · {{ a.airportIcao }}</option>
              }
            </select>
          </div>

          <div>
            <label for="oSearch" class="field-label">Item</label>
            <input id="oSearch" type="search" class="field-input" placeholder="Search by code or title" [disabled]="!aircraftId()"
              [value]="query()" (input)="query.set($any($event.target).value)" />
            <div class="mt-2 flex max-h-64 flex-col gap-1 overflow-y-auto">
              @for (m of options(); track m.id) {
                <button type="button" class="flex items-start gap-3 rounded-lg border px-3 py-2 text-left"
                  [class]="itemId() === m.id ? 'border-primary bg-primary/5' : 'border-transparent hover:bg-surface-alt'" (click)="itemId.set(m.id)">
                  <span class="w-20 shrink-0 font-mono text-xs font-bold text-ink">{{ label(m) }}</span>
                  <span class="min-w-0 flex-1">
                    <span class="block text-sm font-semibold text-ink">{{ m.title }}</span>
                    <span class="block text-xs text-ink-muted">{{ m.restriction }}</span>
                  </span>
                  <span class="rounded bg-surface-alt px-1.5 py-0.5 text-[11px] font-bold text-ink">{{ m.category }} · {{ m.days }}d</span>
                </button>
              } @empty {
                <p class="px-3 py-3 text-center text-sm text-ink-muted">{{ aircraftId() ? 'No items found for this aircraft type.' : 'Select the aircraft first.' }}</p>
              }
            </div>
          </div>

          <div>
            <label for="oNote" class="field-label">Note (optional)</label>
            <input id="oNote" maxlength="300" class="field-input" placeholder="e.g. Reported by the crew after LTL1234"
              [value]="note()" (input)="note.set($any($event.target).value)" />
          </div>
        </div>

        @if (error(); as message) {
          <p class="border-t border-danger/30 bg-danger/10 px-6 py-3 text-sm text-danger">{{ message }}</p>
        }

        <div class="flex justify-end gap-2 border-t border-line px-6 py-4">
          <button type="button" class="btn-secondary" (click)="close()">Cancel</button>
          <button type="button" class="btn-primary px-5" [disabled]="saving() || !aircraftId() || !itemId()" (click)="save()">
            {{ saving() ? 'Loading...' : 'Open item' }}
          </button>
        </div>
      </div>
    </div>
  `
})
export class MelOpenModal {
  private readonly api = inject(MelService);

  readonly overview = input.required<MelOverview>();
  readonly catalog = input.required<MelItem[]>();
  readonly saved = output<MelOverview>();
  readonly closed = output<void>();

  protected readonly aircraftId = signal('');
  protected readonly itemId = signal('');
  protected readonly query = signal('');
  protected readonly note = signal('');
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly options = computed(() => {
    const aircraft = this.overview().aircraft.find(a => a.id === this.aircraftId());
    if (!aircraft) return [];
    const open = new Set(this.overview().open.filter(o => o.aircraftId === aircraft.id).map(o => o.code));
    const q = this.query().trim().toLowerCase();
    return this.catalog()
      .filter(m => m.isActive && !open.has(m.code))
      .filter(m => m.aircraftTypes.length === 0 || m.aircraftTypes.includes(aircraft.aircraftTypeIcao))
      .filter(m => !q || m.code.toLowerCase().includes(q) || m.title.toLowerCase().includes(q));
  });

  protected label = melLabel;

  protected pickAircraft(id: string): void {
    this.aircraftId.set(id);
    this.itemId.set('');
  }

  protected close(): void {
    if (!this.saving()) this.closed.emit();
  }

  protected async save(): Promise<void> {
    this.saving.set(true);
    this.error.set(null);
    try {
      this.saved.emit(await this.api.open(this.aircraftId(), this.itemId(), this.note().trim() || null));
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.saving.set(false);
    }
  }
}