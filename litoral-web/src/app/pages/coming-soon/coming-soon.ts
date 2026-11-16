import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'app-coming-soon',
  imports: [RouterLink],
  template: `
    <section class="mx-auto flex max-w-7xl flex-col items-center gap-4 px-6 py-24 text-center">
      <span class="flex h-16 w-16 items-center justify-center rounded-2xl bg-surface-alt text-primary">
        <i class="ti {{ icon }} text-3xl" aria-hidden="true"></i>
      </span>
      <h1 class="text-3xl font-bold text-ink">{{ title }}</h1>
      <p class="max-w-md text-ink-muted">This area is under construction and is coming in the next Litoral updates.</p>
      <a routerLink="/dashboard" class="btn-secondary mt-2">Back to dashboard</a>
    </section>
  `
})
export class ComingSoon {
  private readonly data = inject(ActivatedRoute).snapshot.data;

  protected readonly title: string = this.data['title'];
  protected readonly icon: string = this.data['icon'];
}