import { Component, OnInit, inject, signal } from '@angular/core';
import { AdminAircraftType, AdminFleetService } from '../../../core/admin/admin-fleet.service';
import { apiErrorMessage } from '../../../core/api/api-error';
import { AircraftTypeFormModal } from './aircraft-type-form-modal';

@Component({
  selector: 'app-fleet-types',
  imports: [AircraftTypeFormModal],
  template: `
    <div class="flex flex-col gap-4">
      <div class="flex justify-end">
        <button type="button" class="btn-primary gap-2 px-4 py-2.5" (click)="openCreate()" [disabled]="loading()">
          <i class="ti ti-plus text-lg" aria-hidden="true"></i>New type
        </button>
      </div>

      <div class="panel overflow-x-auto">
        @if (loading()) {
          <p class="px-6 py-12 text-center text-ink-muted">Loading...</p>
        } @else if (loadError()) {
          <div class="flex flex-col items-center gap-3 px-6 py-12 text-center">
            <p class="text-danger">Something went wrong. Try again.</p>
            <button type="button" class="btn-secondary" (click)="load()">Try again</button>
          </div>
        } @else if (types().length === 0) {
          <p class="px-6 py-12 text-center text-ink-muted">No aircraft types yet.</p>
        } @else {
          <table class="w-full text-left text-sm">
            <thead class="border-b border-line text-xs uppercase tracking-wide text-ink-muted">
              <tr>
                <th class="px-4 py-3 font-semibold">Type</th>
                <th class="px-4 py-3 font-semibold">Operation</th>
                <th class="px-4 py-3 font-semibold">Range</th>
                <th class="px-4 py-3 font-semibold">Maintenance</th>
                <th class="px-4 py-3 font-semibold">In use</th>
                <th class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody>
              @for (type of types(); track type.id) {
                <tr class="border-b border-line last:border-0 hover:bg-surface-alt/50">
                  <td class="px-4 py-3">
                    <p class="font-mono font-bold text-ink">{{ type.icaoCode }}</p>
                    <p class="text-xs text-ink-muted">{{ type.name }}</p>
                  </td>
                  <td class="px-4 py-3">
                    @if (type.isCargo) {
                      <span class="rounded-full bg-accent/20 px-2.5 py-1 text-xs font-semibold text-ink">Cargo</span>
                    } @else {
                      <span class="rounded-full bg-primary/10 px-2.5 py-1 text-xs font-semibold text-primary">Passenger</span>
                    }
                  </td>
                  <td class="px-4 py-3 text-ink">{{ type.maxRangeNm }} NM</td>
                  <td class="px-4 py-3 text-ink">every {{ type.maintenanceIntervalHours }}h</td>
                  <td class="px-4 py-3 text-ink-muted">{{ type.aircraftCount }} aircraft · {{ type.routeCount }} routes</td>
                  <td class="px-4 py-3">
                    @if (confirmDelete() === type.id) {
                      <div class="flex items-center justify-end gap-2">
                        <span class="text-xs text-ink-muted">Delete this type?</span>
                        <button type="button" class="rounded-md bg-danger px-3 py-1 text-xs font-medium text-white hover:opacity-90"
                          (click)="remove(type)">Delete</button>
                        <button type="button" class="btn-secondary px-3 py-1 text-xs" (click)="confirmDelete.set(null)">Cancel</button>
                      </div>
                    } @else {
                      <div class="flex justify-end gap-1">
                        <button type="button" class="icon-btn h-9 w-9 text-lg" aria-label="Edit type" (click)="openEdit(type)">
                          <i class="ti ti-pencil" aria-hidden="true"></i>
                        </button>
                        <button type="button" class="icon-btn h-9 w-9 text-lg hover:text-danger" aria-label="Delete type"
                          (click)="askDelete(type.id)">
                          <i class="ti ti-trash" aria-hidden="true"></i>
                        </button>
                      </div>
                    }
                    @if (rowError()?.id === type.id) {
                      <p class="mt-1 text-right text-xs text-danger">{{ rowError()?.message }}</p>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        }
      </div>
    </div>

    @if (modalOpen()) {
      <app-aircraft-type-form-modal [type]="editing()" (saved)="onSaved($event)" (closed)="modalOpen.set(false)" />
    }
  `
})
export class FleetTypes implements OnInit {
  private readonly api = inject(AdminFleetService);

  protected readonly types = signal<AdminAircraftType[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly modalOpen = signal(false);
  protected readonly editing = signal<AdminAircraftType | null>(null);
  protected readonly confirmDelete = signal<string | null>(null);
  protected readonly rowError = signal<{ id: string; message: string } | null>(null);

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);

    try {
      this.types.set(await this.api.types());
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected openCreate(): void {
    this.editing.set(null);
    this.modalOpen.set(true);
  }

  protected openEdit(type: AdminAircraftType): void {
    this.editing.set(type);
    this.modalOpen.set(true);
  }

  protected onSaved(type: AdminAircraftType): void {
    this.types.update(list =>
      [...list.filter(t => t.id !== type.id), type].sort((a, b) => Number(a.isCargo) - Number(b.isCargo) || a.icaoCode.localeCompare(b.icaoCode))
    );
    this.modalOpen.set(false);
  }

  protected askDelete(id: string): void {
    this.rowError.set(null);
    this.confirmDelete.set(id);
  }

  protected async remove(type: AdminAircraftType): Promise<void> {
    try {
      await this.api.removeType(type.id);
      this.types.update(list => list.filter(t => t.id !== type.id));
      this.confirmDelete.set(null);
    } catch (error) {
      this.confirmDelete.set(null);
      this.rowError.set({ id: type.id, message: apiErrorMessage(error) ?? 'Something went wrong. Try again.' });
    }
  }
}