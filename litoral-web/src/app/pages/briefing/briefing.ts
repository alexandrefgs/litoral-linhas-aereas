import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { apiErrorMessage, apiErrorStatus } from '../../core/api/api-error';
import { Briefing as BriefingData, Metar, SimBriefOfp } from '../../core/booking/booking.models';
import { BookingService } from '../../core/booking/booking.service';
import { BOOKING_TYPE_LABEL } from '../../core/flights/flight.models';
import { NETWORK_LOGOS } from '../../core/pirep/pirep.models';
import { CrewCard } from '../../shared/crew-card/crew-card';
import { countryFlagUrl } from '../../shared/utils/country-flag';
import { formatMinutes } from '../../shared/utils/flight';
import { BriefingAirport } from './briefing-airport';
import { FlightActions } from './flight-actions';
import { Loadsheet } from './loadsheet';
import { OfpViewer } from './ofp-viewer';

type Section = 'briefing' | 'loadsheet' | 'ofp';

const VATSIM_PREFILE = 'https://my.vatsim.net/pilots/flightplan';
const IVAO_PREFILE = 'https://fpl.ivao.aero/';
const WEATHER_REFRESH_MS = 15 * 60 * 1000;
const WEATHER_RETRY_MS = 30 * 1000;

@Component({
  selector: 'app-briefing',
  imports: [DatePipe, NgTemplateOutlet, RouterLink, CrewCard, BriefingAirport, FlightActions, Loadsheet, OfpViewer],
  template: `
    <section class="mx-auto flex max-w-7xl flex-col gap-6 px-6 py-8">
      @if (loading()) {
        <p class="panel px-6 py-16 text-center text-ink-muted">Loading...</p>
      } @else if (!data()) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-16 text-center">
          <i class="ti ti-plane-off text-4xl text-ink-muted" aria-hidden="true"></i>
          <p class="text-ink-muted">{{ error() ?? "You don't have an open flight." }}</p>
          <a routerLink="/book" class="btn-primary">Book flight</a>
        </div>
      } @else {
        @let brief = data()!;
        @let b = brief.booking;
        <div class="grid gap-6 lg:grid-cols-[380px_minmax(0,1fr)]">
          <aside class="flex min-w-0 flex-col gap-6">
            <div class="panel card-hover flex flex-col gap-4 p-5">
              <div class="flex items-start justify-between gap-3">
                <div>
                  <p class="font-mono text-4xl font-extrabold text-primary">{{ b.callsign }}</p>
                  <p class="text-xs text-ink-muted">{{ bookingLabel[b.type] }}{{ b.isCargo ? ' · Cargo' : '' }} · {{ b.distanceNm }} NM · {{ format(b.estimatedMinutes) }}</p>
                </div>
                <div class="flex gap-1">
                  <button type="button" class="icon-btn h-9 w-9 text-xl text-primary" title="Refresh weather and OFP" [disabled]="refreshing()" (click)="refresh()">
                    <i class="ti ti-refresh" [class.animate-spin]="refreshing()" aria-hidden="true"></i>
                  </button>
                  <button type="button" class="icon-btn h-9 w-9 text-xl text-danger" title="Cancel flight" (click)="confirmCancel.set(true)">
                    <i class="ti ti-trash" aria-hidden="true"></i>
                  </button>
                </div>
              </div>

              <div class="flex items-center justify-between gap-3 font-mono text-3xl font-extrabold text-ink">
                <span [title]="b.departure.name">{{ b.departure.icao }}</span>
                <span class="h-px flex-1 border-t border-dashed border-line"></span>
                <i class="ti ti-plane text-2xl text-primary" aria-hidden="true"></i>
                <span class="h-px flex-1 border-t border-dashed border-line"></span>
                <span [title]="b.arrival.name">{{ b.arrival.icao }}</span>
              </div>

              @if (confirmCancel()) {
                <div class="flex flex-col gap-2 rounded-lg border border-danger/40 bg-danger/10 p-3 text-sm">
                  <p class="text-ink">{{ cancelWarning() }}</p>
                  @if (cancelError(); as message) {
                    <p class="text-danger">{{ message }}</p>
                  }
                  <div class="flex gap-2">
                    <button type="button" class="btn-secondary flex-1" (click)="confirmCancel.set(false)">Keep flight</button>
                    <button type="button" class="flex-1 rounded-md bg-danger px-4 py-2 font-medium text-white hover:opacity-90" [disabled]="cancelling()" (click)="cancel()">
                      {{ cancelling() ? 'Cancelling...' : 'Yes, cancel' }}
                    </button>
                  </div>
                </div>
              }

              <div>
                <p class="mb-2 text-center text-[11px] font-semibold uppercase tracking-wide text-ink-muted">Flight plan</p>
                <div class="grid grid-cols-2 gap-2">
                  <a [href]="ofp()?.vatsimPrefileUrl ?? vatsim" target="_blank" rel="noopener"
                    class="flex items-center justify-center gap-2 rounded-md bg-[#1b2a6b] px-3 py-2 text-sm font-semibold text-white hover:opacity-90">
                    <img [src]="logos.Vatsim" alt="" class="h-5 w-auto object-contain" />VATSIM
                  </a>
                  <a [href]="ofp()?.ivaoPrefileUrl ?? ivao" target="_blank" rel="noopener"
                    class="flex items-center justify-center gap-2 rounded-md bg-surface-alt px-3 py-2 text-sm font-semibold text-ink hover:bg-line">
                    <img [src]="logos.Ivao" alt="" class="h-5 w-auto object-contain invert dark:invert-0" />IVAO
                  </a>
                </div>
                <p class="mt-2 text-center text-[11px] text-ink-muted">
                  Callsign taken on the network? Use <span class="font-mono font-semibold text-ink">{{ b.networkCallsign }}</span>
                </p>
              </div>

              <p class="border-t border-line pt-3 text-center text-[11px] uppercase tracking-wide text-ink-muted">
                Expires at {{ b.expiresAt | date: 'd MMM, HH:mm' : 'UTC' }}Z
              </p>
            </div>

            <div class="grid grid-cols-2 gap-3">
              <app-crew-card [member]="brief.captain" role="Captain" [compact]="true" />
              <app-crew-card [member]="brief.firstOfficer" role="First Officer" [compact]="true" />
            </div>

            <nav class="panel flex flex-col overflow-hidden">
              @for (t of sections; track t.value) {
                <button type="button" (click)="scrollTo(t.value)" [disabled]="t.value !== 'briefing' && !matchingOfp()"
                  class="flex items-center justify-between gap-3 border-b border-line px-5 py-3 text-left text-sm font-semibold uppercase tracking-wide last:border-0 disabled:cursor-not-allowed disabled:opacity-40"
                  [class]="active() === t.value ? 'bg-primary text-on-primary' : 'text-ink hover:bg-surface-alt'">
                  <span class="flex items-center gap-3"><i class="ti {{ t.icon }} text-lg" aria-hidden="true"></i>{{ t.label }}</span>
                  <i class="ti ti-chevron-right" aria-hidden="true"></i>
                </button>
              }
            </nav>

            <div class="panel card-hover flex flex-col overflow-hidden">
              <div class="relative h-44 bg-cover bg-center" [style.background-image]="aircraftCover()">
                @if (!b.aircraft.photoUrl) {
                  <i class="ti ti-plane absolute inset-0 m-auto h-fit w-fit text-5xl text-white/40" aria-hidden="true"></i>
                }
                @if (b.aircraft.photoCredit) {
                  <span class="absolute bottom-1 right-2 max-w-[70%] truncate text-[10px] text-white/80 [text-shadow:0_1px_2px_rgba(0,0,0,.8)]">© {{ b.aircraft.photoCredit }}</span>
                }
              </div>
              <div class="flex flex-col gap-3 p-5">
                <div class="flex items-start justify-between gap-2">
                  <div>
                    <p class="font-mono text-3xl font-extrabold text-ink">{{ b.aircraft.registration }}</p>
                    <p class="text-sm text-ink-muted">{{ b.aircraft.typeName }}</p>
                  </div>
                  @if (flag('Brazil'); as url) {
                    <img [src]="url" alt="Brazil" class="h-4 w-auto rounded-sm" />
                  }
                </div>
                <div class="grid grid-cols-2 gap-3 border-t border-line pt-3 text-sm">
                  <div class="flex items-center gap-2.5">
                    <span class="stat-icon h-9 w-9 text-lg"><i class="ti ti-settings" aria-hidden="true"></i></span>
                    <div><p class="text-[11px] text-ink-muted">Model</p><p class="font-semibold text-ink">{{ b.aircraft.typeIcao }}{{ b.aircraft.variant ? ' · ' + b.aircraft.variant : '' }}</p></div>
                  </div>
                  <div class="flex items-center gap-2.5">
                    <span class="stat-icon h-9 w-9 text-lg"><i class="ti ti-history" aria-hidden="true"></i></span>
                    <div><p class="text-[11px] text-ink-muted">Last flight</p><p class="font-semibold text-ink">{{ b.aircraft.lastFlightAt ? (b.aircraft.lastFlightAt | date: 'd MMM y') : '—' }}</p></div>
                  </div>
                </div>
              </div>
            </div>
          </aside>

          <div class="flex min-w-0 flex-col gap-6">
            <div id="briefing-section" class="flex scroll-mt-24 flex-col gap-6">
              <div class="grid gap-6 xl:grid-cols-2">
                <app-briefing-airport [airport]="b.departure" [metar]="metarFor(b.departure.icao)" kind="departure"
                  [isBase]="brief.baseIcaos.includes(b.departure.icao)" [runway]="matchingOfp()?.departureRunway ?? null" />
                <app-briefing-airport [airport]="b.arrival" [metar]="metarFor(b.arrival.icao)" kind="arrival"
                  [isBase]="brief.baseIcaos.includes(b.arrival.icao)" [runway]="matchingOfp()?.arrivalRunway ?? null"
                  [alternate]="matchingOfp()?.alternate ?? null" />
              </div>

              <div class="panel card-hover flex flex-col gap-4 p-6">
                <div class="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <h2 class="text-lg font-bold text-ink">SimBrief</h2>
                    <p class="text-sm text-ink-muted">{{ brief.simBriefApiEnabled ? 'Generate the OFP without leaving the briefing.' : 'Generate the OFP on SimBrief, then load it here.' }}</p>
                  </div>
                  <ng-container [ngTemplateOutlet]="ofpButtons" />
                </div>

                @if (!brief.simBriefUsername) {
                  <p class="rounded-lg border border-highlight bg-highlight/15 px-4 py-3 text-sm text-ink">
                    Set your SimBrief username in your <a routerLink="/profile" [queryParams]="{ tab: 'settings' }" class="font-semibold text-primary hover:underline">settings</a> to load the OFP here.
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
                  } @else {
                    <div class="grid gap-4 sm:grid-cols-3 lg:grid-cols-6">
                      @for (item of ofpSummary(); track item.label) {
                        <div><p class="text-[11px] uppercase tracking-wide text-ink-muted">{{ item.label }}</p><p class="font-mono font-semibold text-ink">{{ item.value }}</p></div>
                      }
                    </div>
                    <div>
                      <p class="text-[11px] uppercase tracking-wide text-ink-muted">Route</p>
                      <p class="break-words rounded-lg bg-surface-alt px-3 py-2 font-mono text-sm text-ink">{{ plan.route ?? '—' }}</p>
                    </div>
                    <div class="flex flex-wrap items-center gap-2">
                      <button type="button" class="btn-secondary gap-2" (click)="scrollTo('ofp')"><i class="ti ti-file-text" aria-hidden="true"></i>View OFP</button>
                      <button type="button" class="btn-secondary gap-2" (click)="scrollTo('loadsheet')"><i class="ti ti-scale" aria-hidden="true"></i>Loadsheet</button>
                      @if (plan.generatedAt) {
                        <span class="text-xs text-ink-muted">Generated {{ plan.generatedAt | date: 'd MMM, HH:mm' : 'UTC' }}Z</span>
                      }
                    </div>
                  }
                }
              </div>

              <app-flight-actions [booking]="b" />
            </div>

            @if (matchingOfp(); as plan) {
              <app-loadsheet id="loadsheet-section" class="scroll-mt-24" [ofp]="plan" />
              <app-ofp-viewer id="ofp-section" class="scroll-mt-24" [planHtml]="plan.planHtml" [hasPdf]="!!plan.pdfUrl" [callsign]="b.callsign" [subtitle]="ofpSubtitle()" />
            }
          </div>
        </div>

        <ng-template #ofpButtons>
          <div class="flex flex-wrap justify-center gap-2">
            @if (brief.simBriefApiEnabled) {
              <button type="button" class="btn-primary gap-2" [disabled]="generating() || !brief.simBriefUsername" (click)="generateOfp()">
                <i class="ti ti-file-plus" [class.animate-pulse]="generating()" aria-hidden="true"></i>{{ generating() ? 'Generating on SimBrief...' : 'Generate OFP' }}
              </button>
              <button type="button" class="btn-secondary gap-2" [disabled]="loadingOfp() || generating() || !brief.simBriefUsername" (click)="loadOfp(true)">
                <i class="ti ti-download" aria-hidden="true"></i>{{ loadingOfp() ? 'Loading...' : 'Load my OFP' }}
              </button>
            } @else {
              <a [href]="brief.simBriefDispatchUrl" target="_blank" rel="noopener" class="btn-secondary gap-2">
                <i class="ti ti-external-link" aria-hidden="true"></i>Generate on SimBrief
              </a>
              <button type="button" class="btn-primary gap-2" [disabled]="loadingOfp() || !brief.simBriefUsername" (click)="loadOfp(true)">
                <i class="ti ti-download" aria-hidden="true"></i>{{ loadingOfp() ? 'Loading...' : 'Load my OFP' }}
              </button>
            }
          </div>
        </ng-template>
      }
    </section>
  `
})
export class Briefing implements OnInit {
  private readonly api = inject(BookingService);
  private weatherTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.weatherTimer));
  }

  protected readonly format = formatMinutes;
  protected readonly flag = countryFlagUrl;
  protected readonly bookingLabel = BOOKING_TYPE_LABEL;
  protected readonly vatsim = VATSIM_PREFILE;
  protected readonly ivao = IVAO_PREFILE;
  protected readonly logos = NETWORK_LOGOS;

  protected readonly sections: { value: Section; label: string; icon: string }[] = [
    { value: 'briefing', label: 'Briefing', icon: 'ti-clipboard-text' },
    { value: 'loadsheet', label: 'Loadsheet', icon: 'ti-scale' },
    { value: 'ofp', label: 'OFP', icon: 'ti-file-text' }
  ];

  protected readonly data = signal<BriefingData | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly active = signal<Section>('briefing');
  protected readonly ofp = signal<SimBriefOfp | null>(null);
  protected readonly loadingOfp = signal(false);
  protected readonly ofpError = signal<string | null>(null);
  protected readonly refreshing = signal(false);
  protected readonly generating = signal(false);
  protected readonly confirmCancel = signal(false);
  protected readonly cancelling = signal(false);
  protected readonly cancelError = signal<string | null>(null);

  protected readonly matchingOfp = computed(() => {
    const plan = this.ofp();
    return plan?.matchesBooking ? plan : null;
  });

  protected readonly aircraftCover = computed(() => {
    const gradient = 'linear-gradient(135deg, #0F3768 0%, #1D6FD8 60%, #2EC4B6 100%)';
    const url = this.data()?.booking.aircraft.photoUrl;
    return url ? `url('${url}'), ${gradient}` : gradient;
  });

  protected readonly cancelWarning = computed(() => {
    const flight = this.data()?.booking;
    if (!flight) return '';
    if (flight.type === 'Special') return 'Cancelling a special flight costs 500 points.';
    if (flight.pointsCost > 0) return `Cancel this flight? The ${flight.pointsCost} points are not refunded.`;
    return 'Cancel this flight?';
  });

  protected readonly ofpSummary = computed(() => {
    const plan = this.matchingOfp();
    if (!plan) return [];
    return [
      { label: 'Cruise', value: plan.cruiseAltitude ? `FL${Math.round(plan.cruiseAltitude / 100)}` : '—' },
      { label: 'Cost index', value: plan.costIndex ?? '—' },
      { label: 'Alternate', value: plan.alternate ?? '—' },
      { label: 'Block fuel', value: plan.blockFuel ? `${plan.blockFuel.toLocaleString('en-US')} ${plan.fuelUnits ?? ''}` : '—' },
      { label: 'Enroute', value: plan.enrouteMinutes ? formatMinutes(plan.enrouteMinutes) : '—' },
      { label: 'AIRAC', value: plan.airac ?? '—' }
    ];
  });

  protected readonly ofpSubtitle = computed(() => {
    const plan = this.matchingOfp();
    if (!plan) return '';
    const parts = [plan.airac ? `AIRAC ${plan.airac}` : null, plan.generatedAt ? `generated ${new Date(plan.generatedAt).toUTCString().slice(5, 22)}Z` : null];
    return parts.filter(Boolean).join(' · ');
  });

  async ngOnInit(): Promise<void> {
    await this.loadBriefing();
    this.loading.set(false);
    this.scheduleWeather();
    if (this.data()?.simBriefUsername) this.loadOfp(false);
  }

  protected scrollTo(section: Section): void {
    this.active.set(section);
    document.getElementById(`${section}-section`)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  protected metarFor(icao: string): Metar | null {
    return this.data()?.metars.find(m => m.icao === icao) ?? null;
  }

  protected async refresh(): Promise<void> {
    this.refreshing.set(true);
    await this.loadBriefing();
    this.scheduleWeather();
    if (this.data()?.simBriefUsername) await this.loadOfp(false);
    this.refreshing.set(false);
  }

  protected async loadOfp(showErrors: boolean): Promise<void> {
    this.loadingOfp.set(true);
    this.ofpError.set(null);

    try {
      this.ofp.set(await this.api.simBrief());
    } catch (error) {
      if (showErrors) this.ofpError.set(apiErrorMessage(error) ?? 'Could not load your OFP.');
    } finally {
      this.loadingOfp.set(false);
    }
  }

  protected async generateOfp(): Promise<void> {
    const width = 900;
    const height = 700;
    const left = window.screenX + (window.outerWidth - width) / 2;
    const top = window.screenY + (window.outerHeight - height) / 2;
    const popup = window.open('', 'simbrief', `width=${width},height=${height},left=${left},top=${top}`);

    if (!popup) {
      this.ofpError.set('Allow pop-ups for this site to generate the OFP.');
      return;
    }

    this.generating.set(true);
    this.ofpError.set(null);

    try {
      const { url } = await this.api.simBriefGenerate();
      popup.location.href = url;
    } catch (error) {
      popup.close();
      this.generating.set(false);
      this.ofpError.set(apiErrorMessage(error) ?? 'Could not start SimBrief.');
      return;
    }

    let finished = false;
    const finish = () => {
      if (finished) return;
      finished = true;
      clearInterval(timer);
      window.removeEventListener('message', onMessage);
      if (!popup.closed) popup.close();
      this.generating.set(false);
      this.loadOfp(true);
    };
    const onMessage = (event: MessageEvent) => {
      if (event.origin === window.location.origin && event.data === 'simbrief:done') finish();
    };
    window.addEventListener('message', onMessage);
    const timer = setInterval(() => {
      if (popup.closed) finish();
    }, 1000);
  }

  protected async cancel(): Promise<void> {
    this.cancelling.set(true);
    try {
      await this.api.cancel();
      this.data.set(null);
      this.confirmCancel.set(false);
    } catch (error) {
      this.cancelError.set(apiErrorMessage(error) ?? 'Could not cancel the flight.');
    } finally {
      this.cancelling.set(false);
    }
  }

  private scheduleWeather(): void {
    clearTimeout(this.weatherTimer);
    const brief = this.data();
    if (!brief) return;
    const missing = brief.metars.length < 2 || brief.metars.some(m => !m.raw);
    this.weatherTimer = setTimeout(() => this.refreshWeather(), missing ? WEATHER_RETRY_MS : WEATHER_REFRESH_MS);
  }

  private async refreshWeather(): Promise<void> {
    try {
      const metars = await this.api.weather();
      this.data.update(current => (current ? { ...current, metars } : current));
    } catch {
    }
    this.scheduleWeather();
  }

  private async loadBriefing(): Promise<void> {
    try {
      this.data.set(await this.api.briefing());
    } catch (error) {
      if (apiErrorStatus(error) !== 404) this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    }
  }
}