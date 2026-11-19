import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { apiErrorMessage } from '../../../core/api/api-error';
import { AdminDownloadItem, DOWNLOAD_CATEGORIES, formatBytes } from '../../../core/downloads/downloads.models';
import { DownloadsService } from '../../../core/downloads/downloads.service';
import { ConfirmService } from '../../../shared/confirm-dialog/confirm.service';
import { DownloadFormModal } from './download-form-modal';

@Component({
  selector: 'app-admin-downloads',
  imports: [DatePipe, DownloadFormModal],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-ink">Downloads</h1>
          <p class="text-ink-muted">Liveries, documents and software for the pilots. Only signed-in members can download them.</p>
        </div>
        <button type="button" class="btn-primary flex items-center gap-2" (click)="editing.set({ item: null })">
          <i class="ti ti-plus" aria-hidden="true"></i>New download
        </button>
      </div>

      @if (error(); as message) {
        <p class="rounded-lg bg-danger/10 px-4 py-3 text-sm text-danger">{{ message }}</p>
      }

      @if (loading()) {
        <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (items().length === 0) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-12 text-center">
          <i class="ti ti-download text-4xl text-ink-muted" aria-hidden="true"></i>
          <p class="text-ink-muted">No downloads yet.</p>
        </div>
      } @else {
        @for (group of groups(); track group.value) {
          <section class="flex flex-col gap-2">
            <h2 class="flex items-center gap-2 text-sm font-semibold uppercase tracking-wide text-ink-muted">
              <i class="ti {{ group.icon }} text-lg" aria-hidden="true"></i>{{ group.label }}
            </h2>
            <div class="panel divide-y divide-line">
              @for (item of group.items; track item.id) {
                <div class="flex flex-col gap-3 px-5 py-4 sm:flex-row sm:items-center">
                  <div class="min-w-0 flex-1">
                    <p class="flex flex-wrap items-center gap-2 font-semibold text-ink">
                      {{ item.title }}
                      @if (item.aircraftType) {
                        <span class="rounded bg-surface-alt px-1.5 py-0.5 font-mono text-xs text-ink-muted">{{ item.aircraftType }}</span>
                      }
                      @if (item.version) {
                        <span class="rounded bg-surface-alt px-1.5 py-0.5 text-xs text-ink-muted">v{{ item.version }}</span>
                      }
                    </p>
                    <p class="mt-1 flex flex-wrap gap-x-3 text-xs text-ink-muted">
                      @if (item.isFile) {
                        <span class="flex items-center gap-1"><i class="ti ti-file" aria-hidden="true"></i>{{ item.fileName }} · {{ size(item.sizeBytes) }}</span>
                      } @else {
                        <span class="flex max-w-xs items-center gap-1 truncate"><i class="ti ti-link" aria-hidden="true"></i>{{ item.externalUrl }}</span>
                      }
                      <span>{{ item.downloads }} {{ item.downloads === 1 ? 'download' : 'downloads' }}</span>
                      <span>Updated {{ item.updatedAt | date: 'd MMM y' }} by {{ item.updatedBy }}</span>
                    </p>
                  </div>
                  <div class="flex shrink-0 gap-1">
                    <button type="button" class="icon-btn h-9 w-9 text-lg" title="Edit" aria-label="Edit" (click)="editing.set({ item })">
                      <i class="ti ti-pencil" aria-hidden="true"></i>
                    </button>
                    <button type="button" class="icon-btn h-9 w-9 text-lg hover:text-danger" title="Delete" aria-label="Delete" (click)="remove(item)">
                      <i class="ti ti-trash" aria-hidden="true"></i>
                    </button>
                  </div>
                </div>
              }
            </div>
          </section>
        }
      }
    </div>

    @if (editing(); as state) {
      <app-download-form-modal [item]="state.item" (saved)="onSaved($event)" (closed)="editing.set(null)" />
    }
  `
})
export class AdminDownloads implements OnInit {
  private readonly api = inject(DownloadsService);
  private readonly confirm = inject(ConfirmService);

  protected readonly items = signal<AdminDownloadItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly editing = signal<{ item: AdminDownloadItem | null } | null>(null);

  protected readonly groups = computed(() =>
    DOWNLOAD_CATEGORIES
      .map(c => ({ ...c, items: this.items().filter(i => i.category === c.value) }))
      .filter(g => g.items.length > 0));

  async ngOnInit(): Promise<void> {
    try {
      this.items.set(await this.api.adminList());
    } catch {
      this.error.set('Something went wrong. Reload the page.');
    } finally {
      this.loading.set(false);
    }
  }

  protected size(bytes: number): string {
    return formatBytes(bytes);
  }

  protected onSaved(item: AdminDownloadItem): void {
    this.items.update(list => list.some(i => i.id === item.id) ? list.map(i => i.id === item.id ? item : i) : [...list, item]);
    this.editing.set(null);
  }

  protected async remove(item: AdminDownloadItem): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Delete download?',
      message: `"${item.title}" will be removed for every pilot.${item.isFile ? ' The file is deleted from the server.' : ''}`,
      confirmLabel: 'Delete',
      tone: 'danger',
      icon: 'ti-trash'
    });
    if (!ok) return;

    this.error.set(null);
    try {
      await this.api.delete(item.id);
      this.items.update(list => list.filter(i => i.id !== item.id));
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    }
  }
}