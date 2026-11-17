import { DecimalPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { SimBriefOfp } from '../../core/booking/booking.models';

@Component({
  selector: 'app-loadsheet',
  imports: [DecimalPipe],
  host: { class: 'panel flex flex-col' },
  template: `
    <div class="border-b border-line px-6 py-4">
      <h2 class="text-lg font-bold text-ink">Loadsheet</h2>
      <p class="text-xs text-ink-muted">From your SimBrief OFP · weights in {{ units() }}</p>
    </div>

    <div class="grid gap-6 p-6 lg:grid-cols-2">
      <div class="flex flex-col gap-4">
        <h3 class="text-sm font-semibold uppercase tracking-wide text-ink-muted">Load</h3>
        <div class="grid grid-cols-3 gap-3">
          @for (item of load(); track item.label) {
            <div class="rounded-lg bg-surface-alt px-3 py-2">
              <p class="text-[11px] text-ink-muted">{{ item.label }}</p>
              <p class="font-mono text-lg font-bold text-ink">{{ item.value !== null ? (item.value | number) : '—' }}</p>
            </div>
          }
        </div>

        <h3 class="mt-2 text-sm font-semibold uppercase tracking-wide text-ink-muted">Weights</h3>
        @for (item of weights(); track item.label) {
          <div class="flex flex-col gap-1">
            <div class="flex items-center justify-between text-sm">
              <span class="font-semibold text-ink">{{ item.label }}</span>
              <span class="font-mono text-ink">
                {{ item.value !== null ? (item.value | number) : '—' }}
                <span class="text-ink-muted">/ {{ item.max !== null ? (item.max | number) : '—' }}</span>
              </span>
            </div>
            <div class="progress">
              <div class="progress-bar" [class]="item.percent > 100 ? 'bg-danger' : item.percent > 95 ? 'bg-highlight' : 'bg-onda-500'" [style.width.%]="min(item.percent, 100)"></div>
            </div>
          </div>
        }
      </div>

      <div class="flex flex-col gap-2">
        <h3 class="text-sm font-semibold uppercase tracking-wide text-ink-muted">Fuel</h3>
        <table class="w-full text-sm">
          <tbody>
            @for (item of fuel(); track item.label) {
              <tr class="border-b border-line last:border-0" [class.font-bold]="item.total">
                <td class="py-2 text-ink">{{ item.label }}</td>
                <td class="py-2 text-right font-mono text-ink">{{ item.value !== null ? (item.value | number) : '—' }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `
})
export class Loadsheet {
  readonly ofp = input.required<SimBriefOfp>();

  protected readonly units = computed(() => (this.ofp().fuelUnits ?? 'kgs').toUpperCase());

  protected readonly load = computed(() => {
    const l = this.ofp().loadsheet;
    return [
      { label: 'Passengers', value: l.passengers },
      { label: 'Cargo', value: l.cargo },
      { label: 'Payload', value: l.payload }
    ];
  });

  protected readonly weights = computed(() => {
    const l = this.ofp().loadsheet;
    const percent = (value: number | null, max: number | null) => (value && max ? Math.round((value / max) * 100) : 0);
    return [
      { label: 'Zero fuel weight', value: l.zeroFuelWeight, max: l.maxZeroFuelWeight, percent: percent(l.zeroFuelWeight, l.maxZeroFuelWeight) },
      { label: 'Takeoff weight', value: l.takeoffWeight, max: l.maxTakeoffWeight, percent: percent(l.takeoffWeight, l.maxTakeoffWeight) },
      { label: 'Landing weight', value: l.landingWeight, max: l.maxLandingWeight, percent: percent(l.landingWeight, l.maxLandingWeight) }
    ];
  });

  protected readonly fuel = computed(() => {
    const l = this.ofp().loadsheet;
    return [
      { label: 'Taxi', value: l.taxiFuel, total: false },
      { label: 'Trip', value: l.tripFuel, total: false },
      { label: 'Contingency', value: l.contingencyFuel, total: false },
      { label: 'Alternate', value: l.alternateFuel, total: false },
      { label: 'Final reserve', value: l.reserveFuel, total: false },
      { label: 'Extra', value: l.extraFuel, total: false },
      { label: 'Takeoff fuel', value: l.takeoffFuel, total: true },
      { label: 'Block fuel', value: l.blockFuel, total: true }
    ];
  });

  protected min(a: number, b: number): number {
    return Math.min(a, b);
  }
}