import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LEGAL_UPDATED_AT, LegalDocument, LegalLanguage } from './legal.models';
import { PRIVACY } from './privacy.content';
import { REGULATIONS } from './regulations.content';

type LegalKind = 'regulations' | 'privacy';

const DOCUMENTS: Record<LegalKind, Record<LegalLanguage, LegalDocument>> = {
  regulations: REGULATIONS,
  privacy: PRIVACY
};

@Component({
  selector: 'app-legal-page',
  imports: [DatePipe, RouterLink],
  template: `
    @let d = doc();
    <section class="mx-auto flex max-w-6xl flex-col gap-8 px-6 py-10">
      <div class="flex flex-wrap items-end justify-between gap-4 border-b border-line pb-6">
        <div>
          <p class="text-xs font-semibold uppercase tracking-[0.2em] text-primary">Litoral Linhas Aéreas</p>
          <h1 class="mt-1 text-3xl font-extrabold text-ink">{{ d.title }}</h1>
          <p class="mt-1 text-ink-muted">{{ d.subtitle }}</p>
          <p class="mt-2 text-xs text-ink-muted">{{ d.updatedLabel }}: {{ updatedAt | date: 'd MMM y' : 'UTC' }}</p>
        </div>
        <div class="flex rounded-lg border border-line p-0.5 text-sm font-semibold">
          @for (l of languages; track l.value) {
            <button type="button" class="flex items-center gap-2 rounded-md px-3 py-1.5" (click)="setLanguage(l.value)"
              [class]="language() === l.value ? 'bg-primary text-on-primary' : 'text-ink-muted hover:text-ink'">
              <img [src]="l.flag" alt="" class="h-3 w-4 rounded-[2px] object-cover" />{{ l.label }}
            </button>
          }
        </div>
      </div>

      <div class="grid gap-8 lg:grid-cols-[220px_1fr]">
        <nav class="hidden lg:block">
          <ul class="sticky top-36 flex flex-col gap-1 text-sm">
            @for (s of d.sections; track s.id) {
              <li>
                <button type="button" class="w-full rounded-md px-3 py-1.5 text-left text-ink-muted hover:bg-surface-alt hover:text-ink" (click)="scrollTo(s.id)">
                  {{ s.title }}
                </button>
              </li>
            }
          </ul>
        </nav>

        <div class="flex flex-col gap-6">
          @for (s of d.sections; track s.id) {
            <article [id]="s.id" class="panel scroll-mt-36 p-6">
              <h2 class="mb-3 text-lg font-bold text-ink">{{ s.title }}</h2>

              @for (p of s.paragraphs ?? []; track $index) {
                <p class="mb-3 leading-relaxed text-ink">{{ p }}</p>
              }

              @if (s.table; as t) {
                <div class="mb-3 overflow-x-auto">
                  <table class="w-full border border-line text-left text-sm">
                    <thead class="bg-surface-alt text-xs uppercase tracking-wide text-ink-muted">
                      <tr>
                        @for (h of t.head; track h) {
                          <th class="border-b border-line px-4 py-2.5 font-semibold">{{ h }}</th>
                        }
                      </tr>
                    </thead>
                    <tbody>
                      @for (row of t.rows; track $index) {
                        <tr class="border-b border-line last:border-0">
                          @for (cell of row; track $index; let first = $first) {
                            <td class="px-4 py-2.5 align-top" [class]="first ? 'whitespace-nowrap font-semibold text-ink' : 'text-ink'">{{ cell }}</td>
                          }
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
              }

              @if (s.items?.length) {
                <ul class="mb-3 flex flex-col gap-2">
                  @for (item of s.items; track $index) {
                    <li class="flex gap-3 leading-relaxed text-ink">
                      <span class="mt-2.5 h-1.5 w-1.5 shrink-0 rounded-full bg-primary" aria-hidden="true"></span><span>{{ item }}</span>
                    </li>
                  }
                </ul>
              }

              @if (s.note) {
                <p class="flex gap-2 rounded-lg bg-surface-alt px-4 py-3 text-sm text-ink-muted">
                  <i class="ti ti-info-circle mt-0.5 shrink-0 text-primary" aria-hidden="true"></i>{{ s.note }}
                </p>
              }
            </article>
          }

          <p class="text-sm text-ink-muted">
            @if (kind() === 'regulations') {
              {{ language() === 'pt' ? 'Veja também a' : 'See also the' }}
              <a routerLink="/privacy" [queryParams]="langParam()" class="font-medium text-primary hover:underline">{{ language() === 'pt' ? 'Política de Privacidade' : 'Privacy Policy' }}</a>.
            } @else {
              {{ language() === 'pt' ? 'Veja também o' : 'See also the' }}
              <a routerLink="/regulations" [queryParams]="langParam()" class="font-medium text-primary hover:underline">{{ language() === 'pt' ? 'Regulamento' : 'Regulations' }}</a>.
            }
          </p>
        </div>
      </div>
    </section>
  `
})
export class LegalPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly updatedAt = LEGAL_UPDATED_AT;
  protected readonly languages: { value: LegalLanguage; label: string; flag: string }[] = [
    { value: 'en', label: 'English', flag: 'https://flagcdn.com/w40/gb.png' },
    { value: 'pt', label: 'Português', flag: 'https://flagcdn.com/w40/br.png' }
  ];

  protected readonly kind = signal<LegalKind>(this.route.snapshot.data['kind'] ?? 'regulations');
  protected readonly language = signal<LegalLanguage>(this.route.snapshot.queryParamMap.get('lang') === 'pt' ? 'pt' : 'en');
  protected readonly doc = computed(() => DOCUMENTS[this.kind()][this.language()]);
  protected readonly langParam = computed(() => (this.language() === 'pt' ? { lang: 'pt' } : {}));

  protected setLanguage(value: LegalLanguage): void {
    this.language.set(value);
    this.router.navigate([], { queryParams: { lang: value === 'pt' ? 'pt' : null }, replaceUrl: true });
  }

  protected scrollTo(id: string): void {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
}