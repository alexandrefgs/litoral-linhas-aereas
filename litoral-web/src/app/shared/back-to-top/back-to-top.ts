import { Component, signal } from '@angular/core';

@Component({
  selector: 'app-back-to-top',
  host: { '(window:scroll)': 'onScroll()' },
  template: `
    <button type="button" title="Back to top" aria-label="Back to top" (click)="toTop()"
      class="fixed bottom-6 right-6 z-40 flex h-12 w-12 items-center justify-center rounded-full bg-primary text-2xl text-on-primary shadow-lg transition duration-300 hover:-translate-y-0.5 hover:bg-primary-hover"
      [class]="visible() ? 'translate-y-0 opacity-100' : 'pointer-events-none translate-y-4 opacity-0'">
      <i class="ti ti-arrow-up" aria-hidden="true"></i>
    </button>
  `
})
export class BackToTop {
  protected readonly visible = signal(false);

  protected onScroll(): void {
    this.visible.set(window.scrollY > 400);
  }

  protected toTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
}