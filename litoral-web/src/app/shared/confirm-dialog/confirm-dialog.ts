import { Component, computed, inject } from '@angular/core';
import { ConfirmService, ConfirmTone } from './confirm.service';

const TONES: Record<ConfirmTone, { icon: string; badge: string; button: string }> = {
  danger: { icon: 'ti-alert-triangle', badge: 'bg-danger/15 text-danger', button: 'bg-danger text-white' },
  warning: { icon: 'ti-alert-circle', badge: 'bg-highlight text-on-highlight', button: 'bg-highlight text-on-highlight' },
  success: { icon: 'ti-circle-check', badge: 'bg-onda-500/15 text-onda-600', button: 'bg-onda-500 text-white' },
  primary: { icon: 'ti-help-circle', badge: 'bg-primary/15 text-primary', button: 'bg-primary text-on-primary' }
};

@Component({
  selector: 'app-confirm-dialog',
  host: { '(document:keydown.escape)': 'close(false)' },
  template: `
    @if (request(); as r) {
      <div class="fixed inset-0 z-[1300] flex items-center justify-center bg-black/60 p-4" (click)="close(false)">
        <div class="panel flex w-full max-w-md flex-col overflow-hidden shadow-2xl" (click)="$event.stopPropagation()" role="alertdialog" aria-modal="true">
          <div class="flex flex-col items-center gap-3 px-6 pb-2 pt-6 text-center">
            <span class="flex h-14 w-14 items-center justify-center rounded-full text-3xl" [class]="tone().badge">
              <i class="ti {{ r.icon ?? tone().icon }}" aria-hidden="true"></i>
            </span>
            <h2 class="text-xl font-bold text-ink">{{ r.title }}</h2>
            @if (r.message) {
              <p class="whitespace-pre-line text-sm text-ink-muted">{{ r.message }}</p>
            }
          </div>

          @if (r.details?.length) {
            <div class="px-6 py-2">
              <ul class="flex flex-col gap-1.5 rounded-lg bg-surface-alt px-4 py-3 text-sm text-ink">
                @for (item of r.details; track item) {
                  <li class="flex items-start gap-2"><i class="ti ti-point-filled mt-0.5 text-ink-muted" aria-hidden="true"></i>{{ item }}</li>
                }
              </ul>
            </div>
          }

          <div class="mt-4 flex gap-2 border-t border-line px-6 py-4">
            <button type="button" class="btn-secondary flex-1" (click)="close(false)">{{ r.cancelLabel ?? 'Cancel' }}</button>
            <button type="button" class="flex-1 rounded-md px-4 py-2 text-sm font-medium hover:opacity-90" [class]="tone().button"
              (click)="close(true)" autofocus>{{ r.confirmLabel ?? 'Confirm' }}</button>
          </div>
        </div>
      </div>
    }
  `
})
export class ConfirmDialog {
  private readonly service = inject(ConfirmService);

  protected readonly request = this.service.current;
  protected readonly tone = computed(() => TONES[this.request()?.tone ?? 'danger']);

  protected close(result: boolean): void {
    this.service.close(result);
  }
}