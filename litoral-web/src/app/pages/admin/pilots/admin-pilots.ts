import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { apiErrorMessage } from '../../../core/api/api-error';
import { AdminPilot, AdminPilotsService } from '../../../core/admin/admin-pilots.service';
import { AuthService } from '../../../core/auth/auth.service';
import { STAFF_POSITION_NAMES } from '../../../core/staff/staff-positions';
import { ConfirmService } from '../../../shared/confirm-dialog/confirm.service';
import { DeleteAccountDialog } from '../../../shared/delete-account-dialog/delete-account-dialog';
import { formatMinutes } from '../../../shared/utils/flight';

type StatusFilter = 'active' | 'inactive' | 'all';

const PAGE_SIZE = 25;

@Component({
  selector: 'app-admin-pilots',
  imports: [DatePipe, DecimalPipe, RouterLink, DeleteAccountDialog],
  template: `
    <div class="flex flex-col gap-6">
      <div>
        <h1 class="text-2xl font-bold text-ink">Pilots</h1>
        <p class="text-ink-muted">Deactivate, reactivate or delete pilot accounts.</p>
      </div>

      <div class="grid gap-4 sm:grid-cols-3">
        @for (stat of stats(); track stat.label) {
          <div class="panel card-hover flex items-center gap-3 p-4">
            <span class="stat-icon"><i class="ti {{ stat.icon }}" aria-hidden="true"></i></span>
            <div>
              <p class="text-2xl font-bold text-ink">{{ stat.value }}</p>
              <p class="text-xs text-ink-muted">{{ stat.label }}</p>
            </div>
          </div>
        }
      </div>

      <div class="flex flex-wrap items-end justify-between gap-3 border-b border-line">
        <div class="flex gap-1">
          @for (option of tabs; track option.value) {
            <button type="button" (click)="setStatus(option.value)"
              class="-mb-px border-b-2 px-4 py-2.5 text-sm font-semibold"
              [class]="status() === option.value ? 'border-primary text-primary' : 'border-transparent text-ink-muted hover:text-ink'">
              {{ option.label }}
            </button>
          }
        </div>
        <div class="relative mb-2 w-full sm:w-72">
          <i class="ti ti-search pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-ink-muted" aria-hidden="true"></i>
          <input type="search" class="field-input pl-9" placeholder="Name, ID or email" [value]="search()" (input)="setSearch($any($event.target).value)" />
        </div>
      </div>

      <div class="panel overflow-x-auto">
        @if (loading()) {
          <p class="px-6 py-12 text-center text-ink-muted">Loading...</p>
        } @else if (loadError()) {
          <div class="flex flex-col items-center gap-3 px-6 py-12 text-center">
            <p class="text-danger">Something went wrong. Try again.</p>
            <button type="button" class="btn-secondary" (click)="load()">Try again</button>
          </div>
        } @else if (filtered().length === 0) {
          <p class="px-6 py-12 text-center text-ink-muted">No pilots found.</p>
        } @else {
          <table class="w-full text-left text-sm">
            <thead class="border-b border-line text-xs uppercase tracking-wide text-ink-muted">
              <tr>
                <th class="px-4 py-3 font-semibold">Pilot</th>
                <th class="px-4 py-3 font-semibold">Base / location</th>
                <th class="px-4 py-3 font-semibold">Hours</th>
                <th class="px-4 py-3 font-semibold">Points</th>
                <th class="px-4 py-3 font-semibold">Flights</th>
                <th class="px-4 py-3 font-semibold">Last flight</th>
                <th class="px-4 py-3 font-semibold">Joined</th>
                <th class="px-4 py-3 font-semibold">Status</th>
                <th class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody>
              @for (p of page(); track p.id) {
                <tr class="border-b border-line last:border-0 hover:bg-surface-alt/50" [class.opacity-60]="!p.isActive">
                  <td class="px-4 py-3">
                    <div class="flex items-center gap-3">
                      <span class="flex h-9 w-9 shrink-0 items-center justify-center overflow-hidden rounded-full bg-primary text-xs font-bold text-on-primary">
                        @if (p.photoUrl) {
                          <img [src]="p.photoUrl" alt="" class="h-full w-full object-cover" />
                        } @else {
                          {{ initials(p.fullName) }}
                        }
                      </span>
                      <div class="min-w-0">
                        <a [routerLink]="['/profile', p.pilotNumber]" class="font-semibold text-ink hover:text-primary hover:underline">{{ p.fullName }}</a>
                        <p class="font-mono text-xs text-ink-muted">{{ p.displayId }}</p>
                        <p class="truncate text-xs text-ink-muted">{{ p.email }}</p>
                        @if (p.staffPositions.length) {
                          <div class="mt-1 flex flex-wrap gap-1">
                            @for (code of p.staffPositions; track code) {
                              <span class="rounded-full bg-primary/10 px-2 py-0.5 text-[10px] font-semibold text-primary">{{ staffNames[code] ?? code }}</span>
                            }
                          </div>
                        }
                      </div>
                    </div>
                  </td>
                  <td class="px-4 py-3 font-mono text-ink">{{ p.baseIcao }} <span class="text-ink-muted">/ {{ p.locationIcao }}</span></td>
                  <td class="px-4 py-3 text-ink">{{ format(p.flightMinutes) }}</td>
                  <td class="px-4 py-3 font-semibold" [class]="p.points < 0 ? 'text-danger' : 'text-ink'">{{ p.points | number }}</td>
                  <td class="px-4 py-3 text-ink">{{ p.flights }}</td>
                  <td class="px-4 py-3 text-ink-muted">{{ p.lastFlightAt ? (p.lastFlightAt | date: 'd MMM y') : '—' }}</td>
                  <td class="px-4 py-3 text-ink-muted">{{ p.joinedAt | date: 'd MMM y' }}</td>
                  <td class="px-4 py-3">
                    @if (p.isActive) {
                      <span class="rounded-full bg-onda-500/15 px-2.5 py-1 text-xs font-semibold text-onda-600">Active</span>
                    } @else {
                      <span class="rounded-full bg-danger/10 px-2.5 py-1 text-xs font-semibold text-danger">Inactive</span>
                    }
                  </td>
                  <td class="px-4 py-3">
                    @if (isLocked(p)) {
                      <p class="text-right text-xs text-ink-muted">{{ p.pilotNumber === myNumber() ? 'You' : 'Staff' }}</p>
                    } @else {
                      <div class="flex justify-end gap-1">
                        @if (p.isActive) {
                          <button type="button" class="icon-btn h-9 w-9 text-lg hover:text-danger" title="Deactivate" aria-label="Deactivate pilot"
                            [disabled]="busy()" (click)="deactivate(p)">
                            <i class="ti ti-user-off" aria-hidden="true"></i>
                          </button>
                        } @else {
                          <button type="button" class="icon-btn h-9 w-9 text-lg hover:text-onda-600" title="Reactivate" aria-label="Reactivate pilot"
                            [disabled]="busy()" (click)="activate(p)">
                            <i class="ti ti-user-check" aria-hidden="true"></i>
                          </button>
                        }
                        <button type="button" class="icon-btn h-9 w-9 text-lg hover:text-danger disabled:cursor-not-allowed disabled:opacity-30"
                          [title]="p.canDelete ? 'Delete permanently' : 'Pilots with an open flight or special flight in progress can not be deleted'" aria-label="Delete pilot"
                          [disabled]="!p.canDelete" (click)="askDelete(p)">
                          <i class="ti ti-trash" aria-hidden="true"></i>
                        </button>
                      </div>
                    }
                    @if (rowError()?.id === p.id) {
                      <p class="mt-1 text-right text-xs text-danger">{{ rowError()!.message }}</p>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>

          @if (pages() > 1) {
            <div class="flex items-center justify-between border-t border-line px-4 py-3 text-sm">
              <span class="text-ink-muted">{{ filtered().length }} pilots · page {{ current() }} of {{ pages() }}</span>
              <div class="flex gap-1">
                <button type="button" class="btn-secondary px-3 py-1" [disabled]="current() === 1" (click)="current.set(current() - 1)">
                  <i class="ti ti-chevron-left" aria-hidden="true"></i>
                </button>
                <button type="button" class="btn-secondary px-3 py-1" [disabled]="current() === pages()" (click)="current.set(current() + 1)">
                  <i class="ti ti-chevron-right" aria-hidden="true"></i>
                </button>
              </div>
            </div>
          }
        }
      </div>
    </div>

    @if (toDelete(); as target) {
      <app-delete-account-dialog [name]="target.fullName + ' (' + target.displayId + ')'" [busy]="busy()" [error]="deleteError()"
        (confirmed)="remove(target)" (cancelled)="toDelete.set(null)" />
    }
  `
})
export class AdminPilots implements OnInit {
  private readonly api = inject(AdminPilotsService);
  private readonly confirm = inject(ConfirmService);
  private readonly auth = inject(AuthService);

  protected readonly format = formatMinutes;
  protected readonly staffNames = STAFF_POSITION_NAMES;
  protected readonly tabs: { value: StatusFilter; label: string }[] = [
    { value: 'active', label: 'Active' },
    { value: 'inactive', label: 'Inactive' },
    { value: 'all', label: 'All' }
  ];

  protected readonly pilots = signal<AdminPilot[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly status = signal<StatusFilter>('active');
  protected readonly search = signal('');
  protected readonly current = signal(1);
  protected readonly busy = signal(false);
  protected readonly rowError = signal<{ id: string; message: string } | null>(null);
  protected readonly toDelete = signal<AdminPilot | null>(null);
  protected readonly deleteError = signal<string | null>(null);

  protected readonly myNumber = computed(() => this.auth.pilot()?.pilotNumber ?? null);

  protected readonly stats = computed(() => {
    const all = this.pilots();
    const active = all.filter(p => p.isActive).length;
    return [
      { icon: 'ti-users', label: 'Pilots', value: all.length },
      { icon: 'ti-user-check', label: 'Active', value: active },
      { icon: 'ti-user-off', label: 'Inactive', value: all.length - active }
    ];
  });

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const status = this.status();
    return this.pilots().filter(p =>
      (status === 'all' || (status === 'active') === p.isActive)
      && (!term
        || p.fullName.toLowerCase().includes(term)
        || p.displayId.toLowerCase().includes(term)
        || String(p.pilotNumber).includes(term)
        || p.email.toLowerCase().includes(term)));
  });

  protected readonly pages = computed(() => Math.max(1, Math.ceil(this.filtered().length / PAGE_SIZE)));

  protected readonly page = computed(() => {
    const start = (Math.min(this.current(), this.pages()) - 1) * PAGE_SIZE;
    return this.filtered().slice(start, start + PAGE_SIZE);
  });

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);
    try {
      this.pilots.set(await this.api.list());
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected setStatus(value: StatusFilter): void {
    this.status.set(value);
    this.current.set(1);
  }

  protected setSearch(value: string): void {
    this.search.set(value);
    this.current.set(1);
  }

  protected isLocked(pilot: AdminPilot): boolean {
    return pilot.staffPositions.length > 0 || pilot.pilotNumber === this.myNumber();
  }

  protected askDelete(pilot: AdminPilot): void {
    this.rowError.set(null);
    this.deleteError.set(null);
    this.toDelete.set(pilot);
  }

  protected async remove(pilot: AdminPilot): Promise<void> {
    this.busy.set(true);
    this.deleteError.set(null);
    try {
      await this.api.remove(pilot.id);
      this.pilots.update(list => list.filter(p => p.id !== pilot.id));
      this.toDelete.set(null);
    } catch (error) {
      this.deleteError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.busy.set(false);
    }
  }

  protected async deactivate(pilot: AdminPilot): Promise<void> {
    this.rowError.set(null);
    const ok = await this.confirm.ask({
      title: `Deactivate ${pilot.fullName}?`,
      message: 'The pilot will not be able to book or fly until the account is reactivated. Hours, points and flights are kept.',
      confirmLabel: 'Yes, deactivate',
      tone: 'warning',
      icon: 'ti-user-off'
    });
    if (!ok) return;

    this.busy.set(true);
    try {
      this.replace(await this.api.deactivate(pilot.id));
    } catch (error) {
      this.rowError.set({ id: pilot.id, message: apiErrorMessage(error) ?? 'Something went wrong. Try again.' });
    } finally {
      this.busy.set(false);
    }
  }

  protected async activate(pilot: AdminPilot): Promise<void> {
    this.rowError.set(null);
    this.busy.set(true);
    try {
      this.replace(await this.api.activate(pilot.id));
    } catch (error) {
      this.rowError.set({ id: pilot.id, message: apiErrorMessage(error) ?? 'Something went wrong. Try again.' });
    } finally {
      this.busy.set(false);
    }
  }

  protected initials(name: string): string {
    return name.split(/\s+/).filter(Boolean).map(p => p[0]).slice(0, 2).join('').toUpperCase();
  }

  private replace(updated: AdminPilot): void {
    this.pilots.update(list => list.map(p => (p.id === updated.id ? updated : p)));
  }
}