import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RecentFlight, RecentFlights as RecentFlightsModel, hhmm } from '../../core/flights/flight.models';
import { FlightService } from '../../core/flights/flight.service';
import { NETWORK_LABELS, NETWORK_LOGOS, NETWORK_LOGO_LIGHT_INVERT } from '../../core/pirep/pirep.models';
import { countryFlagUrl } from '../utils/country-flag';

@Component({
  selector: 'app-recent-flights',
  imports: [RouterLink],
  template: `
    <div class="flex flex-col gap-4">
      <div class="flex items-center gap-4">
        <h2 class="whitespace-nowrap text-xl font-bold text-ink">
          Last flights in the VA@if (data(); as d) {, totaling <span class="text-primary">{{ d.monthTotal }}</span> in {{ month }}}
        </h2>
        <span class="h-px flex-1 bg-line"></span>
      </div>

      @if (loading()) {
        <p class="panel px-6 py-10 text-center text-ink-muted">Loading...</p>
      } @else if (!data()?.flights?.length) {
        <div class="panel flex flex-col items-center gap-2 px-6 py-10 text-center">
          <i class="ti ti-plane-off text-4xl text-ink-muted" aria-hidden="true"></i>
          <p class="text-ink-muted">No flights yet. Be the first!</p>
        </div>
      } @else {
        <div class="grid gap-5 sm:grid-cols-2 xl:grid-cols-4">
          @for (f of data()!.flights; track f.id) {
            <article class="card-hover flex flex-col overflow-hidden rounded-xl border border-line bg-surface">
              <a [routerLink]="['/flights', f.id]" class="block h-40 bg-cover bg-center" [style.background-image]="cover(f)" [attr.aria-label]="'Open ' + f.callsign"></a>
              <div class="flex flex-1 flex-col gap-1 p-5">
                <div class="flex items-center justify-between gap-2">
                  <a [routerLink]="['/flights', f.id]" class="font-mono text-2xl font-extrabold text-primary hover:underline">{{ f.callsign }}</a>
                  @if (flag(f.departureCountry); as url) {
                    <img [src]="url" [alt]="f.departureCountry" class="h-4 w-auto rounded-sm" />
                  }
                </div>
                <p class="flex items-center gap-2 font-mono text-xl font-bold text-ink">
                  {{ f.departureIcao }} <i class="ti ti-plane text-lg text-primary" aria-hidden="true"></i> {{ f.arrivalIcao }}
                </p>
                <p class="text-sm font-semibold text-ink">{{ f.registration }} · <span class="font-normal">{{ f.typeName }}{{ f.variant ? ' ' + f.variant : '' }}</span></p>
                <p class="text-sm text-ink-muted">Flight type: <span class="text-ink">{{ f.isCargo ? 'Cargo' : 'Passengers' }}</span></p>
                <p class="text-sm text-ink-muted">Duration: <span class="text-ink">{{ hhmm(f.blockMinutes) }}</span></p>

                <div class="mt-auto flex items-center justify-between gap-2 border-t border-line pt-3">
                  <a [routerLink]="['/profile', f.pilotNumber]" class="flex min-w-0 items-center gap-2 hover:underline">
                    <span class="flex h-8 w-8 shrink-0 items-center justify-center overflow-hidden rounded-full bg-primary text-xs font-bold text-on-primary">
                      @if (f.pilotPhotoUrl) {
                        <img [src]="f.pilotPhotoUrl" alt="" class="h-full w-full object-cover" />
                      } @else {
                        {{ initials(f.pilotName) }}
                      }
                    </span>
                    <img [src]="'images/ranks/' + f.rankCode.toLowerCase() + '.png'" alt="" class="h-4 w-auto" />
                    <span class="truncate text-sm font-semibold text-ink">{{ f.pilotName }}</span>
                  </a>
                  @if (logos[f.network]; as logo) {
                    <img [src]="logo" [alt]="networks[f.network]" [title]="networks[f.network]" class="h-7 w-7 object-contain"
                      [class.dark:invert-0]="invert[f.network]" [class.invert]="invert[f.network]" />
                  } @else {
                    <span class="rounded bg-surface-alt px-2 py-0.5 text-xs text-ink-muted">Offline</span>
                  }
                </div>
              </div>
            </article>
          }
        </div>
      }
    </div>
  `
})
export class RecentFlights implements OnInit {
  private readonly api = inject(FlightService);

  protected readonly data = signal<RecentFlightsModel | null>(null);
  protected readonly loading = signal(true);
  protected readonly month = new Date().toLocaleString('en-US', { month: 'long', timeZone: 'UTC' });
  protected readonly hhmm = hhmm;
  protected readonly flag = countryFlagUrl;
  protected readonly logos = NETWORK_LOGOS;
  protected readonly invert = NETWORK_LOGO_LIGHT_INVERT;
  protected readonly networks = NETWORK_LABELS;

  async ngOnInit(): Promise<void> {
    try {
      this.data.set(await this.api.recent(4));
    } catch {
      this.data.set(null);
    } finally {
      this.loading.set(false);
    }
  }

  protected cover(f: RecentFlight): string {
    const gradient = 'linear-gradient(135deg, #0F3768 0%, #1D6FD8 60%, #2EC4B6 100%)';
    return f.aircraftPhotoUrl ? `url('${f.aircraftPhotoUrl}'), ${gradient}` : gradient;
  }

  protected initials(name: string): string {
    return name.split(/[\s.]+/).filter(Boolean).map(p => p[0]).slice(0, 2).join('').toUpperCase();
  }
}