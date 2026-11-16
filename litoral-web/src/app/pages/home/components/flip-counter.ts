import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-flip-counter',
  template: `
    <div class="flex gap-1.5" [attr.aria-label]="value()">
      @for (digit of digits(); track $index) {
        <span class="relative flex h-16 w-12 items-center justify-center overflow-hidden rounded-md bg-gradient-to-b from-[#1b2b45] to-[#0d1829] text-4xl font-bold text-white shadow-lg ring-1 ring-white/10" aria-hidden="true">
          {{ digit }}
          <span class="absolute inset-x-0 top-1/2 h-px bg-black/60"></span>
        </span>
      }
    </div>
  `
})
export class FlipCounter {
  readonly value = input.required<number>();
  readonly minDigits = input(3);

  protected readonly digits = computed(() =>
    Math.max(0, Math.floor(this.value())).toString().padStart(this.minDigits(), '0').split('')
  );
}