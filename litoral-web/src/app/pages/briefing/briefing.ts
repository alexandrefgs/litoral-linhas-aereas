import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { apiErrorMessage, apiErrorStatus } from '../../core/api/api-error';
import { Briefing as BriefingData, SimBriefOfp } from '../../core/booking/booking.models';
import { BookingService } from '../../core/booking/booking.service';
import { formatMinutes } from '../../shared/utils/flight';
import { CurrentBooking } from '../book/current-booking';

const VATSIM_PREFILE = 'https://my.vatsim.net/pilots/flightplan';
const IVAO_PREFILE = 'https://fpl.ivao.aero/';

@Component({
  selector: 'app-briefing',
  imports: [DatePipe, RouterLink, CurrentBooking],
  template: `
    <section class="mx-auto flex max-w-7xl flex-col gap-6 px-6 py-8">
      <h1 class="text-2xl font-bold text-ink">Briefing</h1>

      @if (loading()) {
        <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (!data()) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-12 text-center">
          <i class="ti ti-plane-off text-4xl text-ink-muted" aria-hidden="true"></i>
          <p class="text-ink-muted">{{ error() ?? "You don't have an open flight." }}</p>
          <a routerLink="/book" class="btn-primary">Book flight</a>
        </div>
      } @else {
        @let brief = data()!;
        <app-current-booking [booking]="brief.booking" [showBriefingLink]="false" (cancelled)="data.set(null)" />

        <div class="grid gap-6 lg:grid-cols-[2fr_1fr]">
          <div class="panel flex flex-col gap-4 p-6">
            <div class="flex flex-wrap items-center justify-between gap-3">
              <div>
                <h2 class="text-lg font-bold text-ink">Flight plan</h2>
                <p class="text-sm text-ink-muted">Generate the OFP on SimBrief, then load it here.</p>
              </div>
              <div class="flex flex-wrap gap-2">
                <a [href]="brief.simBriefDispatchUrl" target="_blank" rel="noopener" class="btn-secondary gap-2">
                  <i class="ti ti-external-link" aria-hidden="true"></i>Generate on SimBrief
                </a>
                <button type="button" class="btn-primary gap-2" [disabled]="loadingOfp() || !brief.simBriefUsername" (click)="loadOfp()">
                  <i class="ti ti-download" aria-hidden="true"></i>{{ loadingOfp() ? 'Loading...' : 'Load my OFP' }}
                </button>
              </div>
            </div>

            @if (!brief.simBriefUsername) {
              <p class="rounded-lg border border-highlight bg-highlight/15 px-4 py-3 text-sm text-ink">
                Set your SimBrief username in your <a routerLink="/profile" class="font-semibold text-primary hover:underline">profile</a> to load the OFP here.
              </p>
            }
            @if (ofpError(); as message) {
              <p class="rounded-lg bg-danger/10 px-4 py-3 text-sm text-danger">{{ message }}</p>
            }

            @if (ofp(); as plan) {
              @if (!plan.matchesBooking) {
                <p class="rounded-lg border border-highlight bg-highlight/15 px-4 py-3 text-sm text-ink">
                  <i class="ti ti-alert-triangle mr-1" aria-hidden="true"></i>
                  Your latest OFP is {{ plan.origin }} → {{ plan.destination }}, not this flight. Generate a new one.
                </p>
              }
              <div class="grid gap-4 sm:grid-cols-4">
                <div><p class="text-xs uppercase tracking-wide text-ink-muted">Cruise</p><p class="font-semibold text-ink">{{ plan.cruiseAltitude ? 'FL' + (plan.cruiseAltitude / 100) : '—' }}</p></div>
                <div><p class="text-xs uppercase tracking-wide text-ink-muted">Alternate</p><p class="font-mono font-semibold text-ink">{{ plan.alternate ?? '—' }}</p></div>
                <div><p class="text-xs uppercase tracking-wide text-ink-muted">Block fuel</p><p class="font-semibold text-ink">{{ plan.blockFuel ? plan.blockFuel + ' ' + (plan.fuelUnits ?? '') : '—' }}</p></div>
                <div><p class="text-xs uppercase tracking-wide text-ink-muted">Enroute</p><p class="font-semibold text-ink">{{ plan.enrouteMinutes ? format(plan.enrouteMinutes) : '—' }}</p></div>
              </div>
              <div>
                <p class="text-xs uppercase tracking-wide text-ink-muted">Route</p>
                <p class="break-words rounded-lg bg-surface-alt px-3 py-2 font-mono text-sm text-ink">{{ plan.route ?? '—' }}</p>
              </div>
              <div class="flex flex-wrap items-center gap-2">
                @if (plan.pdfUrl) {
                  <a [href]="plan.pdfUrl" target="_blank" rel="noopener" class="btn-secondary gap-2"><i class="ti ti-file-type-pdf" aria-hidden="true"></i>OFP (PDF)</a>
                }
                @if (plan.generatedAt) {
                  <span class="text-xs text-ink-muted">Generated {{ plan.generatedAt | date: 'd MMM, HH:mm' }}</span>
                }
              </div>
            }

            <div class="border-t border-line pt-4">
              <p class="mb-2 text-sm font-semibold text-ink">Prefile</p>
              <div class="flex flex-wrap gap-2">
                <a [href]="ofp()?.vatsimPrefileUrl ?? vatsim" target="_blank" rel="noopener" class="btn-secondary gap-2">
                  <i class="ti ti-broadcast" aria-hidden="true"></i>VATSIM
                </a>
                <a [href]="ofp()?.ivaoPrefileUrl ?? ivao" target="_blank" rel="noopener" class="btn-secondary gap-2">
                  <i class="ti ti-broadcast" aria-hidden="true"></i>IVAO
                </a>
              </div>
              <p class="mt-2 text-xs text-ink-muted">
                With the OFP loaded, the links come pre-filled. If {{ brief.booking.callsign }} is taken on the network, use {{ brief.booking.networkCallsign }}.
              </p>
            </div>
          </div>

          <div class="panel flex flex-col gap-4 p-6">
            <h2 class="text-lg font-bold text-ink">Weather</h2>
            @for (metar of brief.metars; track metar.icao) {
              <div>
                <p class="mb-1 flex items-center justify-between text-sm">
                  <span class="font-mono font-bold text-ink">{{ metar.icao }}</span>
                  @if (metar.observedAt) {
                    <span class="text-xs text-ink-muted">{{ metar.observedAt | date: 'HH:mm' : 'UTC' }}Z</span>
                  }
                </p>
                <p class="rounded-lg bg-surface-alt px-3 py-2 font-mono text-xs leading-relaxed text-ink">{{ metar.raw ?? 'METAR not available.' }}</p>
              </div>
            }
          </div>
        </div>
      }
    </section>
  `
})
export class Briefing implements OnInit {
  private readonly api = inject(BookingService);

  protected readonly format = formatMinutes;
  protected readonly vatsim = VATSIM_PREFILE;
  protected readonly ivao = IVAO_PREFILE;

  protected readonly data = signal<BriefingData | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly ofp = signal<SimBriefOfp | null>(null);
  protected readonly loadingOfp = signal(false);
  protected readonly ofpError = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    try {
      this.data.set(await this.api.briefing());
    } catch (error) {
      if (apiErrorStatus(error) !== 404) this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.loading.set(false);
    }
  }

  protected async loadOfp(): Promise<void> {
    this.loadingOfp.set(true);
    this.ofpError.set(null);

    try {
      this.ofp.set(await this.api.simBrief());
    } catch (error) {
      this.ofpError.set(apiErrorMessage(error) ?? 'Could not load your OFP.');
    } finally {
      this.loadingOfp.set(false);
    }
  }
}