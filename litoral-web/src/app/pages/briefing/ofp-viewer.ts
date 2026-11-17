import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { apiErrorMessage } from '../../core/api/api-error';
import { BookingService } from '../../core/booking/booking.service';

@Component({
  selector: 'app-ofp-viewer',
  host: { class: 'panel flex flex-col overflow-hidden' },
  template: `
    <div class="flex flex-wrap items-center justify-between gap-3 border-b border-line px-6 py-4">
      <div>
        <h2 class="text-lg font-bold text-ink">OFP</h2>
        <p class="text-xs text-ink-muted">{{ subtitle() }}</p>
      </div>
      @if (hasPdf()) {
        <div class="flex gap-2">
          <button type="button" class="btn-secondary gap-2 px-3 py-1.5 text-sm" [disabled]="busy()" (click)="download()">
            <i class="ti ti-download" aria-hidden="true"></i>Download PDF
          </button>
          <button type="button" class="btn-secondary gap-2 px-3 py-1.5 text-sm" [disabled]="busy()" (click)="openTab()">
            <i class="ti ti-external-link" aria-hidden="true"></i>New tab
          </button>
        </div>
      }
    </div>

    @if (error(); as message) {
      <p class="border-b border-line bg-danger/10 px-6 py-3 text-sm text-danger">{{ message }}</p>
    }

    @if (planHtml(); as html) {
      <div class="overflow-x-auto px-6 py-5">
        <div class="ofp-text mx-auto w-fit font-mono text-[12.5px] leading-[1.35] text-ink" [innerHTML]="html"></div>
      </div>
    } @else {
      <p class="px-6 py-16 text-center text-ink-muted">This OFP has no text version. Use the PDF.</p>
    }
  `,
  styles: `
    :host ::ng-deep .ofp-text pre { margin: 0; font: inherit; white-space: pre; }
    :host ::ng-deep .ofp-text b { color: var(--color-primary); font-weight: 700; }
    :host ::ng-deep .ofp-text h2 { height: 0; margin: 1.25rem 0; border-top: 1px dashed var(--color-line); }
    :host ::ng-deep .ofp-text img { display: none; }
  `
})
export class OfpViewer {
  private readonly api = inject(BookingService);

  readonly planHtml = input<string | null>(null);
  readonly hasPdf = input(false);
  readonly callsign = input.required<string>();
  readonly subtitle = input('');

  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async download(): Promise<void> {
    const url = await this.pdfUrl();
    if (!url) return;
    const link = document.createElement('a');
    link.href = url;
    link.download = `${this.callsign()}-OFP.pdf`;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  }

  protected async openTab(): Promise<void> {
    const tab = window.open('', '_blank');
    const url = await this.pdfUrl();
    if (!url) {
      tab?.close();
      return;
    }
    if (tab) tab.location.href = url;
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  }

  private async pdfUrl(): Promise<string | null> {
    this.busy.set(true);
    this.error.set(null);
    try {
      const blob = await this.api.simBriefPdf();
      return URL.createObjectURL(new Blob([blob], { type: 'application/pdf' }));
    } catch (error) {
      this.error.set((await this.blobErrorMessage(error)) ?? apiErrorMessage(error) ?? 'Could not load the OFP PDF.');
      return null;
    } finally {
      this.busy.set(false);
    }
  }

  private async blobErrorMessage(error: unknown): Promise<string | null> {
    if (!(error instanceof HttpErrorResponse) || !(error.error instanceof Blob) || error.status >= 500) return null;
    try {
      const body = JSON.parse(await error.error.text());
      return typeof body?.title === 'string' ? body.title : null;
    } catch {
      return null;
    }
  }
}