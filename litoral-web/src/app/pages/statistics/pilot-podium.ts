import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RankedPilot } from '../../core/statistics/statistics.models';
import { formatMinutes } from '../../shared/utils/flight';

@Component({
  selector: 'app-pilot-podium',
  imports: [RouterLink],
  host: { class: 'panel card-hover flex flex-col gap-5 p-5' },
  template: `
    <div class="flex items-center justify-between gap-3">
      <h3 class="flex items-center gap-2 font-semibold text-ink">
        <i class="ti {{ icon() }} text-lg text-primary" aria-hidden="true"></i>{{ title() }}
      </h3>
      <span class="text-xs text-ink-muted">{{ subtitle() }}</span>
    </div>

    @if (pilots().length === 0) {
      <p class="py-10 text-center text-sm text-ink-muted">No approved flights this month yet. Be the first!</p>
    } @else {
      <div class="grid grid-cols-3 items-end gap-3">
        @for (slot of podium(); track slot.place) {
          @if (slot.pilot; as p) {
            <a [routerLink]="['/profile', p.pilotNumber]" class="group flex flex-col items-center gap-2 text-center">
              <span class="relative">
                <span class="flex items-center justify-center overflow-hidden rounded-full bg-primary font-bold text-on-primary ring-4"
                  [class]="slot.place === 1 ? 'h-20 w-20 text-xl ring-sol-400' : 'h-14 w-14 ring-line'">
                  @if (p.photoUrl) {
                    <img [src]="p.photoUrl" alt="" class="h-full w-full object-cover" />
                  } @else {
                    {{ initials(p.name) }}
                  }
                </span>
                @if (slot.place === 1) {
                  <i class="ti ti-crown absolute -top-4 left-1/2 -translate-x-1/2 text-xl text-sol-400" aria-hidden="true"></i>
                }
              </span>
              <span class="max-w-full truncate text-sm font-semibold text-ink group-hover:text-primary" [class.text-primary]="p.isMe">{{ p.name }}</span>
              <span class="font-mono text-xs text-ink-muted">{{ format(p.value) }}</span>
              <div class="flex w-full flex-col items-center justify-start rounded-t-lg pt-2 font-bold"
                [class]="slot.style">
                <span class="text-2xl">{{ slot.place }}</span>
                @if (bonus()[slot.place - 1]; as points) {
                  <span class="text-[11px] font-semibold opacity-80">+{{ points }}</span>
                }
              </div>
            </a>
          } @else {
            <div class="flex flex-col items-center justify-end gap-2">
              <div class="flex w-full items-start justify-center rounded-t-lg pt-2 text-2xl font-bold opacity-30" [class]="slot.style">{{ slot.place }}</div>
            </div>
          }
        }
      </div>

      @if (rest().length) {
        <ul class="flex flex-col divide-y divide-line border-t border-line">
          @for (p of rest(); track p.pilotNumber) {
            <li class="flex items-center gap-3 py-2 text-sm" [class.text-primary]="p.isMe">
              <span class="w-6 text-center font-mono text-ink-muted">{{ p.position }}</span>
              <a [routerLink]="['/profile', p.pilotNumber]" class="min-w-0 flex-1 truncate font-medium hover:underline" [class]="p.isMe ? 'text-primary' : 'text-ink'">{{ p.name }}</a>
              <span class="font-mono text-xs text-ink-muted">{{ format(p.value) }}</span>
            </li>
          }
        </ul>
      }
    }
  `
})
export class PilotPodium {
  readonly pilots = input.required<RankedPilot[]>();
  readonly title = input.required<string>();
  readonly subtitle = input('');
  readonly icon = input('ti-trophy');
  readonly unit = input<'minutes' | 'flights'>('minutes');
  readonly bonus = input<number[]>([]);

  protected readonly podium = computed(() => {
    const top = this.pilots();
    return [
      { place: 2, pilot: top[1] ?? null, style: 'h-20 bg-slate-300/70 text-slate-800' },
      { place: 1, pilot: top[0] ?? null, style: 'h-28 bg-sol-400 text-atlantico-900' },
      { place: 3, pilot: top[2] ?? null, style: 'h-16 bg-orange-300/80 text-orange-950' }
    ];
  });

  protected readonly rest = computed(() => this.pilots().slice(3));

  protected format(value: number): string {
    return this.unit() === 'minutes' ? formatMinutes(value) : `${value} ${value === 1 ? 'flight' : 'flights'}`;
  }

  protected initials(name: string): string {
    return name.split(/[\s.]+/).filter(Boolean).map(p => p[0]).slice(0, 2).join('').toUpperCase();
  }
}