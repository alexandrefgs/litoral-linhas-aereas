import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DOWNLOAD_CATEGORIES, DownloadCategory, DownloadItem, formatBytes } from '../../core/downloads/downloads.models';
import { DownloadsService } from '../../core/downloads/downloads.service';

@Component({
  selector: 'app-profile-downloads',
  imports: [DatePipe],
  template: `
    <div class="flex flex-col gap-4">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div class="flex flex-wrap gap-2">
          <button type="button" class="rounded-full px-3 py-1.5 text-sm font-medium"
            [class]="category() === null ? 'bg-primary text-on-primary' : 'border border-line bg-surface text-ink hover:bg-surface-alt'"
            (click)="category.set(null)">All</button>
          @for (c of categories; track c.value) {
            <button type="button" class="flex items-center gap-1.5 rounded-full px-3 py-1.5 text-sm font-medium"
              [class]="category() === c.value ? 'bg-primary text-on-primary' : 'border border-line bg-surface text-ink hover:bg-surface-alt'"
              (click)="category.set(c.value)">
              <i class="ti {{ c.icon }}" aria-hidden="true"></i>{{ c.label }}
            </button>
          }
        </div>
        <input type="search" class="field-input w-full sm:w-64" placeholder="Search downloads"
          [value]="query()" (input)="query.set($any($event.target).value)" />
      </div>

      @if (loading()) {
        <p class="panel px-6 py-16 text-center text-ink-muted">Loading...</p>
      } @else if (failed()) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-16 text-center">
          <p class="text-danger">Something went wrong. Try again.</p>
          <button type="button" class="btn-secondary" (click)="load()">Try again</button>
        </div>
      } @else if (filtered().length === 0) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-16 text-center">
          <i class="ti ti-download text-4xl text-ink-muted" aria-hidden="true"></i>
          <p class="text-ink-muted">{{ items().length === 0 ? 'No downloads available yet.' : 'No downloads match your search.' }}</p>
        </div>
      } @else {
        <div class="panel divide-y divide-line">
          @for (item of filtered(); track item.id) {
            <div class="flex flex-col gap-3 px-5 py-4 sm:flex-row sm:items-center">
              <span class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-2xl text-primary">
                <i class="ti {{ icon(item.category) }}" aria-hidden="true"></i>
              </span>
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
                @if (item.description) {
                  <p class="mt-0.5 text-sm text-ink-muted">{{ item.description }}</p>
                }
                <p class="mt-1 flex flex-wrap gap-x-3 text-xs text-ink-muted">
                  <span>{{ item.isFile ? size(item.sizeBytes) : 'External link' }}</span>
                  <span>Updated {{ item.updatedAt | date: 'd MMM y' }}</span>
                  <span>{{ item.downloads }} {{ item.downloads === 1 ? 'download' : 'downloads' }}</span>
                </p>
              </div>
              <button type="button" class="btn-primary flex shrink-0 items-center gap-2" [disabled]="busy() === item.id" (click)="open(item)">
                <i class="ti {{ item.isFile ? 'ti-download' : 'ti-external-link' }}" aria-hidden="true"></i>
                {{ busy() === item.id ? 'Preparing...' : item.isFile ? 'Download' : 'Open' }}
              </button>
            </div>
          }
        </div>
        @if (error()) {
          <p class="text-sm text-danger">{{ error() }}</p>
        }
      }
      <p class="text-xs text-ink-muted">Downloads are for Litoral members only. Don't share them outside the airline.</p>
    </div>
  `
})
export class ProfileDownloads implements OnInit {
  private readonly api = inject(DownloadsService);

  protected readonly categories = DOWNLOAD_CATEGORIES;
  protected readonly items = signal<DownloadItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly failed = signal(false);
  protected readonly category = signal<DownloadCategory | null>(null);
  protected readonly query = signal('');
  protected readonly busy = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);

  protected readonly filtered = computed(() => {
    const c = this.category();
    const q = this.query().trim().toLowerCase();
    return this.items().filter(i =>
      (c === null || i.category === c)
      && (!q || i.title.toLowerCase().includes(q) || (i.description ?? '').toLowerCase().includes(q) || (i.aircraftType ?? '').toLowerCase().includes(q)));
  });

  ngOnInit(): void {
    this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.failed.set(false);
    try {
      this.items.set(await this.api.list());
    } catch {
      this.failed.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected async open(item: DownloadItem): Promise<void> {
    this.busy.set(item.id);
    this.error.set(null);
    try {
      await this.api.download(item);
      this.items.update(list => list.map(i => i.id === item.id ? { ...i, downloads: i.downloads + 1 } : i));
    } catch {
      this.error.set('The download failed. Try again or let the staff know.');
    } finally {
      this.busy.set(null);
    }
  }

  protected icon(category: DownloadCategory): string {
    return DOWNLOAD_CATEGORIES.find(c => c.value === category)?.icon ?? 'ti-file';
  }

  protected size(bytes: number): string {
    return formatBytes(bytes);
  }
}