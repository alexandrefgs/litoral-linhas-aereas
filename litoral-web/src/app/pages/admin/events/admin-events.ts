import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { apiErrorMessage } from '../../../core/api/api-error';
import { AdminEvent, EVENT_STATE } from '../../../core/events/events.models';
import { EventsService } from '../../../core/events/events.service';
import { ConfirmService } from '../../../shared/confirm-dialog/confirm.service';
import { EventFormModal } from './event-form-modal';

@Component({
  selector: 'app-admin-events',
  imports: [DatePipe, DecimalPipe, EventFormModal],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-ink">Events</h1>
          <p class="text-ink-muted">Fly-ins and network events. Every approved flight inside the window and at the event airports earns the bonus.</p>
        </div>
        <button type="button" class="btn-primary flex items-center gap-2" (click)="editing.set({ item: null })">
          <i class="ti ti-plus" aria-hidden="true"></i>New event
        </button>
      </div>

      @if (error(); as message) {
        <p class="rounded-lg bg-danger/10 px-4 py-3 text-sm text-danger">{{ message }}</p>
      }

      @if (loading()) {
        <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (items().length === 0) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-12 text-center">
          <i class="ti ti-flag text-4xl text-ink-muted" aria-hidden="true"></i>
          <p class="text-ink-muted">No events yet.</p>
        </div>
      } @else {
        <div class="panel divide-y divide-line">
          @for (item of items(); track item.id) {
            <div class="flex flex-col gap-3 px-5 py-4 sm:flex-row sm:items-center">
              <div class="h-16 w-full shrink-0 rounded-lg bg-gradient-to-br from-atlantico-700 to-onda-500 bg-cover bg-center sm:w-28"
                [style.background-image]="item.imageUrl ? 'url(' + item.imageUrl + ')' : null"></div>
              <div class="min-w-0 flex-1">
                <p class="flex flex-wrap items-center gap-2 font-semibold text-ink">
                  {{ item.title }}
                  <span class="rounded px-2 py-0.5 text-[11px] font-bold" [class]="state[item.state].class">{{ state[item.state].label }}</span>
                </p>
                <p class="mt-1 flex flex-wrap gap-x-3 text-xs text-ink-muted">
                  <span>{{ item.startsAt | date: 'dd/MM/yy HH:mm' : 'UTC' }} – {{ item.endsAt | date: 'dd/MM HH:mm' : 'UTC' }} UTC</span>
                  <span class="font-mono">{{ item.airports.length ? item.airports.join(', ') : 'Any airport' }}</span>
                  <span>+{{ item.bonusPoints | number }} per flight</span>
                  <span>{{ item.flights }} flights · {{ item.pilots }} pilots</span>
                </p>
              </div>
              <div class="flex shrink-0 gap-1">
                <button type="button" class="icon-btn h-9 w-9 text-lg" title="Edit" aria-label="Edit" (click)="editing.set({ item })">
                  <i class="ti ti-pencil" aria-hidden="true"></i>
                </button>
                <button type="button" class="icon-btn h-9 w-9 text-lg hover:text-danger" title="Delete" aria-label="Delete" (click)="remove(item)">
                  <i class="ti ti-trash" aria-hidden="true"></i>
                </button>
              </div>
            </div>
          }
        </div>
      }
    </div>

    @if (editing(); as state) {
      <app-event-form-modal [item]="state.item" (saved)="onSaved($event)" (closed)="editing.set(null)" />
    }
  `
})
export class AdminEvents implements OnInit {
  private readonly api = inject(EventsService);
  private readonly confirm = inject(ConfirmService);

  protected readonly state = EVENT_STATE;
  protected readonly items = signal<AdminEvent[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly editing = signal<{ item: AdminEvent | null } | null>(null);

  async ngOnInit(): Promise<void> {
    try {
      this.items.set(await this.api.adminList());
    } catch {
      this.error.set('Something went wrong. Reload the page.');
    } finally {
      this.loading.set(false);
    }
  }

  protected onSaved(item: AdminEvent): void {
    this.items.update(list => (list.some(i => i.id === item.id) ? list.map(i => (i.id === item.id ? item : i)) : [item, ...list]));
    this.editing.set(null);
  }

  protected async remove(item: AdminEvent): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Delete event?',
      message: `"${item.title}" will be deleted. This can't be undone.`,
      confirmLabel: 'Delete',
      tone: 'danger',
      icon: 'ti-trash'
    });
    if (!ok) return;

    this.error.set(null);
    try {
      await this.api.delete(item.id);
      this.items.update(list => list.filter(i => i.id !== item.id));
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    }
  }
}