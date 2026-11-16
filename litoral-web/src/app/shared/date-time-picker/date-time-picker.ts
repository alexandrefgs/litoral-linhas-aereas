import { Component, ElementRef, OnDestroy, computed, forwardRef, inject, input, signal, viewChild } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

interface Parts {
  year: number;
  month: number;
  day: number;
  hour: number;
  minute: number;
}

interface CalendarDay {
  key: string;
  day: number;
  month: number;
  year: number;
  inMonth: boolean;
}

type Segment = 'hour' | 'minute';

const MONTHS = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
const WEEKDAYS = ['S', 'M', 'T', 'W', 'T', 'F', 'S'];
const WEEKDAY_NAMES = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const POPOVER_WIDTH = 320;
const POPOVER_HEIGHT = 600;
const MARGIN = 8;

const pad = (n: number) => n.toString().padStart(2, '0');

function parse(value: string | null | undefined): Parts | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/.exec(value ?? '');
  if (!match) return null;
  const [, y, mo, d, h, mi] = match.map(Number);
  return { year: y, month: mo - 1, day: d, hour: h, minute: mi };
}

function format(p: Parts): string {
  return `${p.year}-${pad(p.month + 1)}-${pad(p.day)}T${pad(p.hour)}:${pad(p.minute)}`;
}

function dayKey(year: number, month: number, day: number): string {
  return `${year}-${pad(month + 1)}-${pad(day)}`;
}

function weekday(p: Parts): string {
  return WEEKDAY_NAMES[new Date(Date.UTC(p.year, p.month, p.day)).getUTCDay()];
}

@Component({
  selector: 'app-date-time-picker',
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => DateTimePicker), multi: true }],
  host: {
    class: 'block',
    '(document:mousedown)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'close()',
    '(window:resize)': 'close()'
  },
  template: `
    <button #trigger type="button" [id]="inputId()" (click)="toggle()" [disabled]="disabled()"
      class="field-input group flex items-center gap-3 text-left disabled:opacity-60"
      [class.border-primary]="open()" [class.ring-2]="open()" [class.ring-primary/30]="open()">
      <i class="ti ti-calendar-event text-lg text-ink-muted transition-colors group-hover:text-primary"
        [class.text-primary]="open()" aria-hidden="true"></i>
      @if (parts(); as p) {
        <span class="flex-1 text-ink">{{ p.day }} {{ monthShort(p.month) }} {{ p.year }}</span>
        <span class="rounded-md bg-surface-alt px-2 py-0.5 font-mono text-sm font-semibold text-ink">
          {{ pad(p.hour) }}:{{ pad(p.minute) }}{{ utc() ? 'Z' : '' }}
        </span>
      } @else {
        <span class="flex-1 text-ink-muted">{{ placeholder() }}</span>
      }
    </button>

    @if (open()) {
      <div #popover role="dialog" aria-label="Choose date and time"
        class="picker fixed z-[1300] overflow-hidden rounded-2xl border border-line bg-surface shadow-2xl"
        [style.top.px]="position().top" [style.left.px]="position().left" [style.width.px]="width">

        <div class="bg-primary px-5 py-4 text-on-primary">
          <p class="text-xs font-semibold uppercase tracking-widest opacity-80">{{ utc() ? 'Date & time · UTC' : 'Date & time' }}</p>
          @if (parts(); as p) {
            <p class="mt-1 text-2xl font-bold">{{ weekdayOf(p) }}, {{ p.day }} {{ monthShort(p.month) }}</p>
            <p class="font-mono text-sm opacity-90">{{ p.year }} · {{ pad(p.hour) }}:{{ pad(p.minute) }}{{ utc() ? 'Z' : '' }}</p>
          } @else {
            <p class="mt-1 text-2xl font-bold">Pick a date</p>
            <p class="text-sm opacity-90">Then set the time below</p>
          }
        </div>

        <div class="px-4 pt-3">
          <div class="flex items-center justify-between">
            <span class="pl-1 font-semibold text-ink">{{ monthLabel() }}</span>
            <div class="flex gap-1">
              <button type="button" class="nav-btn" aria-label="Previous month" (click)="shiftMonth(-1)">
                <i class="ti ti-chevron-left" aria-hidden="true"></i>
              </button>
              <button type="button" class="nav-btn" aria-label="Next month" (click)="shiftMonth(1)">
                <i class="ti ti-chevron-right" aria-hidden="true"></i>
              </button>
            </div>
          </div>

          <div class="mt-2 grid grid-cols-7 text-center text-[11px] font-semibold text-ink-muted">
            @for (w of weekdays; track $index) {
              <span class="py-1">{{ w }}</span>
            }
          </div>

          <div class="grid grid-cols-7 gap-y-1">
            @for (d of days(); track d.key) {
              <div class="flex justify-center">
                <button type="button" (click)="selectDay(d)" [disabled]="isBeforeMin(d)"
                  class="day-btn" [class]="dayClass(d)">
                  {{ d.day }}
                </button>
              </div>
            }
          </div>
        </div>

        <div class="mx-4 mt-3 flex items-center justify-between rounded-xl bg-surface-alt px-4 py-3">
          <span class="flex items-center gap-2 text-sm font-medium text-ink-muted">
            <i class="ti ti-clock" aria-hidden="true"></i>Time
          </span>
          <div class="flex items-center gap-1">
            @for (segment of segments; track segment; let last = $last) {
              <div class="flex flex-col items-center">
                <button type="button" class="step-btn" [attr.aria-label]="'Increase ' + segment" (click)="step(segment, 1)">
                  <i class="ti ti-chevron-up" aria-hidden="true"></i>
                </button>
                <input type="text" inputmode="numeric" maxlength="2" class="time-input"
                  [attr.aria-label]="segment === 'hour' ? 'Hours' : 'Minutes'"
                  [value]="segmentValue(segment)" (focus)="$any($event.target).select()"
                  (change)="typeSegment(segment, $any($event.target).value)"
                  (keydown.arrowUp)="$event.preventDefault(); step(segment, 1)"
                  (keydown.arrowDown)="$event.preventDefault(); step(segment, -1)"
                  (wheel)="$event.preventDefault(); step(segment, $event.deltaY < 0 ? 1 : -1)" />
                <button type="button" class="step-btn" [attr.aria-label]="'Decrease ' + segment" (click)="step(segment, -1)">
                  <i class="ti ti-chevron-down" aria-hidden="true"></i>
                </button>
              </div>
              @if (!last) {
                <span class="pb-0.5 text-2xl font-bold text-ink-muted">:</span>
              }
            }
            @if (utc()) {
              <span class="ml-1 text-sm font-semibold text-ink-muted">Z</span>
            }
          </div>
        </div>

        <div class="flex items-center gap-2 px-4 py-4">
          <button type="button" class="chip" (click)="clear()">Clear</button>
          <button type="button" class="chip" (click)="now()">Now</button>
          <button type="button" class="btn-primary ml-auto rounded-full px-5" (click)="close()">Done</button>
        </div>
      </div>
    }
  `,
  styles: `
    .picker { animation: picker-in 120ms ease-out; }
    @keyframes picker-in {
      from { opacity: 0; transform: translateY(-4px) scale(0.98); }
      to { opacity: 1; transform: none; }
    }
    .nav-btn {
      display: inline-flex; align-items: center; justify-content: center;
      width: 2rem; height: 2rem; border-radius: 9999px; color: var(--ink);
    }
    .nav-btn:hover { background: var(--surface-alt); color: var(--primary); }
    .day-btn {
      width: 2.25rem; height: 2.25rem; border-radius: 9999px; font-size: 0.875rem;
      transition: background-color 120ms, color 120ms;
    }
    .day-btn:disabled { cursor: not-allowed; opacity: 0.3; }
    .step-btn {
      display: inline-flex; align-items: center; justify-content: center;
      width: 2.5rem; height: 1.25rem; border-radius: 0.375rem; color: var(--ink-muted);
    }
    .step-btn:hover { color: var(--primary); background: var(--surface); }
    .time-input {
      width: 2.75rem; padding: 0.15rem 0; border-radius: 0.5rem; background: var(--surface);
      border: 1px solid var(--line); text-align: center; font-family: ui-monospace, monospace;
      font-size: 1.25rem; font-weight: 700; color: var(--ink); outline: none;
    }
    .time-input:focus { border-color: var(--primary); box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary) 25%, transparent); }
    .chip {
      border-radius: 9999px; border: 1px solid var(--line); padding: 0.375rem 0.875rem;
      font-size: 0.875rem; font-weight: 500; color: var(--ink);
    }
    .chip:hover { border-color: var(--primary); color: var(--primary); }
  `
})
export class DateTimePicker implements ControlValueAccessor, OnDestroy {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly trigger = viewChild.required<ElementRef<HTMLButtonElement>>('trigger');
  private readonly popover = viewChild<ElementRef<HTMLDivElement>>('popover');

  readonly inputId = input<string | null>(null);
  readonly placeholder = input('Select date and time');
  readonly utc = input(false);
  readonly min = input<string | null>(null);

  protected readonly pad = pad;
  protected readonly weekdays = WEEKDAYS;
  protected readonly segments: Segment[] = ['hour', 'minute'];
  protected readonly width = POPOVER_WIDTH;

  protected readonly parts = signal<Parts | null>(null);
  protected readonly open = signal(false);
  protected readonly disabled = signal(false);
  protected readonly view = signal({ year: 2026, month: 0 });
  protected readonly position = signal({ top: 0, left: 0 });

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};
  private readonly reposition = () => this.place();

  protected readonly monthLabel = computed(() => `${MONTHS[this.view().month]} ${this.view().year}`);

  protected readonly days = computed<CalendarDay[]>(() => {
    const { year, month } = this.view();
    const first = new Date(Date.UTC(year, month, 1));
    const start = new Date(Date.UTC(year, month, 1 - first.getUTCDay()));

    return Array.from({ length: 42 }, (_, i) => {
      const date = new Date(start.getTime() + i * 86_400_000);
      const d = { day: date.getUTCDate(), month: date.getUTCMonth(), year: date.getUTCFullYear() };
      return { ...d, key: dayKey(d.year, d.month, d.day), inMonth: d.month === month };
    });
  });

  writeValue(value: string | null): void {
    this.parts.set(parse(value));
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(disabled: boolean): void {
    this.disabled.set(disabled);
  }

  ngOnDestroy(): void {
    window.removeEventListener('scroll', this.reposition, true);
  }

  protected monthShort(month: number): string {
    return MONTHS[month].slice(0, 3);
  }

  protected weekdayOf(p: Parts): string {
    return weekday(p);
  }

  protected segmentValue(segment: Segment): string {
    const p = this.parts();
    return p ? pad(p[segment]) : '--';
  }

  protected toggle(): void {
    if (this.open()) {
      this.close();
      return;
    }

    const base = this.parts() ?? this.current();
    this.view.set({ year: base.year, month: base.month });
    this.place();
    this.open.set(true);
    window.addEventListener('scroll', this.reposition, true);
    setTimeout(() => this.place());
  }

  protected close(): void {
    if (!this.open()) return;
    this.open.set(false);
    window.removeEventListener('scroll', this.reposition, true);
    this.onTouched();
  }

  protected onDocumentClick(event: MouseEvent): void {
    if (this.open() && !this.host.nativeElement.contains(event.target as Node)) this.close();
  }

  protected shiftMonth(delta: number): void {
    const { year, month } = this.view();
    const date = new Date(Date.UTC(year, month + delta, 1));
    this.view.set({ year: date.getUTCFullYear(), month: date.getUTCMonth() });
  }

  protected selectDay(d: CalendarDay): void {
    const current = this.parts();
    this.update({ year: d.year, month: d.month, day: d.day, hour: current?.hour ?? 12, minute: current?.minute ?? 0 });
    if (!d.inMonth) this.view.set({ year: d.year, month: d.month });
  }

  protected step(segment: Segment, delta: number): void {
    const base = this.parts() ?? this.current();
    const size = segment === 'hour' ? 24 : 60;
    this.update({ ...base, [segment]: (base[segment] + delta + size) % size });
  }

  protected typeSegment(segment: Segment, raw: string): void {
    const value = Number.parseInt(raw, 10);
    const max = segment === 'hour' ? 23 : 59;
    const base = this.parts() ?? this.current();

    if (Number.isNaN(value)) {
      this.parts.set({ ...base });
      return;
    }

    this.update({ ...base, [segment]: Math.min(max, Math.max(0, value)) });
  }

  protected now(): void {
    const now = this.current();
    this.update(now);
    this.view.set({ year: now.year, month: now.month });
  }

  protected clear(): void {
    this.parts.set(null);
    this.onChange('');
    this.close();
  }

  protected isBeforeMin(d: CalendarDay): boolean {
    const min = parse(this.min());
    return !!min && d.key < dayKey(min.year, min.month, min.day);
  }

  protected dayClass(d: CalendarDay): string {
    const p = this.parts();
    const now = this.current();

    if (p && d.key === dayKey(p.year, p.month, p.day)) return 'bg-primary font-bold text-on-primary shadow-md';
    if (d.key === dayKey(now.year, now.month, now.day)) return 'font-bold text-primary ring-1 ring-primary hover:bg-surface-alt';
    return d.inMonth ? 'text-ink hover:bg-surface-alt' : 'text-ink-muted/40 hover:bg-surface-alt';
  }

  private update(parts: Parts): void {
    this.parts.set(parts);
    this.onChange(format(parts));
  }

  private current(): Parts {
    const d = new Date();
    return this.utc()
      ? { year: d.getUTCFullYear(), month: d.getUTCMonth(), day: d.getUTCDate(), hour: d.getUTCHours(), minute: d.getUTCMinutes() }
      : { year: d.getFullYear(), month: d.getMonth(), day: d.getDate(), hour: d.getHours(), minute: d.getMinutes() };
  }

  private place(): void {
    const rect = this.trigger().nativeElement.getBoundingClientRect();
    const height = this.popover()?.nativeElement.offsetHeight || POPOVER_HEIGHT;
    const below = rect.bottom + MARGIN;
    const above = rect.top - height - MARGIN;
    const top = below + height <= window.innerHeight
      ? below
      : above >= MARGIN
        ? above
        : Math.max(MARGIN, window.innerHeight - height - MARGIN);
    const left = Math.max(MARGIN, Math.min(rect.left, window.innerWidth - POPOVER_WIDTH - MARGIN));
    this.position.set({ top, left });
  }
}