import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate.pipe';

@Component({
  selector: 'app-coming-soon',
  imports: [RouterLink, TranslatePipe],
  template: `
    <section class="mx-auto flex max-w-7xl flex-col items-center gap-4 px-6 py-24 text-center">
      <span class="flex h-16 w-16 items-center justify-center rounded-2xl bg-surface-alt text-primary">
        <i class="ti {{ icon }} text-3xl" aria-hidden="true"></i>
      </span>
      <h1 class="text-3xl font-bold text-ink">{{ titleKey | t }}</h1>
      <p class="max-w-md text-ink-muted">{{ 'comingSoon.text' | t }}</p>
      <a routerLink="/dashboard" class="btn-secondary mt-2">{{ 'comingSoon.back' | t }}</a>
    </section>
  `
})
export class ComingSoon {
  private readonly data = inject(ActivatedRoute).snapshot.data;

  protected readonly titleKey: string = this.data['titleKey'];
  protected readonly icon: string = this.data['icon'];
}