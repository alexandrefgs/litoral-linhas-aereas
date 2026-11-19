import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { apiErrorMessage } from '../../../core/api/api-error';
import { AdminAward, AwardHolder, AwardPilotOption } from '../../../core/awards/awards.models';
import { AwardsService } from '../../../core/awards/awards.service';
import { ConfirmService } from '../../../shared/confirm-dialog/confirm.service';

@Component({
  selector: 'app-award-holders-modal',
  imports: [DatePipe, RouterLink],
  host: { '(document:keydown.escape)': 'close()' },
  template: `
    <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/50 p-4" (click)="close()">
      <div (click)="$event.stopPropagation()" class="panel flex max-h-[90vh] w-full max-w-2xl flex-col overflow-hidden shadow-2xl">
        <div class="flex items-center justify-between border-b border-line px-6 py-4">
          <div>
            <h2 class="text-lg font-bold text-ink">{{ award().name }}</h2>
            <p class="text-sm text-ink-muted">{{ holders().length }} {{ holders().length === 1 ? 'pilot has' : 'pilots have' }} this award</p>
          </div>
          <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Close" (click)="close()">
            <i class="ti ti-x" aria-hidden="true"></i>
          </button>
        </div>

        <div class="flex flex-col gap-5 overflow-y-auto px-6 py-5">
          @if (canGrant()) {
            <section class="flex flex-col gap-2 rounded-xl border border-line p-4">
              <h3 class="font-semibold text-ink">Grant to a pilot</h3>
              @if (selected(); as pilot) {
                <div class="flex items-center gap-3 rounded-lg bg-surface-alt px-3 py-2">
                  <span class="min-w-0 flex-1">
                    <span class="block truncate font-semibold text-ink">{{ pilot.name }}</span>
                    <span class="font-mono text-xs text-ink-muted">{{ pilot.displayId }}</span>
                  </span>
                  <button type="button" class="icon-btn h-8 w-8 text-lg" aria-label="Change pilot" (click)="selected.set(null)">
                    <i class="ti ti-x" aria-hidden="true"></i>
                  </button>
                </div>
                <label for="grantNote" class="field-label mt-1">Note (optional)</label>
                <input id="grantNote" maxlength="300" class="field-input" placeholder="Shown to the pilot, e.g. Thanks for organizing the fly-in"
                  [value]="note()" (input)="note.set($any($event.target).value)" />
                <button type="button" class="btn-primary flex items-center gap-2 self-end" [disabled]="working()" (click)="grant()">
                  <i class="ti ti-award" aria-hidden="true"></i>{{ working() ? 'Loading...' : 'Grant award' }}
                </button>
              } @else {
                <input type="search" class="field-input" placeholder="Search by name or ID"
                  [value]="query()" (input)="query.set($any($event.target).value)" />
                <div class="flex max-h-48 flex-col gap-1 overflow-y-auto">
                  @for (pilot of options(); track pilot.pilotId) {
                    <button type="button" class="flex items-center gap-3 rounded-lg px-3 py-2 text-left hover:bg-surface-alt" (click)="selected.set(pilot)">
                      <span class="min-w-0 flex-1">
                        <span class="block truncate font-semibold text-ink">{{ pilot.name }}</span>
                        <span class="font-mono text-xs text-ink-muted">{{ pilot.displayId }}</span>
                      </span>
                    </button>
                  } @empty {
                    <p class="px-3 py-3 text-center text-sm text-ink-muted">{{ pilots().length ? 'No pilots found.' : 'Loading...' }}</p>
                  }
                </div>
              }
            </section>
          } @else if (award().kind === 'Tour') {
            <p class="rounded-lg bg-surface-alt px-4 py-3 text-sm text-ink-muted">Tour awards are granted automatically when a pilot completes the tour.</p>
          } @else {
            <p class="rounded-lg bg-surface-alt px-4 py-3 text-sm text-ink-muted">This award is inactive. Activate it to grant it again.</p>
          }

          @if (error(); as message) {
            <p class="rounded-lg bg-danger/10 px-4 py-3 text-sm text-danger">{{ message }}</p>
          }

          <section class="flex flex-col gap-2">
            @for (holder of holders(); track holder.pilotId) {
              <div class="flex items-start gap-3 rounded-lg bg-surface-alt px-3 py-2">
                <div class="min-w-0 flex-1">
                  <a [routerLink]="['/profile', holder.pilotNumber]" class="font-semibold text-ink hover:text-primary hover:underline">{{ holder.name }}</a>
                  <span class="ml-2 font-mono text-xs text-ink-muted">{{ holder.displayId }}</span>
                  @if (holder.note) {
                    <p class="text-sm text-ink">{{ holder.note }}</p>
                  }
                  <p class="text-xs text-ink-muted">{{ holder.grantedAt | date: 'd MMM y, HH:mm' }} · {{ holder.grantedBy }}</p>
                </div>
                @if (award().kind === 'Manual') {
                  <button type="button" class="icon-btn h-8 w-8 text-lg hover:text-danger" title="Revoke award" aria-label="Revoke award"
                    [disabled]="working()" (click)="revoke(holder)">
                    <i class="ti ti-x" aria-hidden="true"></i>
                  </button>
                }
              </div>
            } @empty {
              <p class="rounded-lg border border-dashed border-line px-3 py-6 text-center text-sm text-ink-muted">{{ loading() ? 'Loading...' : 'Nobody has this award yet.' }}</p>
            }
          </section>
        </div>
      </div>
    </div>
  `
})
export class AwardHoldersModal implements OnInit {
  private readonly api = inject(AwardsService);
  private readonly confirm = inject(ConfirmService);

  readonly award = input.required<AdminAward>();
  readonly changed = output<number>();
  readonly closed = output<void>();

  protected readonly holders = signal<AwardHolder[]>([]);
  protected readonly pilots = signal<AwardPilotOption[]>([]);
  protected readonly selected = signal<AwardPilotOption | null>(null);
  protected readonly query = signal('');
  protected readonly note = signal('');
  protected readonly loading = signal(true);
  protected readonly working = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly canGrant = computed(() => this.award().kind === 'Manual' && this.award().isActive);

  protected readonly options = computed(() => {
    const held = new Set(this.holders().map(h => h.pilotId));
    const q = this.query().trim().toLowerCase();
    return this.pilots()
      .filter(p => !held.has(p.pilotId))
      .filter(p => !q || p.name.toLowerCase().includes(q) || p.displayId.toLowerCase().includes(q))
      .slice(0, 30);
  });

  async ngOnInit(): Promise<void> {
    try {
      const [holders, pilots] = await Promise.all([
        this.api.holders(this.award().id),
        this.canGrant() ? this.api.pilots() : Promise.resolve([])
      ]);
      this.holders.set(holders);
      this.pilots.set(pilots);
    } catch {
      this.error.set('Something went wrong. Try again.');
    } finally {
      this.loading.set(false);
    }
  }

  protected close(): void {
    if (!this.working()) this.closed.emit();
  }

  protected async grant(): Promise<void> {
    const pilot = this.selected();
    if (!pilot) return;

    this.working.set(true);
    this.error.set(null);
    try {
      this.holders.set(await this.api.grant(this.award().id, pilot.pilotId, this.note().trim() || null));
      this.selected.set(null);
      this.note.set('');
      this.query.set('');
      this.changed.emit(this.holders().length);
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.working.set(false);
    }
  }

  protected async revoke(holder: AwardHolder): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Revoke award?',
      message: `${holder.name} will lose the award "${this.award().name}".`,
      confirmLabel: 'Revoke',
      tone: 'danger',
      icon: 'ti-award-off'
    });
    if (!ok) return;

    this.working.set(true);
    this.error.set(null);
    try {
      this.holders.set(await this.api.revoke(this.award().id, holder.pilotId));
      this.changed.emit(this.holders().length);
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.working.set(false);
    }
  }
}