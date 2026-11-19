import { DatePipe } from '@angular/common';
import { Component, effect, inject, input, signal, untracked } from '@angular/core';
import { PilotAward } from '../../core/awards/awards.models';
import { AwardsService } from '../../core/awards/awards.service';

@Component({
  selector: 'app-profile-awards',
  imports: [DatePipe],
  template: `
    @if (loading()) {
      <p class="panel px-6 py-16 text-center text-ink-muted">Loading...</p>
    } @else if (failed()) {
      <div class="panel flex flex-col items-center gap-3 px-6 py-16 text-center">
        <p class="text-danger">Something went wrong. Try again.</p>
        <button type="button" class="btn-secondary" (click)="load()">Try again</button>
      </div>
    } @else if (awards().length === 0) {
      <div class="panel flex flex-col items-center gap-3 px-6 py-16 text-center">
        <i class="ti ti-award text-4xl text-ink-muted" aria-hidden="true"></i>
        <p class="text-ink-muted">{{ isOwner() ? 'You have no awards yet. Complete tours and take part in events to earn them.' : 'No awards yet.' }}</p>
      </div>
    } @else {
      <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        @for (award of awards(); track award.awardId) {
          <article class="panel card-hover flex flex-col items-center gap-3 p-6 text-center">
            <span class="flex h-24 w-24 items-center justify-center overflow-hidden rounded-full bg-gradient-to-br from-sol-200 to-sol-400 shadow-inner">
              @if (award.imageUrl) {
                <img [src]="award.imageUrl" alt="" class="h-full w-full object-cover" />
              } @else {
                <i class="ti {{ award.kind === 'Tour' ? 'ti-route' : 'ti-award' }} text-5xl text-atlantico-900" aria-hidden="true"></i>
              }
            </span>
            <div>
              <h3 class="font-bold text-ink">{{ award.name }}</h3>
              <p class="mt-1 text-sm text-ink-muted">{{ award.description }}</p>
              @if (award.note) {
                <p class="mt-2 text-sm italic text-ink">“{{ award.note }}”</p>
              }
            </div>
            <p class="mt-auto flex items-center gap-1.5 text-xs text-ink-muted">
              <i class="ti ti-calendar" aria-hidden="true"></i>{{ award.grantedAt | date: 'd MMM y' }}
              <span class="rounded-full bg-surface-alt px-2 py-0.5 font-semibold">{{ award.kind === 'Tour' ? 'Tour' : 'Staff' }}</span>
            </p>
          </article>
        }
      </div>
    }
  `
})
export class ProfileAwards {
  private readonly api = inject(AwardsService);

  readonly pilotNumber = input.required<number>();
  readonly isOwner = input(false);

  protected readonly awards = signal<PilotAward[]>([]);
  protected readonly loading = signal(true);
  protected readonly failed = signal(false);

  constructor() {
    effect(() => {
      this.pilotNumber();
      untracked(() => this.load());
    });
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.failed.set(false);
    try {
      this.awards.set(await this.api.forPilot(this.pilotNumber()));
    } catch {
      this.failed.set(true);
    } finally {
      this.loading.set(false);
    }
  }
}