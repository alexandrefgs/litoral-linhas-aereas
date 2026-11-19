import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { PublicStaffPosition } from '../../core/reference/reference.models';
import { ReferenceService } from '../../core/reference/reference.service';

const TEAMS = [
  { team: 'Executive', label: 'Executive Team', icon: 'ti-crown', text: 'Sets the direction of the airline and appoints the staff.' },
  { team: 'Operations', label: 'Operations Team', icon: 'ti-settings', text: 'Keeps pilots, aircraft, routes and the daily operation running.' },
  { team: 'Safety', label: 'Safety Team', icon: 'ti-shield-check', text: 'Reviews flight reports and keeps the scoring fair for everyone.' }
] as const;

const DESCRIPTIONS: Record<string, string> = {
  CEO_FOUNDER: 'Leads Litoral and has the final word on every area of the airline.',
  CHAIRMAN: 'Guides the long-term direction of the airline and appoints the staff.',
  VICE_CHAIRMAN: 'Supports the Chairman and helps organize the staff.',
  GENERAL_DIRECTOR: 'Follows the airline results through reports and the activity log.',
  HEAD_OF_HR: 'Reviews applications and takes care of pilot accounts.',
  DIRECTOR_OF_OPERATIONS: 'Plans special flights and the day-to-day operation.',
  FLEET_MANAGER: 'Manages the aircraft, their location, maintenance and status.',
  ROUTE_MANAGER: 'Builds the route network and manages airports and bases.',
  SAFETY_MANAGER: 'Reviews flight reports and keeps the scoring rules fair.'
};

@Component({
  selector: 'app-staff-page',
  imports: [RouterLink],
  template: `
    <section class="relative isolate overflow-hidden border-b border-line bg-atlantico-900 text-white">
      <svg class="absolute inset-0 -z-10 h-full w-full opacity-40" viewBox="0 0 1440 400" preserveAspectRatio="xMidYMid slice" aria-hidden="true">
        <path d="M-50 340 Q500 40 1500 90" fill="none" stroke="#2EC4B6" stroke-width="2" stroke-dasharray="4 14" />
        <path d="M-50 400 Q700 180 1500 220" fill="none" stroke="#1D6FD8" stroke-width="2" stroke-dasharray="4 14" />
      </svg>
      <div class="mx-auto max-w-6xl px-6 py-16">
        <p class="text-xs font-semibold uppercase tracking-[0.2em] text-sol-300">Litoral Linhas Aéreas</p>
        <h1 class="mt-2 text-4xl font-semibold tracking-tight md:text-5xl">Meet the staff</h1>
        <p class="mt-4 max-w-2xl text-white/80">Every member of the staff is also a Litoral pilot. They volunteer their time to keep the airline running and the experience realistic.</p>
      </div>
    </section>

    <section class="mx-auto flex max-w-6xl flex-col gap-10 px-6 py-12">
      @if (loading()) {
        <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (failed()) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-12 text-center">
          <p class="text-danger">Something went wrong. Try again.</p>
          <button type="button" class="btn-secondary" (click)="load()">Try again</button>
        </div>
      } @else {
        @for (group of groups(); track group.team) {
          <div class="flex flex-col gap-4">
            <div class="flex items-start gap-3">
              <span class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-2xl text-primary">
                <i class="ti {{ group.icon }}" aria-hidden="true"></i>
              </span>
              <div>
                <h2 class="text-xl font-bold text-ink">{{ group.label }}</h2>
                <p class="text-sm text-ink-muted">{{ group.text }}</p>
              </div>
            </div>

            <div class="grid items-start gap-4 sm:grid-cols-2 lg:grid-cols-3">
              @for (position of group.positions; track position.code) {
                <article class="panel flex h-full flex-col gap-4 p-5">
                  <div>
                    <h3 class="font-bold text-ink">{{ position.name }}</h3>
                    <p class="mt-1 text-sm text-ink-muted">{{ description(position.code) }}</p>
                  </div>
                  <div class="mt-auto flex flex-col gap-2">
                    @for (member of position.holders; track member.pilotNumber) {
                      <div class="flex items-center gap-3 rounded-lg bg-surface-alt px-3 py-2">
                        <span class="flex h-10 w-10 shrink-0 items-center justify-center overflow-hidden rounded-full bg-primary text-xs font-bold text-on-primary">
                          @if (member.photoUrl) {
                            <img [src]="member.photoUrl" alt="" class="h-full w-full object-cover" />
                          } @else {
                            {{ initials(member.name) }}
                          }
                        </span>
                        <div class="min-w-0 flex-1">
                          @if (auth.isAuthenticated()) {
                            <a [routerLink]="['/profile', member.pilotNumber]" class="block truncate font-semibold text-ink hover:text-primary hover:underline">{{ member.name }}</a>
                          } @else {
                            <p class="truncate font-semibold text-ink">{{ member.name }}</p>
                          }
                          <p class="text-xs text-ink-muted"><span class="font-mono">{{ member.displayId }}</span> · {{ member.rank }}</p>
                        </div>
                      </div>
                    } @empty {
                      <p class="rounded-lg border border-dashed border-line px-3 py-3 text-center text-sm text-ink-muted">Vacant</p>
                    }
                  </div>
                </article>
              }
            </div>
          </div>
        }

        <div class="panel flex flex-col items-start justify-between gap-4 p-6 md:flex-row md:items-center">
          <div>
            <h2 class="text-lg font-bold text-ink">Want to help run Litoral?</h2>
            <p class="text-sm text-ink-muted">Staff members are chosen among active pilots. Fly with us, get to know the operation and talk to the staff on Discord.</p>
          </div>
          <a routerLink="/career" class="btn-secondary flex shrink-0 items-center gap-2">See the career <i class="ti ti-arrow-right" aria-hidden="true"></i></a>
        </div>
      }
    </section>
  `
})
export class StaffPage implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly reference = inject(ReferenceService);

  protected readonly positions = signal<PublicStaffPosition[]>([]);
  protected readonly loading = signal(true);
  protected readonly failed = signal(false);

  protected readonly groups = computed(() =>
    TEAMS.map(t => ({ ...t, positions: this.positions().filter(p => p.team === t.team) })).filter(g => g.positions.length > 0));

  ngOnInit(): void {
    this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.failed.set(false);
    try {
      this.positions.set(await this.reference.getStaff());
    } catch {
      this.failed.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected description(code: string): string {
    return DESCRIPTIONS[code] ?? '';
  }

  protected initials(name: string): string {
    return name.split(' ').filter(Boolean).slice(0, 2).map(p => p[0]).join('').toUpperCase();
  }
}