import { DecimalPipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FlightCrewMember } from '../../core/flights/flight.models';

@Component({
  selector: 'app-crew-card',
  imports: [DecimalPipe, RouterLink],
  host: { class: 'block' },
  template: `
    @let m = member();
    <div class="card-hover relative flex h-full flex-col items-center gap-2 overflow-hidden rounded-xl border border-line border-t-4 border-t-primary bg-gradient-to-b from-atlantico-800 to-atlantico-900 text-center text-white"
      [class]="compact() ? 'px-3 pb-4 pt-5' : 'px-5 pb-5 pt-6'">
      <img [src]="'images/ranks/' + m.rankCode.toLowerCase() + '.png'" alt="" class="absolute right-3 top-3 w-auto" [class]="compact() ? 'h-4' : 'h-6'" />
      <span class="flex items-center justify-center overflow-hidden rounded-full bg-primary font-bold ring-4 ring-white/20"
        [class]="compact() ? 'h-16 w-16 text-xl' : 'h-24 w-24 text-3xl'">
        @if (m.photoUrl) {
          <img [src]="m.photoUrl" alt="" class="h-full w-full object-cover" />
        } @else if (m.isVirtual) {
          <i class="ti ti-user-star" aria-hidden="true"></i>
        } @else {
          {{ initials(m.name) }}
        }
      </span>
      @if (m.pilotNumber) {
        <a [routerLink]="['/profile', m.pilotNumber]" class="font-bold hover:underline" [class]="compact() ? 'text-base' : 'text-xl'">{{ m.name }}</a>
      } @else {
        <p class="font-bold" [class]="compact() ? 'text-base' : 'text-xl'">{{ m.name }}</p>
      }
      <p class="text-xs text-white/70">{{ role() }}</p>
      <span class="rounded-md bg-sol-400 px-2.5 py-0.5 font-mono text-xs font-bold text-atlantico-900">{{ m.displayId ?? 'Virtual crew' }}</span>
      <div class="mt-1 grid w-full grid-cols-3 gap-1">
        <div><p class="font-bold" [class]="compact() ? 'text-sm' : 'text-lg'">{{ m.flights }}</p><p class="text-[10px] text-white/70">Flights</p></div>
        <div><p class="font-bold" [class]="compact() ? 'text-sm' : 'text-lg'">{{ m.minutes / 60 | number: '1.0-0' }}</p><p class="text-[10px] text-white/70">Hours</p></div>
        <div><p class="font-bold" [class]="compact() ? 'text-sm' : 'text-lg'">{{ m.points | number }}</p><p class="text-[10px] text-white/70">Points</p></div>
      </div>
    </div>
  `
})
export class CrewCard {
  readonly member = input.required<FlightCrewMember>();
  readonly role = input.required<string>();
  readonly compact = input(false);

  protected initials(name: string): string {
    return name.split(/[\s.]+/).filter(Boolean).map(p => p[0]).slice(0, 2).join('').toUpperCase();
  }
}