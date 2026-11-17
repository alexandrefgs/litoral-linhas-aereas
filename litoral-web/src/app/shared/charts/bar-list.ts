import { Component, computed, input } from '@angular/core';
import { CountItem } from '../../core/statistics/statistics.models';

@Component({
  selector: 'app-bar-list',
  host: { class: 'panel card-hover flex flex-col gap-4 p-5' },
  template: `
    <h3 class="flex items-center gap-2 font-semibold text-ink">
      <i class="ti {{ icon() }} text-lg text-primary" aria-hidden="true"></i>{{ title() }}
    </h3>
    @if (rows().length === 0) {
      <p class="py-6 text-center text-sm text-ink-muted">No data yet.</p>
    } @else {
      <ul class="flex flex-col gap-3">
        @for (row of rows(); track row.key) {
          <li class="flex flex-col gap-1">
            <div class="flex items-center justify-between gap-3 text-sm">
              <span class="truncate font-medium text-ink" [class.font-mono]="mono()">{{ row.label }}</span>
              <span class="shrink-0 font-mono text-xs text-ink-muted">{{ row.flights }} {{ row.flights === 1 ? 'flight' : 'flights' }}</span>
            </div>
            <div class="progress"><div class="progress-bar bg-onda-500" [style.width.%]="row.percent"></div></div>
          </li>
        }
      </ul>
    }
  `
})
export class BarList {
  readonly items = input.required<CountItem[]>();
  readonly title = input.required<string>();
  readonly icon = input('ti-chart-bar');
  readonly mono = input(false);

  protected readonly rows = computed(() => {
    const max = Math.max(1, ...this.items().map(i => i.flights));
    return this.items().map(i => ({ ...i, percent: (i.flights / max) * 100 }));
  });
}