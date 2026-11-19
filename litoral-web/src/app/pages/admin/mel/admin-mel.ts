import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { apiErrorMessage } from '../../../core/api/api-error';
import { MEL_VERIFICATION_LABEL, MelItem, MelOverview, OpenMelItem, melLabel } from '../../../core/mel/mel.models';
import { MelService } from '../../../core/mel/mel.service';
import { ConfirmService } from '../../../shared/confirm-dialog/confirm.service';
import { MelItemFormModal } from './mel-item-form-modal';
import { MelOpenModal } from './mel-open-modal';

type Tab = 'open' | 'catalog';

@Component({
  selector: 'app-admin-mel',
  imports: [DatePipe, MelItemFormModal, MelOpenModal],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-ink">MEL / CDL</h1>
          <p class="text-ink-muted">Open items fly within their category deadline. An expired item grounds the aircraft until it's closed.</p>
        </div>
        <div class="flex gap-2">
          <div class="flex rounded-lg border border-line p-1">
            @for (t of tabs; track t.value) {
              <button type="button" (click)="tab.set(t.value)" class="flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium"
                [class]="tab() === t.value ? 'bg-primary text-on-primary' : 'text-ink hover:bg-surface-alt'">
                <i class="ti {{ t.icon }}" aria-hidden="true"></i>{{ t.label }}
              </button>
            }
          </div>
          @if (tab() === 'open') {
            <button type="button" class="btn-primary flex items-center gap-2" [disabled]="!overview()" (click)="opening.set(true)">
              <i class="ti ti-plus" aria-hidden="true"></i>Open item
            </button>
          } @else {
            <button type="button" class="btn-primary flex items-center gap-2" [disabled]="!overview()" (click)="editing.set({ item: null })">
              <i class="ti ti-plus" aria-hidden="true"></i>New item
            </button>
          }
        </div>
      </div>

      @if (error(); as message) {
        <p class="rounded-lg bg-danger/10 px-4 py-3 text-sm text-danger">{{ message }}</p>
      }

      @if (loading()) {
        <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (tab() === 'open') {
        @if (!overview()?.open?.length) {
          <div class="panel flex flex-col items-center gap-3 px-6 py-12 text-center">
            <i class="ti ti-tool text-4xl text-ink-muted" aria-hidden="true"></i>
            <p class="text-ink-muted">No open items. The whole fleet is clean.</p>
          </div>
        } @else {
          <div class="panel divide-y divide-line">
            @for (o of overview()!.open; track o.id) {
              <div class="flex flex-col gap-3 px-5 py-4 sm:flex-row sm:items-center" [class.bg-danger/5]="o.expired">
                <div class="w-28 shrink-0">
                  <p class="font-mono text-lg font-bold text-ink">{{ o.registration }}</p>
                  <p class="text-xs text-ink-muted">{{ o.aircraftTypeIcao }} · {{ o.airportIcao }}</p>
                </div>
                <div class="min-w-0 flex-1">
                  <p class="flex flex-wrap items-center gap-2 font-semibold text-ink">
                    <span class="font-mono text-xs">{{ label(o) }}</span>{{ o.title }}
                    <span class="rounded bg-surface-alt px-1.5 py-0.5 text-[11px] font-bold">Cat {{ o.category }}</span>
                    @if (o.expired) {
                      <span class="rounded bg-danger px-1.5 py-0.5 text-[11px] font-bold text-white">EXPIRED · AOG</span>
                    }
                  </p>
                  <p class="text-sm text-ink-muted">{{ o.restriction }}</p>
                  <p class="mt-1 text-xs text-ink-muted">
                    Opened {{ o.openedAt | date: 'd MMM HH:mm' : 'UTC' }}Z by {{ o.openedBy }} · due {{ o.dueAt | date: 'd MMM HH:mm' : 'UTC' }}Z
                    {{ o.note ? ' · ' + o.note : '' }}
                  </p>
                </div>
                <button type="button" class="btn-secondary flex shrink-0 items-center gap-2" (click)="close(o)">
                  <i class="ti ti-check" aria-hidden="true"></i>Close
                </button>
              </div>
            }
          </div>
        }
      } @else {
        <div class="flex flex-wrap items-center gap-2">
          <input type="search" class="field-input w-full sm:w-72" placeholder="Search by code or title" [value]="query()" (input)="query.set($any($event.target).value)" />
          <span class="text-sm text-ink-muted">{{ filtered().length }} items · {{ autoCount() }} opened by the system</span>
        </div>
        <div class="panel divide-y divide-line">
          @for (m of filtered(); track m.id) {
            <div class="flex flex-col gap-2 px-5 py-3 sm:flex-row sm:items-center" [class.opacity-50]="!m.isActive">
              <span class="w-20 shrink-0 font-mono text-sm font-bold text-ink">{{ label(m) }}</span>
              <div class="min-w-0 flex-1">
                <p class="flex flex-wrap items-center gap-2 font-semibold text-ink">
                  {{ m.title }}
                  <span class="rounded bg-surface-alt px-1.5 py-0.5 text-[11px] font-bold">Cat {{ m.category }} · {{ m.days }}d</span>
                  <span class="rounded bg-surface-alt px-1.5 py-0.5 text-[11px] text-ink-muted">{{ verification[m.verification] }}</span>
                  @if (m.autoEligible) {
                    <span class="rounded bg-primary/15 px-1.5 py-0.5 text-[11px] font-semibold text-primary">System</span>
                  }
                  @if (m.aircraftTypes.length) {
                    <span class="font-mono text-[11px] text-ink-muted">{{ m.aircraftTypes.join(', ') }}</span>
                  }
                </p>
                <p class="truncate text-sm text-ink-muted">{{ m.restriction }}</p>
              </div>
              <div class="flex shrink-0 gap-1">
                <button type="button" class="icon-btn h-9 w-9 text-lg" title="Edit" aria-label="Edit" (click)="editing.set({ item: m })">
                  <i class="ti ti-pencil" aria-hidden="true"></i>
                </button>
                <button type="button" class="icon-btn h-9 w-9 text-lg hover:text-danger" title="Delete" aria-label="Delete" (click)="remove(m)">
                  <i class="ti ti-trash" aria-hidden="true"></i>
                </button>
              </div>
            </div>
          }
        </div>
      }
    </div>

    @if (editing(); as state) {
      <app-mel-item-form-modal [item]="state.item" [aircraftTypes]="overview()?.aircraftTypes ?? []" (saved)="onSaved($event)" (closed)="editing.set(null)" />
    }
    @if (opening() && overview()) {
      <app-mel-open-modal [overview]="overview()!" [catalog]="catalog()" (saved)="onOpened($event)" (closed)="opening.set(false)" />
    }
  `
})
export class AdminMel implements OnInit {
  private readonly api = inject(MelService);
  private readonly confirm = inject(ConfirmService);

  protected readonly tabs: { value: Tab; label: string; icon: string }[] = [
    { value: 'open', label: 'Open items', icon: 'ti-alert-triangle' },
    { value: 'catalog', label: 'Catalog', icon: 'ti-list-details' }
  ];
  protected readonly verification = MEL_VERIFICATION_LABEL;
  protected readonly label = melLabel;

  protected readonly tab = signal<Tab>('open');
  protected readonly overview = signal<MelOverview | null>(null);
  protected readonly catalog = signal<MelItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly query = signal('');
  protected readonly editing = signal<{ item: MelItem | null } | null>(null);
  protected readonly opening = signal(false);

  protected readonly filtered = computed(() => {
    const q = this.query().trim().toLowerCase();
    return this.catalog().filter(m => !q || m.code.toLowerCase().includes(q) || m.title.toLowerCase().includes(q));
  });
  protected readonly autoCount = computed(() => this.catalog().filter(m => m.autoEligible && m.isActive).length);

  async ngOnInit(): Promise<void> {
    try {
      const [overview, catalog] = await Promise.all([this.api.overview(), this.api.catalog()]);
      this.overview.set(overview);
      this.catalog.set(catalog);
    } catch {
      this.error.set('Something went wrong. Reload the page.');
    } finally {
      this.loading.set(false);
    }
  }

  protected onSaved(item: MelItem): void {
    this.catalog.update(list => (list.some(m => m.id === item.id) ? list.map(m => (m.id === item.id ? item : m)) : [...list, item].sort((a, b) => a.code.localeCompare(b.code))));
    this.editing.set(null);
  }

  protected onOpened(overview: MelOverview): void {
    this.overview.set(overview);
    this.opening.set(false);
  }

  protected async close(item: OpenMelItem): Promise<void> {
    const ok = await this.confirm.ask({
      title: `Close ${item.code} on ${item.registration}?`,
      message: 'The item is marked as fixed and the restriction stops applying.',
      confirmLabel: 'Close item',
      tone: 'success',
      icon: 'ti-tool'
    });
    if (!ok) return;

    this.error.set(null);
    try {
      this.overview.set(await this.api.close(item.id));
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    }
  }

  protected async remove(item: MelItem): Promise<void> {
    const ok = await this.confirm.ask({
      title: `Delete ${item.code}?`,
      message: `"${item.title}" will be removed from the catalog.`,
      confirmLabel: 'Delete',
      tone: 'danger',
      icon: 'ti-trash'
    });
    if (!ok) return;

    this.error.set(null);
    try {
      await this.api.delete(item.id);
      this.catalog.update(list => list.filter(m => m.id !== item.id));
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    }
  }
}