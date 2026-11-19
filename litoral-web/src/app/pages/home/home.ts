import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { EMPTY_STATS, PublicStats } from '../../core/reference/public-stats';
import { ReferenceService } from '../../core/reference/reference.service';
import { AircraftIllustration } from './components/aircraft-illustration';
import { FlipCounter } from './components/flip-counter';

@Component({
  selector: 'app-home',
  imports: [RouterLink, FlipCounter, AircraftIllustration],
  template: `
    <section id="inicio" class="relative isolate -mt-px overflow-hidden bg-atlantico-900 text-white">
      <svg class="absolute inset-0 -z-10 h-full w-full" viewBox="0 0 1440 800" preserveAspectRatio="xMidYMid slice" aria-hidden="true">
        <defs>
          <radialGradient id="glow" cx="78%" cy="38%" r="45%">
            <stop offset="0" stop-color="#1D6FD8" stop-opacity="0.55" />
            <stop offset="1" stop-color="#0A1F3C" stop-opacity="0" />
          </radialGradient>
        </defs>
        <rect width="1440" height="800" fill="url(#glow)" />
        <circle cx="1120" cy="300" r="120" fill="#FFD166" opacity="0.9" />
        <circle cx="1120" cy="300" r="170" fill="none" stroke="#FFD166" stroke-opacity="0.15" stroke-width="2" />
        <path d="M640 560 Q900 160 1300 140" fill="none" stroke="#FFFFFF" stroke-opacity="0.35" stroke-width="2" stroke-dasharray="2 10" stroke-linecap="round" />
        <path d="M0 640 Q180 600 360 640 T720 640 T1080 640 T1440 640 V800 H0 Z" fill="#1D6FD8" opacity="0.35" />
        <path d="M0 690 Q180 650 360 690 T720 690 T1080 690 T1440 690 V800 H0 Z" fill="#2EC4B6" opacity="0.45" />
        <path d="M0 740 Q180 705 360 740 T720 740 T1080 740 T1440 740 V800 H0 Z" fill="#0A1F3C" />
      </svg>
      <i class="ti ti-plane pointer-events-none absolute right-[8%] top-[14%] -z-10 rotate-[-20deg] text-6xl text-white/80" aria-hidden="true"></i>

      <div class="mx-auto flex min-h-[calc(100vh-4.5rem)] max-w-7xl flex-col justify-center px-6 py-24">
        <p class="text-sm font-semibold uppercase tracking-[0.2em] text-white/80">
          Welcome to Litoral Linhas Aéreas · <span class="text-sol-300 underline underline-offset-4">LTL</span>
        </p>
        <h1 class="mt-6 max-w-3xl text-5xl font-semibold leading-[1.05] tracking-tight md:text-7xl">Realism for your simulation starts here.</h1>
        <p class="mt-8 max-w-xl text-lg text-white/80 md:text-xl">We're a virtual airline that brings the routine of a real airline to your simulator, flying across Brazil and the Americas.</p>
        <div class="mt-6 flex flex-wrap gap-2 text-sm">
          @for (sim of simulators; track sim) {
            <span class="rounded-full border border-white/20 bg-white/5 px-3 py-1 text-white/90">{{ sim }}</span>
          }
        </div>
        <div class="mt-10 flex flex-wrap gap-3">
          @if (auth.isAuthenticated()) {
            <a routerLink="/dashboard" class="rounded-lg bg-sol-300 px-6 py-3 font-semibold text-atlantico-900 hover:bg-sol-200">Go to dashboard</a>
          } @else {
            <a routerLink="/register" class="rounded-lg bg-sol-300 px-6 py-3 font-semibold text-atlantico-900 hover:bg-sol-200">Join the crew</a>
            <a routerLink="/login" class="rounded-lg border border-white/30 px-6 py-3 font-semibold text-white hover:bg-white/10">Sign in</a>
          }
        </div>
        <a routerLink="/" fragment="sobre" class="mt-16 flex w-fit items-center gap-3 text-sm text-white/80 hover:text-white">
          <span class="flex h-11 w-11 items-center justify-center rounded-full border border-white/40">
            <i class="ti ti-arrow-down text-xl" aria-hidden="true"></i>
          </span>
          Scroll down
        </a>
      </div>
    </section>

    <section id="sobre" class="scroll-mt-20 bg-page">
      <div class="mx-auto grid max-w-7xl gap-12 px-6 py-24 lg:grid-cols-2 lg:items-center">
        <div>
          <p class="eyebrow">Thanks for visiting</p>
          <h2 class="mt-3 text-4xl font-semibold tracking-tight text-ink md:text-5xl">Born on the coast, made for the sky</h2>
          <div class="relative mt-10 overflow-hidden rounded-2xl bg-gradient-to-br from-atlantico-400 via-atlantico-500 to-onda-400 p-8 shadow-xl">
            <svg class="absolute inset-x-0 bottom-0 w-full" viewBox="0 0 400 60" preserveAspectRatio="none" aria-hidden="true">
              <path d="M0 30 Q50 10 100 30 T200 30 T300 30 T400 30 V60 H0 Z" fill="#FFFFFF" opacity="0.18" />
              <path d="M0 42 Q50 24 100 42 T200 42 T300 42 T400 42 V60 H0 Z" fill="#FFFFFF" opacity="0.25" />
            </svg>
            <span class="absolute right-6 top-6 h-14 w-14 rounded-full bg-sol-300"></span>
            <app-aircraft-illustration class="relative mt-10 drop-shadow-2xl" />
            <p class="relative mt-6 text-xs text-white/80">Coming soon: real shots of the Litoral fleet in the simulators.</p>
          </div>
        </div>
        <div class="space-y-5 text-ink-muted">
          <p class="text-lg font-semibold text-ink">Litoral is a Brazilian VA dedicated to serious simulation, with a 100% Boeing 737 fleet.</p>
          <p>Our pilots fly on the IVAO and VATSIM networks, using MSFS 2020, MSFS 2024, X-Plane and Prepar3D, with hubs in Guarulhos, Florianópolis, Brasília, Manaus and Fortaleza, plus a cargo operation out of Viracopos.</p>
          <p class="text-lg font-semibold text-ink">Our mission is to deliver a realistic, professional and fun airline experience.</p>
          <p>Here you build your career from First Officer to Captain, plan every flight with SimBrief and track your progress through hours and points. All in a welcoming environment where we learn together and share our passion for aviation.</p>
          <a routerLink="/" fragment="operacao" class="inline-flex items-center gap-2 border-b-2 border-primary pb-1 font-medium text-ink hover:text-primary">
            See our operation <i class="ti ti-arrow-right" aria-hidden="true"></i>
          </a>
        </div>
      </div>
    </section>

    <section id="operacao" class="scroll-mt-20 border-y border-line bg-surface">
      <div class="mx-auto grid max-w-7xl gap-10 px-6 py-24 lg:grid-cols-[1fr_3fr]">
        <div>
          <p class="eyebrow">More than a virtual flight</p>
          <p class="mt-4 flex items-center gap-2 text-xs font-bold uppercase tracking-widest text-primary">
            <span class="h-1.5 w-1.5 rounded-full bg-primary"></span>The Litoral experience
          </p>
          <h2 class="mt-3 text-3xl font-semibold leading-tight text-ink md:text-4xl">Real operations. Your next challenge.</h2>
          <p class="mt-5 text-ink-muted">Every step of the flight follows airline logic: from choosing the route and aircraft to landing and reviewing your performance.</p>
          <span class="mt-6 block h-0.5 w-16 bg-primary"></span>
        </div>
        <div class="grid gap-5 md:grid-cols-3">
          @for (card of experienceCards; track card.key; let i = $index) {
            <article class="flex flex-col rounded-2xl border border-line bg-page p-6">
              <div class="flex items-center justify-between">
                <span class="flex h-11 w-11 items-center justify-center rounded-xl bg-primary/10 text-2xl text-primary">
                  <i class="ti {{ card.icon }}" aria-hidden="true"></i>
                </span>
                <span class="text-xs font-semibold uppercase tracking-widest text-ink-muted">0{{ i + 1 }} / {{ card.step }}</span>
              </div>
              <h3 class="mt-6 text-lg font-semibold text-ink">{{ card.title }}</h3>
              <p class="mt-3 flex-1 text-sm leading-relaxed text-ink-muted">{{ card.text }}</p>
              <p class="mt-6 border-t border-line pt-4 text-xs font-bold uppercase tracking-wider text-primary">{{ card.tags }}</p>
            </article>
          }
        </div>
      </div>
    </section>

    <section id="numeros" class="relative isolate scroll-mt-20 overflow-hidden bg-atlantico-900 text-white">
      <svg class="absolute inset-0 -z-10 h-full w-full opacity-40" viewBox="0 0 1440 600" preserveAspectRatio="xMidYMid slice" aria-hidden="true">
        <path d="M-50 520 Q400 80 1500 120" fill="none" stroke="#2EC4B6" stroke-width="2" stroke-dasharray="4 14" />
        <path d="M-50 600 Q600 260 1500 300" fill="none" stroke="#1D6FD8" stroke-width="2" stroke-dasharray="4 14" />
      </svg>
      <div class="mx-auto grid max-w-7xl gap-14 px-6 py-24 lg:grid-cols-[1fr_2fr]">
        <div>
          <p class="text-sm font-semibold uppercase tracking-[0.2em] text-white/70">Our numbers</p>
          <h2 class="mt-3 text-4xl font-semibold md:text-5xl">Total flights</h2>
          <div class="mt-5"><app-flip-counter [value]="stats().completedFlights" [minDigits]="6" /></div>
          <p class="mt-3 text-sm text-white/70">Valid flights since Litoral was founded.</p>
          <span class="my-8 block h-px w-16 bg-white/40"></span>
          <ul class="space-y-3">
            @for (item of highlights; track item) {
              <li class="flex items-center gap-3">
                <i class="ti ti-circle-check text-xl text-onda-300" aria-hidden="true"></i>{{ item }}
              </li>
            }
          </ul>
        </div>
        <div class="grid gap-10 sm:grid-cols-2">
          @for (item of numberCards(); track item.label) {
            <div>
              <app-flip-counter [value]="item.value" />
              <h3 class="mt-4 text-xl font-semibold">{{ item.label }}</h3>
              <span class="my-3 block h-px w-16 bg-white/40"></span>
              <p class="text-sm text-white/70">{{ item.text }}</p>
            </div>
          }
        </div>
      </div>
    </section>

    <section id="voos" class="scroll-mt-20 bg-page">
      <div class="mx-auto max-w-7xl px-6 py-24">
        <div class="flex flex-wrap items-end justify-between gap-4">
          <div>
            <p class="eyebrow">Updated every flight</p>
            <h2 class="mt-3 text-4xl font-semibold tracking-tight text-ink md:text-5xl">Last flights</h2>
          </div>
          <a routerLink="/" fragment="operacao" class="inline-flex items-center gap-2 border-b-2 border-primary pb-1 font-medium text-ink hover:text-primary">
            Our operation <i class="ti ti-arrow-right" aria-hidden="true"></i>
          </a>
        </div>
        <div class="mt-12 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
          @for (slot of [1, 2, 3, 4]; track slot) {
            <article class="overflow-hidden rounded-2xl border border-dashed border-line bg-surface">
              <div class="relative flex h-36 items-center bg-gradient-to-br from-atlantico-300/40 to-onda-200/40 px-4">
                <app-aircraft-illustration class="w-full opacity-60" />
              </div>
              <div class="space-y-2 p-5">
                <p class="font-semibold text-ink">LTL{{ 1000 + slot * 111 }} · ???? <i class="ti ti-arrow-right text-primary" aria-hidden="true"></i> ????</p>
                <p class="text-sm text-ink-muted">Waiting for the next flight</p>
              </div>
            </article>
          }
        </div>
        <p class="mt-8 text-center text-ink-muted">Litoral's first flights will show up here. The next one could be yours.</p>
      </div>
    </section>

    <section class="bg-primary">
      <div class="mx-auto flex max-w-7xl flex-col items-center justify-between gap-6 px-6 py-16 text-center md:flex-row md:text-left">
        <div>
          <h2 class="text-3xl font-semibold text-on-primary">Ready for takeoff?</h2>
          <p class="mt-2 text-on-primary/80">Create your account, pick your hub and fly your first Litoral flight.</p>
        </div>
        <a [routerLink]="auth.isAuthenticated() ? '/dashboard' : '/register'" class="rounded-lg bg-sol-300 px-6 py-3 font-semibold text-atlantico-900 hover:bg-sol-200">
          {{ auth.isAuthenticated() ? 'Go to dashboard' : 'Join the crew' }}
        </a>
      </div>
    </section>

    <footer class="border-t border-line bg-surface">
      <div class="mx-auto flex max-w-7xl flex-col gap-6 px-6 py-10 md:flex-row md:items-center md:justify-between">
        <div class="flex items-center gap-3">
          <img src="favicon.svg" alt="" class="h-9 w-9" />
          <div class="leading-tight">
            <div class="flex flex-col gap-2 md:items-end">
              <div class="flex gap-4 text-sm font-medium">
                <a routerLink="/regulations" class="text-ink hover:text-primary">Regulations</a>
                <a routerLink="/privacy" class="text-ink hover:text-primary">Privacy Policy</a>
              </div>
              <p class="max-w-xl text-xs text-ink-muted md:text-right">Litoral Linhas Aéreas is a virtual airline for flight simulation, not affiliated with any real airline.</p>
            </div>
          </div>
        </div>
        <p class="max-w-xl text-xs text-ink-muted md:text-right">Litoral Linhas Aéreas is a virtual airline for flight simulation, not affiliated with any real airline.</p>
      </div>
    </footer>
  `
})
export class Home implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly reference = inject(ReferenceService);

  protected readonly stats = signal<PublicStats>(EMPTY_STATS);
  protected readonly year = new Date().getFullYear();

  protected readonly simulators = ['MSFS 2020', 'MSFS 2024', 'X-Plane 11 / 12', 'Prepar3D'];

  protected readonly experienceCards = [
    { key: 'operate', step: 'Operate', title: '100% Boeing 737 fleet', text: 'From the 737-600 to the MAX 8, plus the 737-800BCF freighter. Every route respects the real range of the chosen aircraft.', icon: 'ti-plane', tags: 'B736 · B737 · B738 · B739 · B38M' },
    { key: 'brief', step: 'Brief', title: 'Planning with SimBrief', text: 'Book the flight and open SimBrief already filled in. Route, fuel and weights show up in your briefing before takeoff.', icon: 'ti-file-text', tags: 'SimBrief · OFP' },
    { key: 'fly', step: 'Fly', title: 'ACARS and scoring', text: 'Your flight is tracked from push back to the gate. Landing, procedures and deviations set the points that take you to the left seat.', icon: 'ti-radar-2', tags: 'ACARS · PIREP' }
  ];

  protected readonly highlights = [
    'Flights from Brazil to the Americas',
    '100% Boeing 737 fleet',
    'Works with MSFS, X-Plane and P3D',
    'Fly on IVAO and VATSIM',
    'Career from First Officer to Captain'
  ];

  protected numberCards() {
    const s = this.stats();
    return [
      { value: s.activePilots, label: 'Pilots', text: 'Active pilots flying with the LITORAL callsign.' },
      { value: s.hubs, label: 'Hubs', text: 'Passenger and cargo bases across Brazil.' },
      { value: s.aircraftTypes, label: 'Fleet models', text: 'Boeing 737 variants, passenger and freighter.' },
      { value: s.destinations, label: 'Destinations', text: 'And growing with every new route.' }
    ];
  }

  async ngOnInit(): Promise<void> {
    try {
      this.stats.set(await this.reference.getPublicStats());
    } catch {}
  }
}