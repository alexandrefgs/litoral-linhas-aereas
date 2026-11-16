import { Component, signal } from '@angular/core';
import { FleetAircraft } from './fleet-aircraft';
import { FleetTypes } from './fleet-types';

type FleetTab = 'aircraft' | 'types';

@Component({
  selector: 'app-admin-fleet',
  imports: [FleetAircraft, FleetTypes],
  template: `
    <div class="flex flex-col gap-6">
      <div>
        <h1 class="text-2xl font-bold text-ink">Fleet</h1>
        <p class="text-ink-muted">Register aircraft and types, track hours and manage maintenance.</p>
      </div>

      <div class="flex gap-1 border-b border-line">
        @for (option of tabs; track option.value) {
          <button type="button" (click)="tab.set(option.value)"
            class="-mb-px flex items-center gap-2 border-b-2 px-4 py-2.5 text-sm font-semibold"
            [class]="tab() === option.value ? 'border-primary text-primary' : 'border-transparent text-ink-muted hover:text-ink'">
            <i class="ti {{ option.icon }} text-lg" aria-hidden="true"></i>{{ option.label }}
          </button>
        }
      </div>

      @if (tab() === 'aircraft') {
        <app-fleet-aircraft />
      } @else {
        <app-fleet-types />
      }
    </div>
  `
})
export class AdminFleet {
  protected readonly tab = signal<FleetTab>('aircraft');

  protected readonly tabs: { value: FleetTab; label: string; icon: string }[] = [
    { value: 'aircraft', label: 'Aircraft', icon: 'ti-plane' },
    { value: 'types', label: 'Aircraft types', icon: 'ti-category' }
  ];
}