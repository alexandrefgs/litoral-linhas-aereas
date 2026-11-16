import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-airport-modal',
  host: { '(document:keydown.escape)': 'closed.emit()' },
  template: `
    <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/60 p-4" (click)="closed.emit()">
      <div class="panel flex max-h-[90vh] w-full flex-col overflow-hidden shadow-2xl" [class]="width()" (click)="$event.stopPropagation()">
        @if (title()) {
          <div class="flex items-center justify-between gap-4 border-b border-line px-6 py-4">
            <h2 class="text-lg font-semibold text-ink">{{ title() }}</h2>
            <button type="button" class="icon-btn h-9 w-9 text-xl" aria-label="Close" (click)="closed.emit()">
              <i class="ti ti-x" aria-hidden="true"></i>
            </button>
          </div>
        }
        <div class="flex-1 overflow-y-auto">
          <ng-content />
        </div>
      </div>
    </div>
  `
})
export class AirportModal {
  readonly title = input('');
  readonly width = input('max-w-3xl');
  readonly closed = output<void>();
}