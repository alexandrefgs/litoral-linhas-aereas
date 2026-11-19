import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { apiErrorMessage } from '../../../core/api/api-error';
import { AdminPrivacyRequest, PRIVACY_REQUEST_TYPES, PrivacyRequestStatus, PrivacyRequestType, PrivacyService } from '../../../core/privacy/privacy.service';

const STATUS_STYLE: Record<PrivacyRequestStatus, string> = {
  Open: 'bg-highlight/25 text-ink',
  Completed: 'bg-onda-500/15 text-onda-600',
  Rejected: 'bg-danger/10 text-danger'
};

@Component({
  selector: 'app-admin-privacy',
  imports: [DatePipe, RouterLink],
  template: `
    <div class="flex flex-col gap-6">
      <div>
        <h1 class="text-2xl font-bold text-ink">Privacy requests</h1>
        <p class="text-ink-muted">Requests from pilots about their personal data (LGPD). Answer each one within 15 days.</p>
      </div>

      <div class="grid gap-4 sm:grid-cols-3">
        @for (stat of stats(); track stat.label) {
          <div class="panel flex items-center gap-3 p-4">
            <span class="stat-icon"><i class="ti {{ stat.icon }}" aria-hidden="true"></i></span>
            <div>
              <p class="text-2xl font-bold" [class]="stat.danger && stat.value > 0 ? 'text-danger' : 'text-ink'">{{ stat.value }}</p>
              <p class="text-xs text-ink-muted">{{ stat.label }}</p>
            </div>
          </div>
        }
      </div>

      <div class="flex gap-1 border-b border-line">
        @for (option of tabs; track option.value) {
          <button type="button" (click)="setOpenOnly(option.value)"
            class="-mb-px border-b-2 px-4 py-2.5 text-sm font-semibold"
            [class]="openOnly() === option.value ? 'border-primary text-primary' : 'border-transparent text-ink-muted hover:text-ink'">
            {{ option.label }}
          </button>
        }
      </div>

      @if (loading()) {
        <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (loadError()) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-12 text-center">
          <p class="text-danger">Something went wrong. Try again.</p>
          <button type="button" class="btn-secondary" (click)="load()">Try again</button>
        </div>
      } @else if (requests().length === 0) {
        <p class="panel px-6 py-12 text-center text-ink-muted">{{ openOnly() ? 'No open requests.' : 'No requests yet.' }}</p>
      } @else {
        <div class="flex flex-col gap-4">
          @for (r of requests(); track r.id) {
            <article class="panel p-5" [class.border-danger]="isOverdue(r)">
              <div class="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <p class="font-semibold text-ink">{{ typeLabel(r.type) }}</p>
                  <p class="text-sm text-ink-muted">
                    @if (r.pilotNumber) {
                      <a [routerLink]="['/profile', r.pilotNumber]" class="font-medium text-primary hover:underline">{{ r.pilotLabel }}</a>
                    } @else {
                      {{ r.pilotLabel }} <span class="text-xs">(account deleted)</span>
                    }
                    @if (r.email) { · {{ r.email }} }
                  </p>
                </div>
                <div class="text-right">
                  <span class="rounded-full px-2.5 py-0.5 text-xs font-semibold" [class]="statusStyle[r.status]">{{ r.status }}</span>
                  <p class="mt-1 text-xs" [class]="isOverdue(r) ? 'font-semibold text-danger' : 'text-ink-muted'">
                    Sent {{ r.createdAt | date: 'd MMM y' }}
                    @if (r.status === 'Open') { · due {{ r.dueAt | date: 'd MMM y' }}{{ isOverdue(r) ? ' (overdue)' : '' }} }
                  </p>
                </div>
              </div>

              <p class="mt-3 whitespace-pre-line text-sm text-ink">{{ r.message }}</p>

              @if (r.status === 'Open') {
                <div class="mt-4 flex flex-col gap-2">
                  <textarea rows="3" maxlength="2000" class="field-input" placeholder="Response to the pilot"
                    [value]="drafts()[r.id] ?? ''" (input)="setDraft(r.id, $any($event.target).value)" aria-label="Response"></textarea>
                  <div class="flex flex-wrap gap-2">
                    <button type="button" class="btn-primary px-4" [disabled]="busy() || !(drafts()[r.id] ?? '').trim()" (click)="resolve(r, 'Completed')">
                      Mark as completed
                    </button>
                    <button type="button" class="rounded-md border border-danger px-4 py-2 text-sm font-medium text-danger hover:bg-danger hover:text-white disabled:opacity-50"
                      [disabled]="busy() || !(drafts()[r.id] ?? '').trim()" (click)="resolve(r, 'Rejected')">
                      Reject
                    </button>
                  </div>
                  @if (rowError()?.id === r.id) {
                    <p class="text-sm text-danger">{{ rowError()!.message }}</p>
                  }
                </div>
              } @else if (r.response) {
                <div class="mt-3 rounded-md bg-surface-alt px-3 py-2 text-sm">
                  <p class="whitespace-pre-line text-ink">{{ r.response }}</p>
                  <p class="mt-1 text-xs text-ink-muted">{{ r.resolvedBy }} · {{ r.resolvedAt | date: 'd MMM y, HH:mm' }}</p>
                </div>
              }
            </article>
          }
        </div>
      }
    </div>
  `
})
export class AdminPrivacy implements OnInit {
  private readonly api = inject(PrivacyService);

  protected readonly statusStyle = STATUS_STYLE;
  protected readonly tabs = [
    { value: true, label: 'Open' },
    { value: false, label: 'All' }
  ];

  protected readonly requests = signal<AdminPrivacyRequest[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly openOnly = signal(true);
  protected readonly busy = signal(false);
  protected readonly drafts = signal<Record<string, string>>({});
  protected readonly rowError = signal<{ id: string; message: string } | null>(null);

  protected readonly stats = computed(() => {
    const open = this.requests().filter(r => r.status === 'Open');
    return [
      { icon: 'ti-inbox', label: 'Open', value: open.length, danger: false },
      { icon: 'ti-alarm', label: 'Overdue', value: open.filter(r => this.isOverdue(r)).length, danger: true },
      { icon: 'ti-calendar-due', label: 'Due in 3 days', value: open.filter(r => this.daysLeft(r) >= 0 && this.daysLeft(r) <= 3).length, danger: false }
    ];
  });

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);
    try {
      this.requests.set(await this.api.adminList(this.openOnly()));
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected setOpenOnly(value: boolean): void {
    this.openOnly.set(value);
    this.load();
  }

  protected typeLabel(type: PrivacyRequestType): string {
    return PRIVACY_REQUEST_TYPES.find(t => t.value === type)?.label ?? type;
  }

  protected isOverdue(r: AdminPrivacyRequest): boolean {
    return r.status === 'Open' && Date.parse(r.dueAt) < Date.now();
  }

  protected setDraft(id: string, value: string): void {
    this.drafts.update(d => ({ ...d, [id]: value }));
  }

  protected async resolve(r: AdminPrivacyRequest, status: 'Completed' | 'Rejected'): Promise<void> {
    const response = (this.drafts()[r.id] ?? '').trim();
    if (!response) return;

    this.busy.set(true);
    this.rowError.set(null);
    try {
      const updated = await this.api.resolve(r.id, status, response);
      this.requests.update(list =>
        this.openOnly() ? list.filter(x => x.id !== r.id) : list.map(x => (x.id === r.id ? updated : x)));
    } catch (error) {
      this.rowError.set({ id: r.id, message: apiErrorMessage(error) ?? 'Something went wrong. Try again.' });
    } finally {
      this.busy.set(false);
    }
  }

  private daysLeft(r: AdminPrivacyRequest): number {
    return Math.floor((Date.parse(r.dueAt) - Date.now()) / 86_400_000);
  }
}