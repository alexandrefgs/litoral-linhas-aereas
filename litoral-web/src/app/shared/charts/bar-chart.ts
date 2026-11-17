import { Component, computed, input, signal } from '@angular/core';
import { PeriodPoint } from '../../core/statistics/statistics.models';
import { formatMinutes } from '../utils/flight';

type Metric = 'flights' | 'hours';

@Component({
  selector: 'app-bar-chart',
  host: { class: 'block' },
  template: `
    <div class="mb-4 flex items-center justify-between gap-3">
      <h3 class="font-semibold text-ink">{{ title() }}</h3>
      <div class="flex rounded-lg border border-line p-0.5 text-xs font-semibold">
        @for (m of metrics; track m.value) {
          <button type="button" class="rounded-md px-3 py-1" (click)="metric.set(m.value)"
            [class]="metric() === m.value ? 'bg-primary text-on-primary' : 'text-ink-muted hover:text-ink'">{{ m.label }}</button>
        }
      </div>
    </div>
    @if (max() === 0) {
      <p class="py-16 text-center text-sm text-ink-muted">No flights in this period yet.</p>
    } @else {
      <div class="flex h-56 items-end gap-1.5">
        @for (bar of bars(); track bar.label) {
          <div class="group relative flex h-full min-w-0 flex-1 flex-col items-center justify-end">
            <span class="pointer-events-none absolute -top-1 z-10 -translate-y-full whitespace-nowrap rounded-md bg-ink px-2 py-1 text-[11px] font-semibold text-surface opacity-0 shadow transition group-hover:opacity-100">
              {{ bar.label }} · {{ bar.text }}
            </span>
            <div class="w-full max-w-10 rounded-t-md bg-primary/80 transition group-hover:bg-primary" [style.height.%]="bar.height"></div>
          </div>
        }
      </div>
      <div class="mt-2 flex gap-1.5">
        @for (bar of bars(); track bar.label; let i = $index) {
          <span class="min-w-0 flex-1 truncate text-center text-[10px] text-ink-muted">{{ showLabel(i) ? bar.label : '' }}</span>
        }
      </div>
    }
  `
})
export class BarChart {
  readonly points = input.required<PeriodPoint[]>();
  readonly title = input('');

  protected readonly metrics: { value: Metric; label: string }[] = [
    { value: 'flights', label: 'Flights' },
    { value: 'hours', label: 'Hours' }
  ];
  protected readonly metric = signal<Metric>('flights');

  private readonly values = computed(() =>
    this.points().map(p => (this.metric() === 'flights' ? p.flights : p.minutes)));

  protected readonly max = computed(() => Math.max(0, ...this.values()));

  protected readonly bars = computed(() => {
    const max = this.max() || 1;
    return this.points().map((p, i) => {
      const value = this.values()[i];
      return {
        label: p.label,
        height: value === 0 ? 0 : Math.max(2, (value / max) * 100),
        text: this.metric() === 'flights' ? `${value} flights` : formatMinutes(value)
      };
    });
  });

  protected showLabel(index: number): boolean {
    const count = this.points().length;
    const step = count > 16 ? Math.ceil(count / 12) : 1;
    return index % step === 0;
  }
}