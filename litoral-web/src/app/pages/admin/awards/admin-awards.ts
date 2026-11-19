import { Component, OnInit, inject, signal } from '@angular/core';
import { apiErrorMessage } from '../../../core/api/api-error';
import { AdminAward } from '../../../core/awards/awards.models';
import { AwardsService } from '../../../core/awards/awards.service';
import { ConfirmService } from '../../../shared/confirm-dialog/confirm.service';
import { AwardFormModal } from './award-form-modal';
import { AwardHoldersModal } from './award-holders-modal';

@Component({
  selector: 'app-admin-awards',
  imports: [AwardFormModal, AwardHoldersModal],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-ink">Awards</h1>
          <p class="text-ink-muted">Create awards and grant them to pilots. Tour awards are created with each tour.</p>
        </div>
        <button type="button" class="btn-primary flex items-center gap-2" (click)="openForm(null)">
          <i class="ti ti-plus" aria-hidden="true"></i>New award
        </button>
      </div>

      @if (error(); as message) {
        <p class="rounded-lg bg-danger/10 px-4 py-3 text-sm text-danger">{{ message }}</p>
      }

      @if (loading()) {
        <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (awards().length === 0) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-12 text-center">
          <i class="ti ti-award text-4xl text-ink-muted" aria-hidden="true"></i>
          <p class="text-ink-muted">No awards yet.</p>
        </div>
      } @else {
        <div class="grid items-start gap-4 md:grid-cols-2 xl:grid-cols-3">
          @for (award of awards(); track award.id) {
            <article class="panel flex flex-col gap-4 p-5" [class.opacity-60]="!award.isActive">
              <div class="flex items-start gap-4">
                <span class="flex h-16 w-16 shrink-0 items-center justify-center overflow-hidden rounded-full bg-gradient-to-br from-sol-200 to-sol-400">
                  @if (award.imageUrl) {
                    <img [src]="award.imageUrl" alt="" class="h-full w-full object-cover" />
                  } @else {
                    <i class="ti {{ award.kind === 'Tour' ? 'ti-route' : 'ti-award' }} text-3xl text-atlantico-900" aria-hidden="true"></i>
                  }
                </span>
                <div class="min-w-0 flex-1">
                  <h3 class="font-bold text-ink">{{ award.name }}</h3>
                  <div class="mt-1 flex flex-wrap gap-1.5 text-[11px] font-semibold">
                    <span class="rounded-full bg-surface-alt px-2 py-0.5 text-ink-muted">{{ award.kind === 'Tour' ? 'Tour' : 'Manual' }}</span>
                    @if (!award.isActive) {
                      <span class="rounded-full bg-danger/10 px-2 py-0.5 text-danger">Inactive</span>
                    }
                  </div>
                </div>
              </div>
              <p class="text-sm text-ink-muted">{{ award.description }}</p>
              <div class="mt-auto flex items-center justify-between gap-2 border-t border-line pt-3">
                <button type="button" class="flex items-center gap-1.5 text-sm font-medium text-primary hover:underline" (click)="holdersOf.set(award)">
                  <i class="ti ti-users" aria-hidden="true"></i>{{ award.holders }} {{ award.holders === 1 ? 'pilot' : 'pilots' }}
                </button>
                <div class="flex gap-1">
                  <button type="button" class="icon-btn h-9 w-9 text-lg" title="Edit" aria-label="Edit" (click)="openForm(award)">
                    <i class="ti ti-pencil" aria-hidden="true"></i>
                  </button>
                  @if (award.kind === 'Manual') {
                    <button type="button" class="icon-btn h-9 w-9 text-lg hover:text-danger" title="Delete" aria-label="Delete" (click)="remove(award)">
                      <i class="ti ti-trash" aria-hidden="true"></i>
                    </button>
                  }
                </div>
              </div>
            </article>
          }
        </div>
      }
    </div>

    @if (editing(); as state) {
      <app-award-form-modal [award]="state.award" (saved)="onSaved($event)" (closed)="editing.set(null)" />
    }
    @if (holdersOf(); as award) {
      <app-award-holders-modal [award]="award" (changed)="onHoldersChanged(award.id, $event)" (closed)="holdersOf.set(null)" />
    }
  `
})
export class AdminAwards implements OnInit {
  private readonly api = inject(AwardsService);
  private readonly confirm = inject(ConfirmService);

  protected readonly awards = signal<AdminAward[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly editing = signal<{ award: AdminAward | null } | null>(null);
  protected readonly holdersOf = signal<AdminAward | null>(null);

  async ngOnInit(): Promise<void> {
    try {
      this.awards.set(await this.api.list());
    } catch {
      this.error.set('Something went wrong. Reload the page.');
    } finally {
      this.loading.set(false);
    }
  }

  protected openForm(award: AdminAward | null): void {
    this.editing.set({ award });
  }

  protected onSaved(award: AdminAward): void {
    this.awards.update(list => list.some(a => a.id === award.id) ? list.map(a => a.id === award.id ? award : a) : [...list, award]);
    this.editing.set(null);
  }

  protected onHoldersChanged(id: string, holders: number): void {
    this.awards.update(list => list.map(a => a.id === id ? { ...a, holders } : a));
  }

  protected async remove(award: AdminAward): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Delete award?',
      message: `"${award.name}" will be deleted. This can't be undone.`,
      confirmLabel: 'Delete',
      tone: 'danger',
      icon: 'ti-trash'
    });
    if (!ok) return;

    this.error.set(null);
    try {
      await this.api.delete(award.id);
      this.awards.update(list => list.filter(a => a.id !== award.id));
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    }
  }
}