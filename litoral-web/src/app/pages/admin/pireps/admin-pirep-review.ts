import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AdminPirepsService } from '../../../core/admin/admin-pireps.service';
import { apiErrorMessage } from '../../../core/api/api-error';
import { AdminPirep, NETWORK_LABELS, PIREP_STATUS_CLASS, PirepScoreItem } from '../../../core/pirep/pirep.models';
import { RULE_TONE_CLASS, RULE_TONE_ICON, ruleTone } from '../../../core/scoring/scoring.models';
import { ConfirmService } from '../../../shared/confirm-dialog/confirm.service';
import { TrackMap } from '../../../shared/track-map/track-map';
import { formatMinutes } from '../../../shared/utils/flight';

type Decision = 'approve' | 'reject';

@Component({
  selector: 'app-admin-pirep-review',
  imports: [DatePipe, DecimalPipe, FormsModule, RouterLink, TrackMap],
  template: `
    <div class="flex flex-col gap-6">
      <a routerLink="/admin/pireps" class="flex w-fit items-center gap-1 text-sm font-medium text-primary hover:underline">
        <i class="ti ti-arrow-left" aria-hidden="true"></i>Review queue
      </a>

      @if (loading()) {
        <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (loadError()) {
        <p class="panel px-6 py-12 text-center text-danger">{{ loadError() }}</p>
      } @else if (data(); as d) {
        @let p = d.pirep;
        @let s = p.summary;

        <div class="panel flex flex-wrap items-center justify-between gap-4 p-6">
          <div>
            <p class="font-mono text-2xl font-bold text-ink">{{ s.callsign }}</p>
            <p class="font-mono text-ink">
              {{ s.departureIcao }} → {{ s.arrivalIcao }}
              @if (s.arrivalIcao !== s.plannedArrivalIcao) {
                <span class="ml-1 font-sans text-sm text-danger">diverted from {{ s.plannedArrivalIcao }}</span>
              }
            </p>
            <p class="text-sm text-ink-muted">
              {{ p.pilotName }} ({{ p.pilotId }}) · {{ d.pilotEmail }} · {{ d.pilotPoints | number }} pts
              @if (!d.pilotIsActive) {
                <span class="ml-1 rounded bg-danger/10 px-1.5 py-0.5 text-xs font-semibold text-danger">Inactive</span>
              }
            </p>
            <p class="text-sm text-ink-muted">
              {{ s.registration }} · {{ s.aircraftTypeIcao }} · {{ networks[s.network] }} ·
              {{ s.source === 'Manual' ? 'Manual PIREP' : 'ACARS' }} · filed {{ s.submittedAt | date: 'd MMM, HH:mm' }}
            </p>
          </div>
          <div class="flex flex-wrap items-center gap-6">
            <div class="text-center">
              <p class="text-xs uppercase tracking-wide text-ink-muted">Points</p>
              <p class="text-2xl font-bold" [class]="s.hasRejection ? 'text-danger' : 'text-ink'">{{ s.hasRejection ? 'Rejection' : s.scorePoints }}</p>
            </div>
            <div class="text-center">
              <p class="text-xs uppercase tracking-wide text-ink-muted">Conformity</p>
              <p class="text-2xl font-bold" [class]="conformityClass()">{{ s.conformity }}%</p>
            </div>
            <span class="rounded-full px-3 py-1.5 text-sm font-semibold" [class]="statusClass[s.status]">{{ s.status }}</span>
          </div>
        </div>

        @if (s.status !== 'Pending') {
          <p class="rounded-lg bg-surface-alt px-4 py-3 text-sm text-ink">
            <span class="font-semibold">{{ s.status }}</span>
            {{ p.reviewedAt ? 'on ' + (p.reviewedAt | date: 'd MMM yyyy, HH:mm') : '' }}
            {{ d.reviewedByName ? 'by ' + d.reviewedByName : '' }}.
            @if (p.reviewNote) {
              <span class="text-ink-muted">“{{ p.reviewNote }}”</span>
            }
          </p>
        }

        <div class="grid gap-6 lg:grid-cols-[2fr_1fr]">
          <div class="panel overflow-hidden">
            @if (p.track.length > 0) {
              <app-track-map [track]="p.track" class="h-[360px]" />
            } @else {
              <div class="flex h-[360px] flex-col items-center justify-center gap-2 text-ink-muted">
                <i class="ti ti-map-off text-4xl" aria-hidden="true"></i>
                <p>No track recorded for this flight.</p>
              </div>
            }
          </div>

          <div class="panel grid grid-cols-2 gap-4 p-6 text-sm">
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Block off</p><p class="font-semibold text-ink">{{ p.blockOffAt | date: 'd MMM, HH:mm' : 'UTC' }}Z</p></div>
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Block on</p><p class="font-semibold text-ink">{{ p.blockOnAt | date: 'd MMM, HH:mm' : 'UTC' }}Z</p></div>
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Block time</p><p class="font-semibold text-ink">{{ format(s.blockMinutes) }}</p></div>
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Air time</p><p class="font-semibold text-ink">{{ p.airMinutes !== null ? format(p.airMinutes) : '—' }}</p></div>
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Landing rate</p><p class="font-semibold text-ink">{{ s.landingRateFpm !== null ? s.landingRateFpm + ' fpm' : '—' }}</p></div>
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Fuel used</p><p class="font-semibold text-ink">{{ p.fuelUsedKg !== null ? (p.fuelUsedKg | number: '1.0-0') + ' kg' : '—' }}</p></div>
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Fuel at pushback</p><p class="font-semibold text-ink">{{ p.fuelAtPushbackKg !== null ? (p.fuelAtPushbackKg | number: '1.0-0') + ' kg' : '—' }}</p></div>
            <div><p class="text-xs uppercase tracking-wide text-ink-muted">Online</p><p class="font-semibold text-ink">{{ p.onlinePercent !== null ? (p.onlinePercent | number: '1.0-0') + '%' : '—' }}</p></div>
            @if (p.flightRoute) {
              <div class="col-span-2">
                <p class="text-xs uppercase tracking-wide text-ink-muted">Route</p>
                <p class="break-words rounded-lg bg-surface-alt px-3 py-2 font-mono text-xs text-ink">{{ p.flightRoute }}</p>
              </div>
            }
            @if (p.comments) {
              <div class="col-span-2">
                <p class="text-xs uppercase tracking-wide text-ink-muted">Pilot comments</p>
                <p class="whitespace-pre-line rounded-lg bg-surface-alt px-3 py-2 text-ink">{{ p.comments }}</p>
              </div>
            }
          </div>
        </div>

        <div class="grid gap-6 lg:grid-cols-2">
          <div class="panel flex flex-col gap-3 p-6">
            <div class="flex items-center justify-between">
              <h2 class="text-lg font-bold text-ink">Score</h2>
              @if (pending()) {
                <span class="text-xs text-ink-muted">Click <i class="ti ti-eye-off" aria-hidden="true"></i> to ignore a false positive</span>
              }
            </div>

            @if (p.items.length === 0) {
              <p class="text-sm text-ink-muted">No scoring items.</p>
            } @else {
              <table class="w-full border border-line text-left text-sm">
                <tbody>
                  @for (item of p.items; track $index) {
                    <tr class="border-b border-line last:border-0" [class.opacity-50]="item.isVoided">
                      <td class="w-36 border-r border-line px-4 py-2.5 font-bold" [class]="toneClass(item)">
                        <span class="flex items-center gap-2 whitespace-nowrap">
                          <i class="ti {{ toneIcon(item) }}" aria-hidden="true"></i>{{ value(item) }}
                        </span>
                      </td>
                      <td class="px-4 py-2.5 text-ink" [class.line-through]="item.isVoided">
                        {{ item.description }}
                        @if (item.quantity > 1) {
                          <span class="text-xs text-ink-muted">· ×{{ item.quantity }}</span>
                        }
                        @if (item.isManual) {
                          <span class="ml-1 rounded bg-primary/10 px-1.5 py-0.5 text-[10px] font-semibold uppercase text-primary">Review</span>
                        }
                      </td>
                      @if (pending() && item.ruleId) {
                        <td class="w-12 px-2 text-right">
                          <button type="button" class="icon-btn h-8 w-8 hover:text-danger" [disabled]="busy()"
                            [attr.aria-label]="item.isManual ? 'Remove item' : item.isVoided ? 'Restore item' : 'Ignore item'"
                            [title]="item.isManual ? 'Remove' : item.isVoided ? 'Restore' : 'Ignore'"
                            (click)="toggle(item)">
                            <i class="ti" [class]="item.isManual ? 'ti-trash' : item.isVoided ? 'ti-eye' : 'ti-eye-off'" aria-hidden="true"></i>
                          </button>
                        </td>
                      }
                    </tr>
                  }
                </tbody>
              </table>
            }

            @if (pending() && d.reviewRules.length > 0) {
              <div class="mt-2 flex flex-wrap items-end gap-2 rounded-lg bg-surface-alt p-3">
                <div class="min-w-[220px] flex-1">
                  <label for="reviewRule" class="field-label">Add review item</label>
                  <select id="reviewRule" class="field-input" [(ngModel)]="ruleId">
                    <option value="">Choose a rule</option>
                    @for (rule of d.reviewRules; track rule.id) {
                      <option [value]="rule.id">{{ rule.category }} · {{ rule.description }} ({{ ruleValue(rule) }})</option>
                    }
                  </select>
                </div>
                <div class="w-20">
                  <label for="quantity" class="field-label">Qty</label>
                  <input id="quantity" type="number" min="1" max="100" class="field-input" [(ngModel)]="quantity" />
                </div>
                <button type="button" class="btn-secondary gap-2" [disabled]="!ruleId || busy()" (click)="addItem()">
                  <i class="ti ti-plus" aria-hidden="true"></i>Add
                </button>
              </div>
            }
          </div>

          <div class="flex flex-col gap-6">
            @if (d.melChecks.length) {
              <div class="panel flex flex-col gap-3 p-6">
                <h2 class="flex items-center gap-2 text-lg font-bold text-ink"><i class="ti ti-tool text-xl text-highlight" aria-hidden="true"></i>MEL items on this flight</h2>
                @for (m of d.melChecks; track m.id) {
                  <div class="rounded-lg border p-3 text-sm"
                    [class]="m.result === 'Pending' ? 'border-highlight/60 bg-highlight/10' : m.result === 'Complied' ? 'border-onda-500/40 bg-onda-500/5' : 'border-danger/40 bg-danger/5'">
                    <p class="font-semibold text-ink"><span class="font-mono text-xs">MEL {{ m.code }}</span> · {{ m.title }}</p>
                    <p class="mt-1 text-ink"><span class="font-semibold">Restriction:</span> {{ m.restriction }}</p>
                    <p class="mt-1 text-ink-muted"><span class="font-semibold text-ink">Procedure:</span> {{ m.procedure }}</p>
                    @if (m.detail) {
                      <p class="mt-1 text-xs text-ink-muted">{{ m.detail }}</p>
                    }
                    <div class="mt-2 flex flex-wrap items-center justify-between gap-2">
                      <span class="text-xs font-semibold" [class]="m.result === 'Pending' ? 'text-ink' : m.result === 'Complied' ? 'text-onda-600' : 'text-danger'">
                        {{ m.result === 'Pending' ? 'Waiting for your decision' : m.result === 'Complied' ? 'Procedure followed' : 'Procedure not followed' }}
                      </span>
                      @if (m.result === 'Pending' && pending()) {
                        <div class="flex gap-2">
                          <button type="button" class="btn-secondary flex items-center gap-1.5 px-3 py-1.5 text-xs" [disabled]="busy()" (click)="decideMel(m.id, false)">
                            <i class="ti ti-x" aria-hidden="true"></i>Not followed
                          </button>
                          <button type="button" class="btn-primary flex items-center gap-1.5 px-3 py-1.5 text-xs" [disabled]="busy()" (click)="decideMel(m.id, true)">
                            <i class="ti ti-check" aria-hidden="true"></i>Followed
                          </button>
                        </div>
                      }
                    </div>
                  </div>
                }
              </div>
            }

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
            </div>

            @if (pending()) {
              <div class="panel flex flex-col gap-4 p-6">
                <h2 class="text-lg font-bold text-ink">Decision</h2>

                <div>
                  <label for="note" class="field-label">Note to the pilot</label>
                  <textarea id="note" rows="3" maxlength="1000" class="field-input" [(ngModel)]="note"
                    placeholder="Required when rejecting. Optional when approving."></textarea>
                </div>

                <div class="rounded-lg border border-line px-4 py-3 text-sm">
                  <p class="text-ink">
                    <i class="ti ti-circle-check mr-1 text-onda-500" aria-hidden="true"></i>
                    Approve: <span class="font-semibold">{{ s.hasRejection ? 'not possible while a rejection item is active' : melPending() ? 'decide the MEL items first' : signed(s.scorePoints) + ' pts' }}</span>
                  </p>
                  <p class="mt-1 text-ink">
                    <i class="ti ti-circle-x mr-1 text-danger" aria-hidden="true"></i>
                    Reject: {{ ordinal(d.rejectionPreview.rejectionNumber) }} rejection this month,
                    <span class="font-semibold text-danger">{{ signed(-Math.abs(d.rejectionPreview.penalty)) }} pts</span>
                    @if (d.rejectionPreview.deactivatesAccount) {
                      <span class="font-semibold text-danger"> and the account is deactivated</span>
                    }
                  </p>
                  <p class="mt-1 text-xs text-ink-muted">Hours and the aircraft position stay as they are either way.</p>
                </div>

                <div class="flex flex-wrap justify-end gap-2">
                  <button type="button" class="rounded-md border border-danger px-4 py-2 text-sm font-medium text-danger hover:bg-danger hover:text-white disabled:opacity-60"
                    [disabled]="busy()" (click)="askReject()">Reject</button>
                  <button type="button" class="rounded-md bg-onda-500 px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50"
                    [disabled]="busy() || s.hasRejection || melPending()" (click)="askApprove()">{{ busy() ? 'Loading...' : 'Approve' }}</button>
                </div>

                @if (actionError(); as message) {
                  <p class="rounded-lg bg-danger/10 px-4 py-3 text-sm text-danger">{{ message }}</p>
                }
              </div>
            }
          </div>
        </div>
      }
    </div>
  `
})
export class AdminPirepReview implements OnInit {
  private readonly api = inject(AdminPirepsService);
  private readonly route = inject(ActivatedRoute);
  private readonly confirm = inject(ConfirmService);

  protected readonly Math = Math;
  protected readonly format = formatMinutes;
  protected readonly networks = NETWORK_LABELS;
  protected readonly statusClass = PIREP_STATUS_CLASS;

  protected readonly data = signal<AdminPirep | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly actionError = signal<string | null>(null);

  protected ruleId = '';
  protected quantity = 1;
  protected note = '';

  protected readonly pending = computed(() => this.data()?.pirep.summary.status === 'Pending');
  protected readonly melPending = computed(() => (this.data()?.melChecks ?? []).some(m => m.result === 'Pending'));

  protected readonly conformityClass = computed(() => {
    const value = this.data()?.pirep.summary.conformity ?? 0;
    return value >= 98 ? 'text-onda-500' : value >= 80 ? 'text-ink' : 'text-danger';
  });

  private get id(): string {
    return this.route.snapshot.paramMap.get('id') ?? '';
  }

  async ngOnInit(): Promise<void> {
    try {
      this.data.set(await this.api.get(this.id));
    } catch (error) {
      this.loadError.set(apiErrorMessage(error) ?? 'Could not load the PIREP.');
    } finally {
      this.loading.set(false);
    }
  }

  protected toggle(item: PirepScoreItem): void {
    if (!item.ruleId) return;
    this.run(() => this.api.toggleItem(this.id, item.ruleId!));
  }

  protected addItem(): void {
    if (!this.ruleId) return;
    this.run(async () => {
      const result = await this.api.addItem(this.id, this.ruleId, Math.max(1, Number(this.quantity) || 1));
      this.ruleId = '';
      this.quantity = 1;
      return result;
    });
  }

  protected decideMel(checkId: string, complied: boolean): void {
    this.run(() => this.api.decideMel(this.id, checkId, complied));
  }

  protected async askApprove(): Promise<void> {
    const d = this.data();
    if (!d) return;
    const ok = await this.confirm.ask({
      title: 'Approve this PIREP?',
      message: `${d.pirep.summary.callsign} will be approved with ${this.signed(d.pirep.summary.scorePoints)} points.`,
      confirmLabel: 'Yes, approve',
      tone: 'success'
    });
    if (ok) this.decide('approve');
  }

  protected async askReject(): Promise<void> {
    const d = this.data();
    if (!d) return;
    if (!this.note.trim()) {
      this.actionError.set('Write a note explaining why the PIREP is being rejected.');
      return;
    }
    this.actionError.set(null);

    const preview = d.rejectionPreview;
    const details = [
      `${this.ordinal(preview.rejectionNumber)} rejection this month`,
      `${this.signed(-Math.abs(preview.penalty))} points`
    ];
    if (preview.deactivatesAccount) details.push('The pilot account will be deactivated');

    const ok = await this.confirm.ask({
      title: 'Reject this PIREP?',
      message: `${d.pirep.summary.callsign} will be rejected and the pilot will see your note.`,
      details,
      confirmLabel: 'Yes, reject',
      icon: 'ti-circle-x'
    });
    if (ok) this.decide('reject');
  }

  protected decide(decision: Decision): void {
    const note = this.note.trim();
    this.run(() => decision === 'approve'
      ? this.api.approve(this.id, note || null)
      : this.api.reject(this.id, note));
  }

  protected value(item: PirepScoreItem): string {
    if (item.outcome === 'Reject') return 'Rejection';
    return `${this.signed(item.points)} pts`;
  }

  protected ruleValue(rule: { outcome: string; points: number }): string {
    return rule.outcome === 'Reject' ? 'rejection' : `${this.signed(rule.points)} pts`;
  }

  protected signed(points: number): string {
    return points > 0 ? `+${points}` : points < 0 ? `−${Math.abs(points)}` : '0';
  }

  protected ordinal(n: number): string {
    return n === 1 ? '1st' : n === 2 ? '2nd' : n === 3 ? '3rd' : `${n}th`;
  }

  protected toneClass(item: PirepScoreItem): string {
    return RULE_TONE_CLASS[ruleTone(item)];
  }

  protected toneIcon(item: PirepScoreItem): string {
    return RULE_TONE_ICON[ruleTone(item)];
  }

  private async run(action: () => Promise<AdminPirep>): Promise<void> {
    this.busy.set(true);
    this.actionError.set(null);

    try {
      this.data.set(await action());
    } catch (error) {
      this.actionError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.busy.set(false);
    }
  }
}