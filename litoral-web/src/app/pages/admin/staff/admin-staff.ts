import { NgTemplateOutlet } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { apiErrorMessage } from '../../../core/api/api-error';
import { AdminStaffService, StaffMember, StaffOverview, StaffPosition, StaffTeam } from '../../../core/admin/admin-staff.service';
import { AuthService } from '../../../core/auth/auth.service';

const TEAMS: { team: StaffTeam; label: string; icon: string }[] = [
  { team: 'Executive', label: 'Executive Team', icon: 'ti-crown' },
  { team: 'Operations', label: 'Operations Team', icon: 'ti-settings' },
  { team: 'Safety', label: 'Safety Team', icon: 'ti-shield-check' }
];

@Component({
  selector: 'app-admin-staff',
  imports: [NgTemplateOutlet, RouterLink],
  template: `
    <div class="flex flex-col gap-6">
      <div>
        <h1 class="text-2xl font-bold text-ink">Staff</h1>
        <p class="text-ink-muted">Assign and remove staff positions. Changes apply right away.</p>
      </div>

      @if (loading()) {
        <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (!overview()) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-12 text-center">
          <p class="text-danger">Something went wrong. Try again.</p>
          <button type="button" class="btn-secondary" (click)="load()">Try again</button>
        </div>
      } @else {
        @for (group of groups(); track group.team) {
          <section class="flex flex-col gap-3">
            <h2 class="flex items-center gap-2 text-sm font-semibold uppercase tracking-wide text-ink-muted">
              <i class="ti {{ group.icon }} text-lg" aria-hidden="true"></i>{{ group.label }}
            </h2>
            <div class="grid items-start gap-4 md:grid-cols-2">
              @for (position of group.positions; track position.code) {
                <article class="panel card-hover flex flex-col gap-4 p-5">
                  <div class="flex items-start justify-between gap-3">
                    <div>
                      <h3 class="font-bold text-ink">{{ position.name }}</h3>
                      <p class="text-xs text-ink-muted">{{ position.singleHolder ? 'Single position' : 'Multiple members' }}</p>
                    </div>
                    @if (!position.editable) {
                      <span class="flex items-center gap-1 rounded-full bg-surface-alt px-2.5 py-1 text-[11px] font-semibold text-ink-muted"
                        [title]="position.code === 'CEO_FOUNDER' ? 'This position can not be changed' : 'Only the CEO & Founder can change this position'">
                        <i class="ti ti-lock" aria-hidden="true"></i>Locked
                      </span>
                    }
                  </div>

                  <div class="flex flex-col gap-2">
                    @for (member of position.holders; track member.pilotId) {
                      <div class="flex items-center gap-3 rounded-lg bg-surface-alt px-3 py-2">
                        <ng-container *ngTemplateOutlet="avatar; context: { $implicit: member }" />
                        <div class="min-w-0 flex-1">
                          <a [routerLink]="['/profile', member.pilotNumber]" class="block truncate font-semibold text-ink hover:text-primary hover:underline">{{ member.name }}</a>
                          <p class="font-mono text-xs text-ink-muted">{{ member.displayId }}</p>
                        </div>
                        @if (position.editable && member.pilotNumber !== myNumber()) {
                          @if (confirmRemove() === key(position, member)) {
                            <div class="flex items-center gap-1">
                              <button type="button" class="rounded-md bg-danger px-2.5 py-1 text-xs font-medium text-white hover:opacity-90"
                                [disabled]="busy()" (click)="remove(position, member)">Remove</button>
                              <button type="button" class="btn-secondary px-2.5 py-1 text-xs" (click)="confirmRemove.set(null)">Cancel</button>
                            </div>
                          } @else {
                            <button type="button" class="icon-btn h-8 w-8 text-lg hover:text-danger" title="Remove from position" aria-label="Remove from position"
                              (click)="askRemove(position, member)">
                              <i class="ti ti-x" aria-hidden="true"></i>
                            </button>
                          }
                        }
                      </div>
                    } @empty {
                      <p class="rounded-lg border border-dashed border-line px-3 py-3 text-center text-sm text-ink-muted">Vacant</p>
                    }
                  </div>

                  @if (position.editable) {
                    @if (picking() === position.code) {
                      <div class="flex flex-col gap-2">
                        <input type="search" class="field-input" placeholder="Search by name or ID" autofocus
                          [value]="query()" (input)="query.set($any($event.target).value)" />
                        @if (position.singleHolder && position.holders.length) {
                          <p class="text-xs text-ink-muted">The selected pilot will replace {{ position.holders[0].name }}.</p>
                        }
                        <div class="flex max-h-56 flex-col overflow-y-auto rounded-lg border border-line">
                          @for (member of matches(position); track member.pilotId) {
                            <button type="button" class="flex items-center gap-3 border-b border-line px-3 py-2 text-left last:border-0 hover:bg-surface-alt"
                              [disabled]="busy()" (click)="assign(position, member)">
                              <ng-container *ngTemplateOutlet="avatar; context: { $implicit: member }" />
                              <span class="min-w-0 flex-1">
                                <span class="block truncate font-semibold text-ink">{{ member.name }}</span>
                                <span class="font-mono text-xs text-ink-muted">{{ member.displayId }}</span>
                              </span>
                            </button>
                          } @empty {
                            <p class="px-3 py-3 text-center text-sm text-ink-muted">No active pilots found.</p>
                          }
                        </div>
                        <button type="button" class="btn-secondary self-start" (click)="closePicker()">Cancel</button>
                      </div>
                    } @else {
                      <button type="button" class="btn-secondary gap-2 self-start" (click)="openPicker(position.code)">
                        <i class="ti ti-user-plus" aria-hidden="true"></i>
                        {{ position.singleHolder && position.holders.length ? 'Replace' : 'Assign' }}
                      </button>
                    }
                  }

                  @if (error()?.code === position.code) {
                    <p class="text-sm text-danger">{{ error()!.message }}</p>
                  }
                </article>
              }
            </div>
          </section>
        }
      }
    </div>

    <ng-template #avatar let-member>
      <span class="flex h-9 w-9 shrink-0 items-center justify-center overflow-hidden rounded-full bg-primary text-xs font-bold text-on-primary">
        @if (member.photoUrl) {
          <img [src]="member.photoUrl" alt="" class="h-full w-full object-cover" />
        } @else {
          {{ initials(member.name) }}
        }
      </span>
    </ng-template>
  `
})
export class AdminStaff implements OnInit {
  private readonly api = inject(AdminStaffService);
  private readonly auth = inject(AuthService);

  protected readonly overview = signal<StaffOverview | null>(null);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly picking = signal<string | null>(null);
  protected readonly query = signal('');
  protected readonly confirmRemove = signal<string | null>(null);
  protected readonly error = signal<{ code: string; message: string } | null>(null);

  protected readonly myNumber = computed(() => this.auth.pilot()?.pilotNumber ?? null);

  protected readonly groups = computed(() => {
    const positions = this.overview()?.positions ?? [];
    return TEAMS.map(t => ({ ...t, positions: positions.filter(p => p.team === t.team) })).filter(g => g.positions.length);
  });

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    try {
      this.overview.set(await this.api.get());
    } catch {
      this.overview.set(null);
    } finally {
      this.loading.set(false);
    }
  }

  protected matches(position: StaffPosition): StaffMember[] {
    const term = this.query().trim().toLowerCase();
    const holders = new Set(position.holders.map(h => h.pilotId));
    return (this.overview()?.candidates ?? [])
      .filter(c => !holders.has(c.pilotId))
      .filter(c => !term || c.name.toLowerCase().includes(term) || c.displayId.toLowerCase().includes(term) || String(c.pilotNumber).includes(term))
      .slice(0, 30);
  }

  protected openPicker(code: string): void {
    this.error.set(null);
    this.confirmRemove.set(null);
    this.query.set('');
    this.picking.set(code);
  }

  protected closePicker(): void {
    this.picking.set(null);
    this.query.set('');
  }

  protected key(position: StaffPosition, member: StaffMember): string {
    return `${position.code}:${member.pilotId}`;
  }

  protected askRemove(position: StaffPosition, member: StaffMember): void {
    this.error.set(null);
    this.confirmRemove.set(this.key(position, member));
  }

  protected async assign(position: StaffPosition, member: StaffMember): Promise<void> {
    await this.run(position.code, () => this.api.assign(position.code, member.pilotId));
    if (!this.error()) this.closePicker();
  }

  protected async remove(position: StaffPosition, member: StaffMember): Promise<void> {
    await this.run(position.code, () => this.api.remove(position.code, member.pilotId));
    this.confirmRemove.set(null);
  }

  protected initials(name: string): string {
    return name.split(/[\s.]+/).filter(Boolean).map(p => p[0]).slice(0, 2).join('').toUpperCase();
  }

  private async run(code: string, action: () => Promise<StaffOverview>): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    try {
      this.overview.set(await action());
    } catch (error) {
      this.error.set({ code, message: apiErrorMessage(error) ?? 'Something went wrong. Try again.' });
    } finally {
      this.busy.set(false);
    }
  }
}