import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { POINT_TYPE_ICONS, POINT_TYPE_LABELS, PointTransaction, PointsGroup, PointsService, PointsStatement } from '../../core/points/points.service';

const PAGE_SIZE = 20;

const GROUPS: { value: PointsGroup; label: string; icon: string }[] = [
  { value: 'Flights', label: 'Flights', icon: 'ti-plane-arrival' },
  { value: 'Bonuses', label: 'Bonuses', icon: 'ti-gift' },
  { value: 'Spending', label: 'Spending', icon: 'ti-shopping-cart' },
  { value: 'Penalties', label: 'Penalties', icon: 'ti-alert-triangle' },
  { value: 'Adjustments', label: 'Adjustments', icon: 'ti-adjustments' }
];

@Component({
  selector: 'app-points-statement',
  imports: [DatePipe, DecimalPipe],
  host: { class: 'flex flex-col gap-6' },
  template: `
    @if (data(); as d) {
      <div class="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <div class="panel card-hover flex items-center gap-3 p-4">
          <span class="stat-icon"><i class="ti ti-star" aria-hidden="true"></i></span>
          <div>
            <p class="text-xl font-bold" [class]="d.summary.balance < 0 ? 'text-danger' : 'text-ink'">{{ d.summary.balance | number }}</p>
            <p class="text-xs text-ink-muted">Balance</p>
          </div>
        </div>
        <div class="panel card-hover flex items-center gap-3 p-4">
          <span class="stat-icon"><i class="ti ti-calendar" aria-hidden="true"></i></span>
          <div>
            <p class="text-xl font-bold" [class]="monthNet() < 0 ? 'text-danger' : 'text-onda-600'">{{ signed(monthNet()) }}</p>
            <p class="text-xs text-ink-muted">This month · +{{ d.summary.monthEarned | number }} / −{{ d.summary.monthSpent | number }}</p>
          </div>
        </div>
        <div class="panel card-hover flex items-center gap-3 p-4">
          <span class="stat-icon"><i class="ti ti-trending-up" aria-hidden="true"></i></span>
          <div>
            <p class="text-xl font-bold text-onda-600">+{{ d.summary.totalEarned | number }}</p>
            <p class="text-xs text-ink-muted">Earned all time</p>
          </div>
        </div>
        <div class="panel card-hover flex items-center gap-3 p-4">
          <span class="stat-icon"><i class="ti ti-trending-down" aria-hidden="true"></i></span>
          <div>
            <p class="text-xl font-bold text-danger">−{{ d.summary.totalSpent | number }}</p>
            <p class="text-xs text-ink-muted">Spent all time</p>
          </div>
        </div>
      </div>
    }

    <div class="flex flex-wrap gap-2">
      <button type="button" class="rounded-full border px-3 py-1 text-sm font-medium"
        [class]="group() === null ? 'border-primary bg-primary text-on-primary' : 'border-line text-ink hover:bg-surface-alt'"
        (click)="setGroup(null)">All</button>
      @for (g of groups; track g.value) {
        <button type="button" class="flex items-center gap-1.5 rounded-full border px-3 py-1 text-sm font-medium"
          [class]="group() === g.value ? 'border-primary bg-primary text-on-primary' : 'border-line text-ink hover:bg-surface-alt'"
          (click)="setGroup(g.value)">
          <i class="ti {{ g.icon }}" aria-hidden="true"></i>{{ g.label }}
        </button>
      }
    </div>

    <div class="panel overflow-x-auto">
      @if (loading() && !data()) {
        <p class="px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (loadError()) {
        <div class="flex flex-col items-center gap-3 px-6 py-12 text-center">
          <p class="text-danger">Something went wrong. Try again.</p>
          <button type="button" class="btn-secondary" (click)="load()">Try again</button>
        </div>
      } @else if (!data()?.items?.length) {
        <p class="px-6 py-12 text-center text-ink-muted">{{ group() ? 'No transactions of this kind yet.' : 'No transactions yet. Fly your first flight to start earning points!' }}</p>
      } @else {
        <table class="w-full text-left text-sm" [class.opacity-60]="loading()">
          <thead class="border-b border-line text-xs uppercase tracking-wide text-ink-muted">
            <tr>
              <th class="px-4 py-3 font-semibold">Date (UTC)</th>
              <th class="px-4 py-3 font-semibold">Description</th>
              <th class="px-4 py-3 text-right font-semibold">Points</th>
              @if (showBalance()) {
                <th class="px-4 py-3 text-right font-semibold">Balance</th>
              }
            </tr>
          </thead>
          <tbody>
            @for (t of data()!.items; track t.id) {
              <tr class="border-b border-line last:border-0 hover:bg-surface-alt/50">
                <td class="whitespace-nowrap px-4 py-3">
                  <p class="text-ink">{{ t.at | date: 'd MMM y' : 'UTC' }}</p>
                  <p class="font-mono text-xs text-ink-muted">{{ t.at | date: 'HH:mm' : 'UTC' }}Z</p>
                </td>
                <td class="px-4 py-3">
                  <div class="flex items-center gap-3">
                    <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-lg"
                      [class]="t.amount >= 0 ? 'bg-onda-500/15 text-onda-600' : 'bg-danger/10 text-danger'">
                      <i class="ti {{ iconOf(t) }}" aria-hidden="true"></i>
                    </span>
                    <div class="min-w-0">
                      <p class="text-ink">{{ t.description }}</p>
                      <p class="text-xs text-ink-muted">{{ labelOf(t) }}</p>
                    </div>
                  </div>
                </td>
                <td class="whitespace-nowrap px-4 py-3 text-right font-mono font-bold" [class]="t.amount >= 0 ? 'text-onda-600' : 'text-danger'">
                  {{ signed(t.amount) }}
                </td>
                @if (showBalance()) {
                  <td class="whitespace-nowrap px-4 py-3 text-right font-mono" [class]="(t.balanceAfter ?? 0) < 0 ? 'text-danger' : 'text-ink-muted'">
                    {{ t.balanceAfter | number }}
                  </td>
                }
              </tr>
            }
          </tbody>
        </table>

        <div class="flex items-center justify-between border-t border-line px-4 py-3 text-sm">
          <span class="text-ink-muted">{{ data()!.total }} transactions · page {{ page() }} of {{ pages() }}</span>
          <div class="flex gap-1">
            <button type="button" class="btn-secondary px-3 py-1" [disabled]="page() === 1 || loading()" (click)="go(page() - 1)" aria-label="Previous page">
              <i class="ti ti-chevron-left" aria-hidden="true"></i>
            </button>
            <button type="button" class="btn-secondary px-3 py-1" [disabled]="page() === pages() || loading()" (click)="go(page() + 1)" aria-label="Next page">
              <i class="ti ti-chevron-right" aria-hidden="true"></i>
            </button>
          </div>
        </div>
      }
    </div>
  `
})
export class PointsStatementView implements OnInit {
  private readonly api = inject(PointsService);

  protected readonly groups = GROUPS;

  protected readonly data = signal<PointsStatement | null>(null);
  protected readonly group = signal<PointsGroup | null>(null);
  protected readonly page = signal(1);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);

  protected readonly pages = computed(() => Math.max(1, Math.ceil((this.data()?.total ?? 0) / PAGE_SIZE)));
  protected readonly monthNet = computed(() => {
    const s = this.data()?.summary;
    return s ? s.monthEarned - s.monthSpent : 0;
  });
  protected readonly showBalance = computed(() => this.data()?.items.some(t => t.balanceAfter !== null) ?? false);

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);
    try {
      this.data.set(await this.api.statement(this.group(), this.page(), PAGE_SIZE));
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected setGroup(value: PointsGroup | null): void {
    this.group.set(value);
    this.page.set(1);
    this.load();
  }

  protected go(page: number): void {
    this.page.set(page);
    this.load();
  }

  protected labelOf(t: PointTransaction): string {
    return POINT_TYPE_LABELS[t.type] ?? t.type;
  }

  protected iconOf(t: PointTransaction): string {
    return POINT_TYPE_ICONS[t.type] ?? 'ti-star';
  }

  protected signed(value: number): string {
    return value > 0 ? `+${value.toLocaleString('en-US')}` : value < 0 ? `−${Math.abs(value).toLocaleString('en-US')}` : '0';
  }
}