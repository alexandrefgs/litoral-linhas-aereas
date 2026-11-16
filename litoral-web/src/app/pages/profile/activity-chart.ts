import { Component, computed, input, signal } from '@angular/core';
import { ActivityYear } from '../../core/profile/profile.models';

type SeriesKey = 'currentFlights' | 'currentHours' | 'previousFlights' | 'previousHours';

interface Series {
  key: SeriesKey;
  label: string;
  kind: 'bar' | 'line';
  color: string;
  values: number[];
}

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
const WIDTH = 1000;
const HEIGHT = 300;
const LEFT = 44;
const RIGHT = 12;
const TOP = 12;
const BOTTOM = 28;
const PLOT_W = WIDTH - LEFT - RIGHT;
const PLOT_H = HEIGHT - TOP - BOTTOM;
const SLOT = PLOT_W / 12;
const BAR_W = 16;

@Component({
  selector: 'app-activity-chart',
  host: { class: 'block' },
  template: `
    <div class="relative">
      <svg [attr.viewBox]="'0 0 ' + width + ' ' + height" class="h-auto w-full select-none" role="img" aria-label="Annual activity chart">
        @for (tick of scale().ticks; track tick) {
          <line [attr.x1]="left" [attr.x2]="width - right" [attr.y1]="y(tick)" [attr.y2]="y(tick)"
            class="stroke-line" stroke-dasharray="6 6" />
          <text [attr.x]="left - 10" [attr.y]="y(tick) + 4" text-anchor="end" class="fill-ink-muted text-[11px]">{{ tick }}</text>
        }

        @for (month of months; track $index; let i = $index) {
          <text [attr.x]="center(i)" [attr.y]="height - 6" text-anchor="middle" class="fill-ink-muted text-[11px]">{{ month }}</text>
        }

        @if (hovered() !== null) {
          <rect [attr.x]="left + hovered()! * slot" [attr.y]="top" [attr.width]="slot" [attr.height]="plotH" class="fill-surface-alt" opacity="0.6" />
        }

        @for (s of visibleBars(); track s.key; let b = $index) {
          @for (value of s.values; track $index; let i = $index) {
            @if (value > 0) {
              <rect [attr.x]="barX(i, b, visibleBars().length)" [attr.y]="y(value)" [attr.width]="barW"
                [attr.height]="baseline - y(value)" [attr.fill]="s.color" rx="3" opacity="0.85" />
            }
          }
        }

        @for (s of visibleLines(); track s.key) {
          <path [attr.d]="linePath(s.values)" fill="none" [attr.stroke]="s.color" stroke-width="3" stroke-linecap="round" />
          @for (value of s.values; track $index; let i = $index) {
            <circle [attr.cx]="center(i)" [attr.cy]="y(value)" r="4.5" class="fill-surface" [attr.stroke]="s.color" stroke-width="2.5" />
          }
        }

        @for (month of months; track $index; let i = $index) {
          <rect [attr.x]="left + i * slot" [attr.y]="top" [attr.width]="slot" [attr.height]="plotH" fill="transparent"
            (mouseenter)="hovered.set(i)" (mouseleave)="hovered.set(null)" />
        }
      </svg>

      @if (hovered() !== null) {
        <div class="pointer-events-none absolute top-2 z-10 min-w-40 -translate-x-1/2 rounded-lg border border-line bg-surface px-3 py-2 text-xs shadow-lg"
          [style.left.%]="(center(hovered()!) / width) * 100">
          <p class="mb-1 font-semibold text-ink">{{ fullMonths[hovered()!] }}</p>
          @for (s of visibleSeries(); track s.key) {
            <p class="flex items-center justify-between gap-4 text-ink-muted">
              <span class="flex items-center gap-1.5"><span class="h-2 w-2 rounded-full" [style.background]="s.color"></span>{{ s.label }}</span>
              <span class="font-semibold text-ink">{{ s.values[hovered()!] }}</span>
            </p>
          }
        </div>
      }
    </div>

    <div class="mt-3 flex flex-wrap justify-center gap-2">
      @for (s of series(); track s.key) {
        <button type="button" (click)="toggle(s.key)"
          class="flex items-center gap-2 rounded-md border border-line px-3 py-1.5 text-xs font-medium transition-colors hover:bg-surface-alt"
          [class]="hidden().has(s.key) ? 'text-ink-muted line-through opacity-60' : 'text-ink'">
          <span class="h-2.5 w-2.5 rounded-full" [style.background]="s.color"></span>
          {{ s.label }}
        </button>
      }
    </div>
  `
})
export class ActivityChart {
  readonly current = input<ActivityYear | null>(null);
  readonly previous = input<ActivityYear | null>(null);
  readonly currentYear = input.required<number>();

  protected readonly width = WIDTH;
  protected readonly height = HEIGHT;
  protected readonly left = LEFT;
  protected readonly right = RIGHT;
  protected readonly top = TOP;
  protected readonly slot = SLOT;
  protected readonly plotH = PLOT_H;
  protected readonly barW = BAR_W;
  protected readonly baseline = TOP + PLOT_H;
  protected readonly months = MONTHS;
  protected readonly fullMonths = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

  protected readonly hovered = signal<number | null>(null);
  protected readonly hidden = signal(new Set<SeriesKey>());

  protected readonly series = computed<Series[]>(() => {
    const year = this.currentYear();
    const flights = (a: ActivityYear | null) => MONTHS.map((_, i) => a?.months[i]?.flights ?? 0);
    const hours = (a: ActivityYear | null) => MONTHS.map((_, i) => Math.round(((a?.months[i]?.minutes ?? 0) / 60) * 10) / 10);

    return [
      { key: 'currentFlights', label: `Flights in ${year}`, kind: 'bar', color: '#1D6FD8', values: flights(this.current()) },
      { key: 'currentHours', label: `Hours in ${year}`, kind: 'line', color: '#2EC4B6', values: hours(this.current()) },
      { key: 'previousFlights', label: `Flights in ${year - 1}`, kind: 'bar', color: '#7C8BA1', values: flights(this.previous()) },
      { key: 'previousHours', label: `Hours in ${year - 1}`, kind: 'line', color: '#F5B83D', values: hours(this.previous()) }
    ];
  });

  protected readonly visibleSeries = computed(() => this.series().filter(s => !this.hidden().has(s.key)));
  protected readonly visibleBars = computed(() => this.visibleSeries().filter(s => s.kind === 'bar'));
  protected readonly visibleLines = computed(() => this.visibleSeries().filter(s => s.kind === 'line'));

  protected readonly scale = computed(() => {
    const max = Math.max(1, ...this.visibleSeries().flatMap(s => s.values));
    const rawStep = max / 5;
    const magnitude = 10 ** Math.floor(Math.log10(rawStep));
    const step = [1, 2, 2.5, 5, 10].map(f => f * magnitude).find(s => s >= rawStep) ?? rawStep;
    const top = Math.ceil(max / step) * step;
    const ticks: number[] = [];
    for (let t = 0; t <= top + step / 2; t += step) ticks.push(Math.round(t * 10) / 10);
    return { max: top, ticks };
  });

  protected toggle(key: SeriesKey): void {
    this.hidden.update(set => {
      const next = new Set(set);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  }

  protected y(value: number): number {
    return TOP + PLOT_H - (value / this.scale().max) * PLOT_H;
  }

  protected center(index: number): number {
    return LEFT + SLOT * index + SLOT / 2;
  }

  protected barX(index: number, bar: number, total: number): number {
    const gap = 4;
    const groupWidth = total * BAR_W + (total - 1) * gap;
    return this.center(index) - groupWidth / 2 + bar * (BAR_W + gap);
  }

  protected linePath(values: number[]): string {
    const points = values.map((v, i) => [this.center(i), this.y(v)] as const);
    const clamp = (y: number) => Math.min(y, this.baseline);
    let d = `M ${points[0][0]} ${points[0][1]}`;

    for (let i = 0; i < points.length - 1; i++) {
      const p0 = points[Math.max(0, i - 1)];
      const p1 = points[i];
      const p2 = points[i + 1];
      const p3 = points[Math.min(points.length - 1, i + 2)];
      const c1x = p1[0] + (p2[0] - p0[0]) / 6;
      const c1y = clamp(p1[1] + (p2[1] - p0[1]) / 6);
      const c2x = p2[0] - (p3[0] - p1[0]) / 6;
      const c2y = clamp(p2[1] - (p3[1] - p1[1]) / 6);
      d += ` C ${c1x} ${c1y}, ${c2x} ${c2y}, ${p2[0]} ${p2[1]}`;
    }

    return d;
  }
}