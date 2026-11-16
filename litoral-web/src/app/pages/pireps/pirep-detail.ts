import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { apiErrorMessage } from '../../core/api/api-error';
import { NETWORK_LABELS, PIREP_STATUS_CLASS, Pirep, PirepScoreItem } from '../../core/pirep/pirep.models';
import { PirepService } from '../../core/pirep/pirep.service';
import { RULE_TONE_CLASS, RULE_TONE_ICON, ruleTone } from '../../core/scoring/scoring.models';
import { TrackMap } from '../../shared/track-map/track-map';
import { formatMinutes } from '../../shared/utils/flight';

@Component({
  selector: 'app-pirep-detail',
  imports: [DatePipe, DecimalPipe, RouterLink, TrackMap],
  template: `
    <section class="mx-auto flex max-w-7xl flex-col gap-6 px-6 py-8">
      <a routerLink="/pireps" class="flex w-fit items-center gap-1 text-sm font-medium text-primary hover:underline">
        <i class="ti ti-arrow-left" aria-hidden="true"></i>All PIREPs
      </a>

      @if (loading()) {
        <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (error()) {
        <p class="panel px-6 py-12 text-center text-danger">{{ error() }}</p>
      } @else if (pirep(); as p) {
        <div class="panel flex flex-wrap items-center justify-between gap-4 p-6">
          <div>
            <p class="font-mono text-2xl font-bold text-ink">{{ p.summary.callsign }}</p>
            <p class="font-mono text-ink">
              {{ p.summary.departureIcao }} → {{ p.summary.arrivalIcao }}
              @if (p.summary.arrivalIcao !== p.summary.plannedArrivalIcao) {
                <span class="ml-1 font-sans text-sm text-danger">diverted from {{ p.summary.plannedArrivalIcao }}</span>
              }
            </p>
            <p class="text-sm text-ink-muted">
              {{ p.summary.registration }} · {{ p.summary.aircraftTypeIcao }} · {{ networks[p.summary.network] }} ·
              {{ p.summary.source === 'Manual' ? 'Manual PIREP' : 'ACARS' }}
            </p>
          </div>
          <div class="flex flex-wrap items-center gap-6">
            <div class="text-center">
              <p class="text-xs uppercase tracking-wide text-ink-muted">Points</p>
              <p class="text-2xl font-bold" [class]="p.summary.hasRejection ? 'text-danger' : 'text-ink'">
                {{ p.summary.hasRejection ? 'Rejection' : p.summary.scorePoints }}
              </p>
            </div>
            <div class="text-center">
              <p class="text-xs uppercase tracking-wide text-ink-muted">Conformity</p>
              <p class="text-2xl font-bold" [class]="conformityClass()">{{ p.summary.conformity }}%</p>
            </div>
            <span class="rounded-full px-3 py-1.5 text-sm font-semibold" [class]="statusClass[p.summary.status]">{{ p.summary.status }}</span>
          </div>
        </div>

        @if (p.summary.status === 'Pending') {
          <p class="rounded-lg border border-highlight bg-highlight/15 px-4 py-3 text-sm text-ink">
            <i class="ti ti-hourglass mr-1" aria-hidden="true"></i>
            Waiting for the Safety review. Your hours already count; the points are credited when the PIREP is approved.
          </p>
        }
        @if (p.reviewNote) {
          <p class="rounded-lg bg-surface-alt px-4 py-3 text-sm text-ink"><span class="font-semibold">Review:</span> {{ p.reviewNote }}</p>
        }

        <div class="grid gap-6 lg:grid-cols-[2fr_1fr]">
          <div class="panel overflow-hidden">
            @if (p.track.length > 0) {
              <app-track-map [track]="p.track" class="h-[380px]" />
            } @else {
              <div class="flex h-[380px] flex-col items-center justify-center gap-2 text-ink-muted">
                <i class="ti ti-map-off text-4xl" aria-hidden="true"></i>
                <p>No track recorded for this flight.</p>
              </div>
            }
          </div>

          <div class="panel grid grid-cols-2 gap-4 p-6 text-sm">
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Block off</p><p class="font-semibold text-ink">{{ p.blockOffAt | date: 'd MMM, HH:mm' : 'UTC' }}Z</p></div>
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Block on</p><p class="font-semibold text-ink">{{ p.blockOnAt | date: 'd MMM, HH:mm' : 'UTC' }}Z</p></div>
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Block time</p><p class="font-semibold text-ink">{{ format(p.summary.blockMinutes) }}</p></div>
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Air time</p><p class="font-semibold text-ink">{{ p.airMinutes !== null ? format(p.airMinutes) : '—' }}</p></div>
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Landing rate</p><p class="font-semibold text-ink">{{ p.summary.landingRateFpm !== null ? p.summary.landingRateFpm + ' fpm' : '—' }}</p></div>
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Fuel used</p><p class="font-semibold text-ink">{{ p.fuelUsedKg !== null ? (p.fuelUsedKg | number: '1.0-0') + ' kg' : '—' }}</p></div>
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Distance</p><p class="font-semibold text-ink">{{ p.distanceNm !== null ? p.distanceNm + ' NM' : '—' }}</p></div>
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Cruise</p><p class="font-semibold text-ink">{{ p.cruiseAltitudeFt ? 'FL' + (p.cruiseAltitudeFt / 100 | number: '1.0-0') : '—' }}</p></div>
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Online</p><p class="font-semibold text-ink">{{ p.onlinePercent !== null ? (p.onlinePercent | number: '1.0-0') + '%' : '—' }}</p></div>
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Filed</p><p class="font-semibold text-ink">{{ p.summary.submittedAt | date: 'd MMM, HH:mm' }}</p></div>
            @if (p.flightRoute) {
              <div class="col-span-2">
                <p class="text-xs uppercase tracking-wide text-ink-muted">Route</p>
                <p class="break-words rounded-lg bg-surface-alt px-3 py-2 font-mono text-xs text-ink">{{ p.flightRoute }}</p>
              </div>
            }
          </div>
        </div>

        <div class="grid gap-6 lg:grid-cols-2">
          <div class="panel flex flex-col gap-3 p-6">
            <h2 class="text-lg font-bold text-ink">Score</h2>
            @if (p.items.length === 0) {
              <p class="text-sm text-ink-muted">No scoring items.</p>
            } @else {
              <table class="w-full border border-line text-left text-sm">
                <tbody>
                  @for (item of p.items; track $index) {
                    <tr class="border-b border-line last:border-0">
                      <td class="w-36 border-r border-line px-4 py-2.5 font-bold" [class]="toneClass(item)">
                        <span class="flex items-center gap-2 whitespace-nowrap">
                          <i class="ti {{ toneIcon(item) }}" aria-hidden="true"></i>{{ value(item) }}
                        </span>
                      </td>
                      <td class="px-4 py-2.5 text-ink">
                        {{ item.description }}
                        @if (item.quantity > 1) {
                          <span class="text-xs text-ink-muted">· ×{{ item.quantity }}</span>
                        }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            }
          </div>

          <div class="panel flex flex-col gap-3 p-6">
            <h2 class="text-lg font-bold text-ink">Events</h2>
            @if (p.events.length === 0) {
              <p class="text-sm text-ink-muted">No events reported.</p>
            } @else {
              <ul class="flex flex-col gap-2 text-sm">
                @for (e of p.events; track $index) {
                  <li class="flex items-center justify-between gap-3 rounded-lg bg-surface-alt px-3 py-2">
                    <span class="font-semibold text-ink">{{ e.type }}<span class="font-normal text-ink-muted">{{ e.detail ? ' · ' + e.detail : '' }}</span></span>
                    <span class="whitespace-nowrap text-xs text-ink-muted">{{ e.at | date: 'HH:mm:ss' : 'UTC' }}Z · {{ e.durationSeconds }} s</span>
                  </li>
                }
              </ul>
            }

            @if (p.comments) {
              <h3 class="mt-2 font-semibold text-ink">Comments</h3>
              <p class="whitespace-pre-line rounded-lg bg-surface-alt px-3 py-2 text-sm text-ink">{{ p.comments }}</p>
            }
          </div>
        </div>
      }
    </section>
  `
})
export class PirepDetail implements OnInit {
  private readonly api = inject(PirepService);
  private readonly route = inject(ActivatedRoute);

  protected readonly format = formatMinutes;
  protected readonly networks = NETWORK_LABELS;
  protected readonly statusClass = PIREP_STATUS_CLASS;

  protected readonly pirep = signal<Pirep | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly conformityClass = computed(() => {
    const value = this.pirep()?.summary.conformity ?? 0;
    return value >= 98 ? 'text-onda-500' : value >= 80 ? 'text-ink' : 'text-danger';
  });

  async ngOnInit(): Promise<void> {
    try {
      this.pirep.set(await this.api.get(this.route.snapshot.paramMap.get('id') ?? ''));
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Could not load the PIREP.');
    } finally {
      this.loading.set(false);
    }
  }

  protected value(item: PirepScoreItem): string {
    if (item.outcome === 'Reject') return 'Rejection';
    const sign = item.points > 0 ? '+' : item.points < 0 ? '−' : '';
    return `${sign}${Math.abs(item.points)} pts`;
  }

  protected toneClass(item: PirepScoreItem): string {
    return RULE_TONE_CLASS[ruleTone(item)];
  }

  protected toneIcon(item: PirepScoreItem): string {
    return RULE_TONE_ICON[ruleTone(item)];
  }
}