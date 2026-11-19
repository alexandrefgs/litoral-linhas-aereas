import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { CareerRank } from '../../core/reference/reference.models';
import { ReferenceService } from '../../core/reference/reference.service';

const RANK_ICONS = ['ti-user', 'ti-star', 'ti-crown'];

@Component({
  selector: 'app-career-page',
  imports: [DecimalPipe, RouterLink],
  template: `
    <section class="relative isolate overflow-hidden border-b border-line bg-atlantico-900 text-white">
      <svg class="absolute inset-0 -z-10 h-full w-full" viewBox="0 0 1440 400" preserveAspectRatio="xMidYMid slice" aria-hidden="true">
        <circle cx="1180" cy="120" r="70" fill="#FFD166" opacity="0.85" />
        <path d="M600 360 Q900 60 1300 60" fill="none" stroke="#FFFFFF" stroke-opacity="0.35" stroke-width="2" stroke-dasharray="2 10" stroke-linecap="round" />
        <path d="M0 340 Q180 310 360 340 T720 340 T1080 340 T1440 340 V400 H0 Z" fill="#2EC4B6" opacity="0.35" />
      </svg>
      <div class="mx-auto max-w-6xl px-6 py-16">
        <p class="text-xs font-semibold uppercase tracking-[0.2em] text-sol-300">Career at Litoral</p>
        <h1 class="mt-2 max-w-2xl text-4xl font-semibold tracking-tight md:text-5xl">From the right seat to the left seat</h1>
        <p class="mt-4 max-w-2xl text-white/80">Every pilot starts as a First Officer. Fly online, earn points with good flying and reach the command of a Litoral 737.</p>
      </div>
    </section>

    <section class="mx-auto flex max-w-6xl flex-col gap-12 px-6 py-12">
      <div class="flex flex-col gap-4">
        <h2 class="text-2xl font-bold text-ink">Ranks</h2>
        @if (loading()) {
          <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
        } @else if (failed()) {
          <div class="panel flex flex-col items-center gap-3 px-6 py-12 text-center">
            <p class="text-danger">Something went wrong. Try again.</p>
            <button type="button" class="btn-secondary" (click)="load()">Try again</button>
          </div>
        } @else {
          <ol class="grid gap-4 md:grid-cols-2">
            @for (rank of ranks(); track rank.code; let i = $index; let first = $first) {
              <li class="panel relative flex flex-col gap-5 overflow-hidden p-6">
                <span class="absolute right-5 top-4 text-6xl font-black text-ink/5">{{ rank.code }}</span>
                <div class="flex items-center gap-4">
                  <span class="flex h-12 w-12 items-center justify-center rounded-xl text-2xl"
                    [class]="first ? 'bg-primary/10 text-primary' : 'bg-sol-400/25 text-ink'">
                    <i class="ti {{ icon(i) }}" aria-hidden="true"></i>
                  </span>
                  <div>
                    <p class="text-xs font-semibold uppercase tracking-widest text-ink-muted">Level {{ rank.level }} · {{ rank.code }}</p>
                    <h3 class="text-xl font-bold text-ink">{{ rank.name }}</h3>
                  </div>
                </div>

                <div>
                  <p class="text-xs font-semibold uppercase tracking-wide text-ink-muted">Requirements</p>
                  @if (rank.minFlightHours === 0 && rank.minPoints === 0) {
                    <p class="mt-1 font-medium text-ink">Every pilot who joins Litoral</p>
                  } @else {
                    <div class="mt-2 flex flex-wrap gap-2">
                      <span class="flex items-center gap-1.5 rounded-full bg-surface-alt px-3 py-1 text-sm font-semibold text-ink">
                        <i class="ti ti-clock text-primary" aria-hidden="true"></i>{{ rank.minFlightHours | number }} flight hours
                      </span>
                      <span class="flex items-center gap-1.5 rounded-full bg-surface-alt px-3 py-1 text-sm font-semibold text-ink">
                        <i class="ti ti-star text-primary" aria-hidden="true"></i>{{ rank.minPoints | number }} points
                      </span>
                    </div>
                  }
                </div>

                <dl class="mt-auto grid grid-cols-2 gap-3 border-t border-line pt-4 text-sm">
                  <div>
                    <dt class="text-ink-muted">Pilots today</dt>
                    <dd class="text-lg font-bold text-ink">{{ rank.pilots | number }}</dd>
                  </div>
                  <div>
                    <dt class="text-ink-muted">Airports available</dt>
                    <dd class="text-lg font-bold text-ink">{{ rank.airports | number }}</dd>
                  </div>
                </dl>
              </li>
            }
          </ol>
        }
      </div>

      <div class="flex flex-col gap-4">
        <h2 class="text-2xl font-bold text-ink">How you progress</h2>
        <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          @for (step of steps; track step.title; let i = $index) {
            <article class="panel flex flex-col gap-3 p-5">
              <div class="flex items-center justify-between">
                <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-primary/10 text-xl text-primary">
                  <i class="ti {{ step.icon }}" aria-hidden="true"></i>
                </span>
                <span class="text-xs font-semibold uppercase tracking-widest text-ink-muted">0{{ i + 1 }}</span>
              </div>
              <h3 class="font-semibold text-ink">{{ step.title }}</h3>
              <p class="text-sm leading-relaxed text-ink-muted">{{ step.text }}</p>
            </article>
          }
        </div>
      </div>

      <div class="grid gap-4 md:grid-cols-2">
        <article class="panel flex flex-col gap-2 p-6">
          <h3 class="flex items-center gap-2 font-bold text-ink"><i class="ti ti-chevrons-down text-xl text-danger" aria-hidden="true"></i>Keeping your rank</h3>
          <p class="text-sm leading-relaxed text-ink-muted">
            Rejected flight reports cost points and your balance can become negative.
            @if (captain(); as c) {
              A {{ c.name }} whose balance falls below {{ c.minPoints | number }} points goes back to {{ ranks()[0].name }} automatically.
            }
            Long periods without flying also cost points, as described in the Regulations.
          </p>
        </article>
        <article class="panel flex flex-col gap-2 p-6">
          <h3 class="flex items-center gap-2 font-bold text-ink"><i class="ti ti-trophy text-xl text-sol-500" aria-hidden="true"></i>Pilots of the month</h3>
          <p class="text-sm leading-relaxed text-ink-muted">Every month the top 3 pilots in flight hours and in number of flights earn a points bonus, a shortcut to the left seat.</p>
        </article>
        <article class="panel flex flex-col gap-2 p-6">
          <h3 class="flex items-center gap-2 font-bold text-ink"><i class="ti ti-calendar-star text-xl text-primary" aria-hidden="true"></i>Special flights</h3>
          <p class="text-sm leading-relaxed text-ink-muted">Charters and special operations open from time to time, some of them only for higher ranks.</p>
        </article>
        <article class="panel flex flex-col gap-2 p-6">
          <h3 class="flex items-center gap-2 font-bold text-ink"><i class="ti ti-users text-xl text-primary" aria-hidden="true"></i>Beyond the cockpit</h3>
          <p class="text-sm leading-relaxed text-ink-muted">Active pilots can be invited to the staff and help run the airline.</p>
          <a routerLink="/staff" class="mt-1 inline-flex w-fit items-center gap-2 border-b-2 border-primary pb-0.5 text-sm font-medium text-ink hover:text-primary">
            Meet the staff <i class="ti ti-arrow-right" aria-hidden="true"></i>
          </a>
        </article>
      </div>

      <div class="flex flex-col items-center justify-between gap-4 rounded-2xl bg-primary px-6 py-10 text-center md:flex-row md:text-left">
        <div>
          <h2 class="text-2xl font-semibold text-on-primary">{{ auth.isAuthenticated() ? 'Your next flight counts' : 'Ready to start as First Officer?' }}</h2>
          <p class="mt-1 text-on-primary/80">{{ auth.isAuthenticated() ? 'Book a flight and keep building your hours and points.' : 'Read the Regulations and send your application.' }}</p>
        </div>
        @if (auth.isAuthenticated()) {
          <a routerLink="/book" class="rounded-lg bg-sol-300 px-6 py-3 font-semibold text-atlantico-900 hover:bg-sol-200">Book a flight</a>
        } @else {
          <a routerLink="/regulations" fragment="join" class="rounded-lg bg-sol-300 px-6 py-3 font-semibold text-atlantico-900 hover:bg-sol-200">Join the crew</a>
        }
      </div>
    </section>
  `
})
export class CareerPage implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly reference = inject(ReferenceService);

  protected readonly ranks = signal<CareerRank[]>([]);
  protected readonly loading = signal(true);
  protected readonly failed = signal(false);

  protected readonly captain = computed(() => {
    const list = this.ranks();
    return list.length > 1 ? list[list.length - 1] : null;
  });

  protected readonly steps = [
    { icon: 'ti-plane-departure', title: 'Fly online', text: 'Book a flight, plan it with SimBrief and fly it on IVAO or VATSIM with the Litoral ACARS.' },
    { icon: 'ti-file-check', title: 'Send the report', text: 'At the end of the flight the ACARS sends your PIREP with the track, landing and procedures.' },
    { icon: 'ti-star', title: 'Earn points', text: 'Every approved PIREP adds flight hours and points according to the scoring rules.' },
    { icon: 'ti-chevrons-up', title: 'Get promoted', text: 'As soon as you meet the hours and points of the next rank, the promotion is automatic.' }
  ];

  ngOnInit(): void {
    this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.failed.set(false);
    try {
      this.ranks.set(await this.reference.getCareer());
    } catch {
      this.failed.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected icon(index: number): string {
    return RANK_ICONS[Math.min(index, RANK_ICONS.length - 1)];
  }
}