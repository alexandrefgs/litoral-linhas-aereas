import { Component, computed, input } from '@angular/core';
import { TrackPoint } from '../../core/pirep/pirep.models';

const WIDTH = 1000;
const HEIGHT = 300;
const PAD = { top: 16, right: 56, bottom: 32, left: 64 };
const ALTITUDE_COLOR = '#2EC4B6';
const SPEED_COLOR = '#F5B83D';

@Component({
  selector: 'app-flight-chart',
  template: `
    @if (chart(); as c) {
      <svg [attr.viewBox]="'0 0 ' + width + ' ' + height" class="h-auto w-full" role="img" aria-label="Altitude and ground speed over time">
        @for (tick of c.altitudeTicks; track tick.value) {
          <line [attr.x1]="pad.left" [attr.x2]="width - pad.right" [attr.y1]="tick.y" [attr.y2]="tick.y" class="stroke-line" stroke-width="1" stroke-dasharray="3 4" />
          <text [attr.x]="pad.left - 8" [attr.y]="tick.y + 4" text-anchor="end" class="fill-ink-muted text-[11px]">{{ tick.label }}</text>
        }
        @for (tick of c.speedTicks; track tick.value) {
          <text [attr.x]="width - pad.right + 8" [attr.y]="tick.y + 4" class="fill-ink-muted text-[11px]">{{ tick.value }}</text>
        }
        @for (tick of c.timeTicks; track tick.x) {
          <text [attr.x]="tick.x" [attr.y]="height - 8" text-anchor="middle" class="fill-ink-muted text-[11px]">{{ tick.label }}</text>
        }

        <polyline [attr.points]="c.altitude" fill="none" [attr.stroke]="altitudeColor" stroke-width="2.5" stroke-linejoin="round" />
        <polyline [attr.points]="c.speed" fill="none" [attr.stroke]="speedColor" stroke-width="2.5" stroke-linejoin="round" />

        <text x="14" y="150" transform="rotate(-90 14 150)" text-anchor="middle" class="fill-ink-muted text-[11px]">Altitude (ft)</text>
        <text x="990" y="150" transform="rotate(90 990 150)" text-anchor="middle" class="fill-ink-muted text-[11px]">Speed (kt)</text>
      </svg>

      <div class="mt-3 flex flex-wrap items-center gap-x-6 gap-y-1 text-xs text-ink-muted">
        <span class="flex items-center gap-2"><span class="h-0.5 w-5 rounded" [style.background]="altitudeColor"></span>Altitude · max {{ c.maxAltitude.toLocaleString('en-US') }} ft</span>
        <span class="flex items-center gap-2"><span class="h-0.5 w-5 rounded" [style.background]="speedColor"></span>Ground speed · max {{ c.maxSpeed }} kt</span>
      </div>
    } @else {
      <div class="flex h-48 flex-col items-center justify-center gap-2 text-ink-muted">
        <i class="ti ti-chart-line text-4xl" aria-hidden="true"></i>
        <p class="text-sm">No telemetry recorded for this flight.</p>
      </div>
    }
  `
})
export class FlightChart {
  readonly track = input.required<TrackPoint[]>();

  protected readonly width = WIDTH;
  protected readonly height = HEIGHT;
  protected readonly pad = PAD;
  protected readonly altitudeColor = ALTITUDE_COLOR;
  protected readonly speedColor = SPEED_COLOR;

  protected readonly chart = computed(() => {
    const points = this.track();
    if (points.length < 2) return null;

    const times = points.map(p => new Date(p.at).getTime());
    const start = times[0];
    const span = Math.max(1, times[times.length - 1] - start);

    const maxAltitude = Math.max(...points.map(p => p.altitudeFt));
    const maxSpeed = Math.max(...points.map(p => p.groundSpeedKt));
    const altitudeTop = Math.max(5000, Math.ceil(maxAltitude / 5000) * 5000);
    const speedTop = Math.max(100, Math.ceil(maxSpeed / 100) * 100);

    const plotWidth = WIDTH - PAD.left - PAD.right;
    const plotHeight = HEIGHT - PAD.top - PAD.bottom;
    const x = (t: number) => PAD.left + ((t - start) / span) * plotWidth;
    const y = (value: number, top: number) => PAD.top + plotHeight - (value / top) * plotHeight;

    const line = (value: (p: TrackPoint) => number, top: number) =>
      points.map((p, i) => `${x(times[i]).toFixed(1)},${y(value(p), top).toFixed(1)}`).join(' ');

    const altitudeStep = altitudeTop / 5;
    const speedStep = speedTop / 5;

    return {
      maxAltitude,
      maxSpeed,
      altitude: line(p => p.altitudeFt, altitudeTop),
      speed: line(p => p.groundSpeedKt, speedTop),
      altitudeTicks: Array.from({ length: 6 }, (_, i) => {
        const value = i * altitudeStep;
        return { value, y: y(value, altitudeTop), label: value.toLocaleString('en-US') };
      }),
      speedTicks: Array.from({ length: 6 }, (_, i) => {
        const value = i * speedStep;
        return { value, y: y(value, speedTop) };
      }),
      timeTicks: Array.from({ length: 7 }, (_, i) => {
        const t = start + (span * i) / 6;
        const date = new Date(t);
        return {
          x: x(t),
          label: `${date.getUTCHours().toString().padStart(2, '0')}:${date.getUTCMinutes().toString().padStart(2, '0')}`
        };
      })
    };
  });
}