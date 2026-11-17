import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { apiErrorMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { FleetAircraft, TransferQuote } from '../../core/booking/booking.models';
import { BookingService } from '../../core/booking/booking.service';
import { formatMinutes } from '../../shared/utils/flight';

const STATUS: Record<FleetAircraft['status'], { label: string; css: string }> = {
  Available: { label: 'Available', css: 'bg-accent/20 text-ink' },
  Reserved: { label: 'Reserved', css: 'bg-primary/10 text-primary' },
  InFlight: { label: 'In flight', css: 'bg-primary text-on-primary' },
  Maintenance: { label: 'Maintenance', css: 'bg-highlight text-on-highlight' }
};

@Component({
  selector: 'app-fleet',
  imports: [DatePipe],
  host: { '(document:keydown.escape)': 'closeTransfer()' },
  template: `
    <section class="mx-auto flex max-w-7xl flex-col gap-6 px-6 py-8">
      <div>
        <h1 class="text-2xl font-bold text-ink">Fleet</h1>
        <p class="text-ink-muted">Where each aircraft is right now. Click an aircraft to bring it to {{ location() }}.</p>
      </div>

      <div class="flex flex-wrap items-center gap-3">
        <div class="relative min-w-60 flex-1">
          <i class="ti ti-search pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-ink-muted" aria-hidden="true"></i>
          <input type="search" class="field-input pl-9" placeholder="Search by registration or airport"
            [value]="search()" (input)="search.set($any($event.target).value)" />
        </div>
        <select class="field-input w-auto" [value]="typeFilter()" (change)="typeFilter.set($any($event.target).value)" aria-label="Filter by type">
          <option value="">All types</option>
          @for (type of types(); track type) {
            <option [value]="type">{{ type }}</option>
          }
        </select>
        <label class="flex cursor-pointer items-center gap-2 rounded-lg border border-line px-3 py-2 text-sm text-ink">
          <input type="checkbox" class="accent-primary" [checked]="hereOnly()" (change)="hereOnly.set($any($event.target).checked)" />
          At {{ location() }}
        </label>
      </div>

      @if (loading()) {
        <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (!filtered().length) {
        <p class="panel px-6 py-12 text-center text-ink-muted">No aircraft found.</p>
      } @else {
        <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          @for (item of filtered(); track item.id) {
            <button type="button" (click)="openTransfer(item)"
              class="panel card-hover flex flex-col overflow-hidden text-left">
              <div class="relative flex aspect-[16/9] items-center justify-center bg-surface-alt">
                @if (item.photoUrl) {
                  <img [src]="item.photoUrl" [alt]="item.registration" class="h-full w-full object-cover" loading="lazy" />
                } @else {
                  <i class="ti ti-plane text-5xl text-ink-muted" aria-hidden="true"></i>
                }
                <span class="absolute right-3 top-3 rounded-full px-2.5 py-1 text-xs font-semibold" [class]="status[item.status].css">{{ status[item.status].label }}</span>
              </div>
              <div class="flex flex-col gap-1 p-4">
                <div class="flex items-center justify-between">
                  <span class="font-mono text-xl font-bold text-ink">{{ item.registration }}</span>
                  <span class="font-mono text-sm font-semibold text-primary">{{ item.typeIcao }}</span>
                </div>
                <p class="text-sm text-ink-muted">{{ item.variant || item.typeName }}</p>
                <p class="mt-1 flex items-center gap-1 text-sm text-ink">
                  <i class="ti ti-map-pin text-primary" aria-hidden="true"></i>
                  <span class="font-mono font-semibold">{{ item.locationIcao }}</span> · {{ item.locationCity }}
                </p>
                <p class="text-xs text-ink-muted">
                  {{ format(item.totalMinutes) }} flown · {{ item.lastFlightAt ? 'Last flight ' + (item.lastFlightAt | date: 'd MMM y') : 'Never flown' }}
                </p>
              </div>
            </button>
          }
        </div>
      }
    </section>

    @if (transferFor(); as item) {
      <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/50 p-4" (click)="closeTransfer()">
        <div class="panel w-full max-w-md overflow-hidden shadow-2xl" (click)="$event.stopPropagation()">
          <div class="border-b border-line px-6 py-4">
            <h2 class="text-lg font-bold text-ink">Bring {{ item.registration }} to {{ location() }}?</h2>
          </div>
          <div class="flex flex-col gap-4 px-6 py-5">
            @if (!quote()) {
              <p class="text-ink-muted">Loading...</p>
            } @else {
              @let q = quote()!;
              <div class="flex items-center justify-between rounded-lg bg-surface-alt px-4 py-3">
                <span class="font-mono text-xl font-bold text-ink">{{ q.fromIcao }}</span>
                <span class="text-sm text-ink-muted">{{ q.distanceNm }} NM</span>
                <span class="font-mono text-xl font-bold text-ink">{{ q.toIcao }}</span>
              </div>
              @if (q.blockedReason) {
                <p class="rounded-lg bg-danger/10 px-4 py-3 text-sm text-danger">{{ q.blockedReason }}</p>
              } @else if (q.free) {
                <p class="rounded-lg bg-accent/20 px-4 py-3 text-sm text-ink">
                  <i class="ti ti-gift mr-1" aria-hidden="true"></i>There is no aircraft available at {{ q.toIcao }}, so the transfer is <strong>free</strong>.
                </p>
              } @else {
                <p class="text-sm text-ink">
                  This transfer costs <strong>{{ q.cost }} points</strong> (1000 + 100 per 100 NM). You have {{ q.points }} points.
                </p>
              }
            }
            @if (transferError(); as message) {
              <p class="rounded-lg bg-danger/10 px-4 py-3 text-sm text-danger">{{ message }}</p>
            }
          </div>
          <div class="flex justify-end gap-2 border-t border-line px-6 py-4">
            <button type="button" class="btn-secondary" (click)="closeTransfer()">Cancel</button>
            <button type="button" class="btn-primary px-5" [disabled]="!quote() || !!quote()!.blockedReason || transferring()" (click)="transfer(item)">
              {{ transferring() ? 'Moving...' : 'Bring aircraft' }}
            </button>
          </div>
        </div>
      </div>
    }
  `
})
export class Fleet implements OnInit {
  private readonly api = inject(BookingService);
  private readonly auth = inject(AuthService);

  protected readonly format = formatMinutes;
  protected readonly status = STATUS;

  protected readonly aircraft = signal<FleetAircraft[]>([]);
  protected readonly loading = signal(true);
  protected readonly search = signal('');
  protected readonly typeFilter = signal('');
  protected readonly hereOnly = signal(false);
  protected readonly transferFor = signal<FleetAircraft | null>(null);
  protected readonly quote = signal<TransferQuote | null>(null);
  protected readonly transferring = signal(false);
  protected readonly transferError = signal<string | null>(null);

  protected readonly location = computed(() => this.auth.pilot()?.currentLocationIcao ?? '');
  protected readonly types = computed(() => [...new Set(this.aircraft().map(a => a.typeIcao))].sort());

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const type = this.typeFilter();
    const here = this.hereOnly() ? this.location() : null;

    return this.aircraft().filter(a =>
      (!type || a.typeIcao === type)
      && (!here || a.locationIcao === here)
      && (!term
        || a.registration.toLowerCase().includes(term)
        || a.registration.replace('-', '').toLowerCase().includes(term)
        || a.locationIcao.toLowerCase().includes(term)
        || a.locationCity.toLowerCase().includes(term)));
  });

  async ngOnInit(): Promise<void> {
    try {
      this.aircraft.set(await this.api.fleet());
    } finally {
      this.loading.set(false);
    }
  }

  protected async openTransfer(item: FleetAircraft): Promise<void> {
    this.transferFor.set(item);
    this.quote.set(null);
    this.transferError.set(null);

    try {
      this.quote.set(await this.api.transferQuote(item.id));
    } catch (error) {
      this.transferError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    }
  }

  protected closeTransfer(): void {
    if (!this.transferring()) this.transferFor.set(null);
  }

  protected async transfer(item: FleetAircraft): Promise<void> {
    this.transferring.set(true);
    this.transferError.set(null);

    try {
      const moved = await this.api.transfer(item.id);
      this.aircraft.update(list => list.map(a => (a.id === moved.id ? moved : a)));
      this.transferFor.set(null);
    } catch (error) {
      this.transferError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.transferring.set(false);
    }
  }
}