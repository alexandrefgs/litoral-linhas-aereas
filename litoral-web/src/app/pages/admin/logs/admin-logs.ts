import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { AdminLogsService, AuditCategory, AuditLogEntry, AuditSeverity } from '../../../core/admin/admin-logs.service';

const PAGE_SIZE = 50;

const CATEGORIES: { value: AuditCategory; label: string; icon: string }[] = [
  { value: 'Account', label: 'Accounts', icon: 'ti-user-circle' },
  { value: 'Pilots', label: 'Pilots', icon: 'ti-users' },
  { value: 'Staff', label: 'Staff', icon: 'ti-shield-lock' },
  { value: 'Pireps', label: 'PIREPs', icon: 'ti-clipboard-check' },
  { value: 'Scoring', label: 'Scoring', icon: 'ti-scale' },
  { value: 'Fleet', label: 'Fleet', icon: 'ti-plane' },
  { value: 'Routes', label: 'Routes', icon: 'ti-route' },
  { value: 'Airports', label: 'Airports', icon: 'ti-building-airport' },
  { value: 'Operations', label: 'Operations', icon: 'ti-calendar-event' },
  { value: 'Awards', label: 'Awards', icon: 'ti-award' },
  { value: 'Bookings', label: 'Bookings', icon: 'ti-ticket' },
  { value: 'Logins', label: 'Logins', icon: 'ti-login' }
];

const SEVERITY: Record<AuditSeverity, { label: string; bar: string; dot: string; text: string }> = {
  Critical: { label: 'Critical', bar: 'border-l-danger', dot: 'bg-danger', text: 'text-danger' },
  Warning: { label: 'Important', bar: 'border-l-highlight', dot: 'bg-highlight', text: 'text-ink' },
  Info: { label: 'Routine', bar: 'border-l-transparent', dot: 'bg-onda-500', text: 'text-ink' }
};

@Component({
  selector: 'app-admin-logs',
  imports: [DatePipe],
  template: `
    <div class="flex flex-col gap-6">
      <div>
        <h1 class="text-2xl font-bold text-ink">Logs</h1>
        <p class="text-ink-muted">Every change made to accounts, staff and operations, with who did it and when.</p>
      </div>

      <div class="flex flex-col gap-3">
        <div class="flex flex-wrap items-center gap-3">
          <div class="relative w-full sm:w-96">
            <i class="ti ti-search pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-ink-muted" aria-hidden="true"></i>
            <input type="search" class="field-input pl-9" placeholder="Search action, pilot, item or detail"
              [value]="search()" (input)="onSearch($any($event.target).value)" />
          </div>
          <button type="button" class="flex items-center gap-1.5 rounded-full border px-3 py-1.5 text-sm font-medium"
            [class]="criticalOnly() ? 'border-danger bg-danger text-white' : 'border-danger/50 text-danger hover:bg-danger/10'"
            (click)="toggleCritical()">
            <i class="ti ti-alert-octagon" aria-hidden="true"></i>Critical only
          </button>
          <div class="flex flex-wrap items-center gap-3 text-xs text-ink-muted sm:ml-auto">
            @for (s of severities; track s.label) {
              <span class="flex items-center gap-1.5"><span class="h-2.5 w-2.5 rounded-full" [class]="s.dot"></span>{{ s.label }}</span>
            }
          </div>
        </div>
        <div class="flex flex-wrap gap-2">
          <button type="button" class="rounded-full border px-3 py-1 text-sm font-medium"
            [class]="category() === null ? 'border-primary bg-primary text-on-primary' : 'border-line text-ink hover:bg-surface-alt'"
            (click)="setCategory(null)">All</button>
          @for (c of categories; track c.value) {
            <button type="button" class="flex items-center gap-1.5 rounded-full border px-3 py-1 text-sm font-medium"
              [class]="category() === c.value ? 'border-primary bg-primary text-on-primary' : 'border-line text-ink hover:bg-surface-alt'"
              (click)="setCategory(c.value)">
              <i class="ti {{ c.icon }}" aria-hidden="true"></i>{{ c.label }}
            </button>
          }
        </div>
      </div>

      <div class="panel overflow-x-auto">
        @if (loading() && items().length === 0) {
          <p class="px-6 py-12 text-center text-ink-muted">Loading...</p>
        } @else if (loadError()) {
          <div class="flex flex-col items-center gap-3 px-6 py-12 text-center">
            <p class="text-danger">Something went wrong. Try again.</p>
            <button type="button" class="btn-secondary" (click)="load()">Try again</button>
          </div>
        } @else if (items().length === 0) {
          <p class="px-6 py-12 text-center text-ink-muted">No logs found.</p>
        } @else {
          <table class="w-full text-left text-sm" [class.opacity-60]="loading()">
            <thead class="border-b border-line text-xs uppercase tracking-wide text-ink-muted">
              <tr>
                <th class="px-4 py-3 font-semibold">When</th>
                <th class="px-4 py-3 font-semibold">Action</th>
                <th class="px-4 py-3 font-semibold">By</th>
                <th class="px-4 py-3 font-semibold">Item</th>
                <th class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody>
              @for (log of items(); track log.id) {
                <tr class="border-b border-line align-top last:border-0 hover:bg-surface-alt/50" [class.cursor-pointer]="!!log.details" (click)="toggle(log)">
                  <td class="whitespace-nowrap border-l-4 px-4 py-3" [class]="severityOf(log).bar">
                    <p class="text-ink">{{ log.at | date: 'd MMM y' : 'UTC' }}</p>
                    <p class="font-mono text-xs text-ink-muted">{{ log.at | date: 'HH:mm:ss' : 'UTC' }}Z</p>
                  </td>
                  <td class="px-4 py-3">
                    <p class="flex items-center gap-2 font-semibold" [class]="severityOf(log).text">
                      <span class="h-2.5 w-2.5 shrink-0 rounded-full" [class]="severityOf(log).dot" [title]="severityOf(log).label"></span>{{ log.action }}
                    </p>
                    <span class="mt-1 inline-flex items-center gap-1 rounded-full bg-surface-alt px-2 py-0.5 text-[11px] font-semibold text-ink-muted">
                      <i class="ti {{ iconOf(log.category) }}" aria-hidden="true"></i>{{ labelOf(log.category) }}
                    </span>
                  </td>
                  <td class="px-4 py-3">
                    <p class="text-ink">{{ log.actorName }}</p>
                    @if (log.ipAddress) {
                      <p class="font-mono text-[11px] text-ink-muted">{{ log.ipAddress }}</p>
                    }
                  </td>
                  <td class="px-4 py-3">
                    <p class="text-ink">{{ log.targetName ?? '—' }}</p>
                    @if (log.targetType) {
                      <p class="text-xs text-ink-muted">{{ log.targetType }}</p>
                    }
                    @if (expanded().has(log.id) && log.details) {
                      <pre class="mt-2 whitespace-pre-wrap rounded-lg bg-surface-alt px-3 py-2 font-mono text-xs leading-relaxed text-ink">{{ log.details }}</pre>
                    }
                  </td>
                  <td class="px-4 py-3 text-right">
                    @if (log.details) {
                      <i class="ti text-lg text-ink-muted" [class]="expanded().has(log.id) ? 'ti-chevron-up' : 'ti-chevron-down'" aria-hidden="true"></i>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>

          <div class="flex items-center justify-between border-t border-line px-4 py-3 text-sm">
            <span class="text-ink-muted">{{ total() }} logs · page {{ page() }} of {{ pages() }}</span>
            <div class="flex gap-1">
              <button type="button" class="btn-secondary px-3 py-1" [disabled]="page() === 1 || loading()" (click)="go(page() - 1)">
                <i class="ti ti-chevron-left" aria-hidden="true"></i>
              </button>
              <button type="button" class="btn-secondary px-3 py-1" [disabled]="page() === pages() || loading()" (click)="go(page() + 1)">
                <i class="ti ti-chevron-right" aria-hidden="true"></i>
              </button>
            </div>
          </div>
        }
      </div>
    </div>
  `
})
export class AdminLogs implements OnInit {
  private readonly api = inject(AdminLogsService);

  protected readonly categories = CATEGORIES;
  protected readonly severities = Object.values(SEVERITY);

  protected readonly items = signal<AuditLogEntry[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly category = signal<AuditCategory | null>(null);
  protected readonly search = signal('');
  protected readonly criticalOnly = signal(false);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly expanded = signal(new Set<string>());

  protected readonly pages = computed(() => Math.max(1, Math.ceil(this.total() / PAGE_SIZE)));

  private searchTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.searchTimer));
  }

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);
    try {
      const result = await this.api.list(this.category(), this.search(), this.criticalOnly(), this.page(), PAGE_SIZE);
      this.items.set(result.items);
      this.total.set(result.total);
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected setCategory(value: AuditCategory | null): void {
    this.category.set(value);
    this.page.set(1);
    this.load();
  }

  protected toggleCritical(): void {
    this.criticalOnly.update(v => !v);
    this.page.set(1);
    this.load();
  }

  protected onSearch(value: string): void {
    this.search.set(value);
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.page.set(1);
      this.load();
    }, 400);
  }

  protected go(page: number): void {
    this.page.set(page);
    this.load();
  }

  protected toggle(log: AuditLogEntry): void {
    if (!log.details) return;
    this.expanded.update(set => {
      const next = new Set(set);
      if (next.has(log.id)) next.delete(log.id);
      else next.add(log.id);
      return next;
    });
  }

  protected severityOf(log: AuditLogEntry) {
    return SEVERITY[log.severity] ?? SEVERITY['Info'];
  }

  protected iconOf(category: AuditCategory): string {
    return CATEGORIES.find(c => c.value === category)?.icon ?? 'ti-history';
  }

  protected labelOf(category: AuditCategory): string {
    return CATEGORIES.find(c => c.value === category)?.label ?? category;
  }
}