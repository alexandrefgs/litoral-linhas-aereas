import { Component, inject } from '@angular/core';
import { Language, LanguageService } from '../../core/i18n/language.service';

@Component({
  selector: 'app-language-switcher',
  template: `
    <div class="flex gap-2">
      @for (lang of language.alternatives(); track lang) {
        <button
          type="button"
          (click)="language.use(lang)"
            class="rounded-md border border-line px-3 py-1 text-sm text-ink hover:bg-surface-alt">
          {{ labels[lang] }}
        </button>
      }
    </div>
  `
})
export class LanguageSwitcher {
  protected readonly language = inject(LanguageService);
  protected readonly labels: Record<Language, string> = {
    pt: 'Português',
    en: 'English',
    es: 'Español'
  };
}