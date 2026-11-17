import { Component, inject, input, isDevMode, signal } from '@angular/core';
import { Router } from '@angular/router';
import { apiErrorMessage } from '../../core/api/api-error';
import { Booking } from '../../core/booking/booking.models';
import { SimulationScenario } from '../../core/pirep/pirep.models';
import { PirepService } from '../../core/pirep/pirep.service';
import { ManualPirepModal } from './manual-pirep-modal';

@Component({
  selector: 'app-flight-actions',
  imports: [ManualPirepModal],
  template: `
    <div class="panel flex flex-wrap items-center justify-between gap-4 p-6">
      <div>
        <h2 class="text-lg font-bold text-ink">Flight report</h2>
        <p class="text-sm text-ink-muted">The ACARS files the PIREP when you finish the flight. If it fails, file it manually.</p>
      </div>
      <div class="flex flex-wrap items-center gap-2">
        @if (devMode) {
          <select class="field-input w-auto py-2 text-sm" [value]="scenario()" (change)="scenario.set($any($event.target).value)"
            aria-label="Simulation scenario">
            <option value="clean">Clean flight</option>
            <option value="violations">With penalties</option>
            <option value="rejected">Rejected</option>
          </select>
          <button type="button" class="btn-secondary gap-2" [disabled]="busy()" (click)="simulate()">
            <i class="ti ti-player-play" aria-hidden="true"></i>{{ busy() ? 'Simulating...' : 'Simulate flight (dev)' }}
          </button>
        }
        <button type="button" class="btn-primary gap-2" [disabled]="busy()" (click)="modalOpen.set(true)">
          <i class="ti ti-clipboard-text" aria-hidden="true"></i>File PIREP manually
        </button>
      </div>
      @if (error(); as message) {
        <p class="w-full rounded-lg bg-danger/10 px-4 py-3 text-sm text-danger">{{ message }}</p>
      }
    </div>

    @if (modalOpen()) {
      <app-manual-pirep-modal [booking]="booking()" (filed)="open($event)" (closed)="modalOpen.set(false)" />
    }
  `
})
export class FlightActions {
  private readonly api = inject(PirepService);
  private readonly router = inject(Router);

  readonly booking = input.required<Booking>();

  protected readonly devMode = isDevMode();
  protected readonly scenario = signal<SimulationScenario>('clean');
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly modalOpen = signal(false);

  protected async simulate(): Promise<void> {
    this.busy.set(true);
    this.error.set(null);

    try {
      const pirep = await this.api.simulate(this.scenario());
      this.open(pirep.summary.id);
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Could not simulate the flight.');
    } finally {
      this.busy.set(false);
    }
  }

  protected open(id: string): void {
    this.modalOpen.set(false);
    this.router.navigate(['/flights', id]);
  }
}