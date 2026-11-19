import { DatePipe } from '@angular/common';
import { Component, effect, inject, input, signal, untracked } from '@angular/core';
import { DownloadItem } from '../../core/downloads/downloads.models';
import { DownloadsService } from '../../core/downloads/downloads.service';
import { MEL_VERIFICATION_LABEL, PilotMelItem, melLabel } from '../../core/mel/mel.models';
import { MelService } from '../../core/mel/mel.service';

type Tab = 'mel' | 'docs';

@Component({
  selector: 'app-tech-log',
  imports: [DatePipe],
  template: `
    <div class="flex flex-col gap-3 border-t border-line pt-3">
      <div class="flex rounded-lg border border-line p-0.5 text-xs font-semibold">
        <button type="button" class="flex flex-1 items-center justify-center gap-1.5 rounded-md px-2 py-1.5"
          [class]="tab() === 'mel' ? 'bg-primary text-on-primary' : 'text-ink-muted hover:text-ink'" (click)="tab.set('mel')">
          <i class="ti ti-tool" aria-hidden="true"></i>Tech log{{ items().length ? ' (' + items().length + ')' : '' }}
        </button>
        <button type="button" class="flex flex-1 items-center justify-center gap-1.5 rounded-md px-2 py-1.5"
          [class]="tab() === 'docs' ? 'bg-primary text-on-primary' : 'text-ink-muted hover:text-ink'" (click)="tab.set('docs')">
          <i class="ti ti-books" aria-hidden="true"></i>Crew Docs
        </button>
      </div>

      @if (tab() === 'mel') {
        @for (m of items(); track m.code) {
          <div class="rounded-lg border border-highlight/60 bg-highlight/10 p-3 text-sm">
            <p class="flex flex-wrap items-center gap-2 font-semibold text-ink">
              <span class="font-mono text-xs">{{ label(m) }}</span>{{ m.title }}
            </p>
            <p class="mt-1 text-ink"><span class="font-semibold">Restriction:</span> {{ m.restriction }}</p>
            <p class="mt-1 text-ink-muted"><span class="font-semibold text-ink">Procedure:</span> {{ m.procedure }}</p>
            <p class="mt-1 text-[11px] text-ink-muted">Cat {{ m.category }} · due {{ m.dueAt | date: 'd MMM' : 'UTC' }} · {{ verification[m.verification] }}</p>
          </div>
        } @empty {
          <p class="flex items-center gap-2 rounded-lg bg-surface-alt px-3 py-2 text-sm text-ink-muted">
            <i class="ti ti-circle-check text-onda-500" aria-hidden="true"></i>No open MEL or CDL items.
          </p>
        }
      } @else {
        @for (d of docs(); track d.id) {
          <button type="button" class="flex items-center gap-3 rounded-lg bg-surface-alt px-3 py-2 text-left text-sm hover:bg-primary/10" (click)="open(d)">
            <i class="ti {{ d.isFile ? 'ti-file-text' : 'ti-external-link' }} text-lg text-primary" aria-hidden="true"></i>
            <span class="min-w-0 flex-1">
              <span class="block truncate font-semibold text-ink">{{ d.title }}</span>
              <span class="text-xs text-ink-muted">{{ d.aircraftType ?? 'All types' }}{{ d.version ? ' · v' + d.version : '' }}</span>
            </span>
          </button>
        } @empty {
          <p class="rounded-lg bg-surface-alt px-3 py-2 text-sm text-ink-muted">No crew documents for this type yet.</p>
        }
      }
    </div>
  `
})
export class TechLog {
  private readonly mel = inject(MelService);
  private readonly downloads = inject(DownloadsService);

  readonly aircraftId = input.required<string>();
  readonly typeIcao = input.required<string>();

  protected readonly verification = MEL_VERIFICATION_LABEL;
  protected readonly label = melLabel;
  protected readonly tab = signal<Tab>('mel');
  protected readonly items = signal<PilotMelItem[]>([]);
  protected readonly docs = signal<DownloadItem[]>([]);

  constructor() {
    effect(() => {
      const id = this.aircraftId();
      const type = this.typeIcao();
      untracked(() => this.load(id, type));
    });
  }

  protected async open(item: DownloadItem): Promise<void> {
    try {
      await this.downloads.download(item);
    } catch {}
  }

  private async load(id: string, type: string): Promise<void> {
    try {
      const [mel, docs] = await Promise.all([this.mel.forAircraft([id]), this.downloads.list()]);
      this.items.set(mel[0]?.items ?? []);
      this.docs.set(docs.filter(d => d.category === 'Documents' && (!d.aircraftType || d.aircraftType === type)));
    } catch {}
  }
}