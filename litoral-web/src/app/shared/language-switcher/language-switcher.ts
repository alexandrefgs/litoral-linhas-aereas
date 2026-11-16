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
          [attr.aria-label]="labels[lang]"
          [title]="labels[lang]"
          class="flex h-9 w-11 items-center justify-center rounded-md border border-line hover:bg-surface-alt">
          @switch (lang) {
            @case ('pt') {
              <svg viewBox="0 0 30 21" class="h-[18px] w-[26px] rounded-[3px]" aria-hidden="true">
                <rect width="30" height="21" fill="#009C3B" />
                <path d="M15 2.5 27 10.5 15 18.5 3 10.5Z" fill="#FFDF00" />
                <circle cx="15" cy="10.5" r="5" fill="#002776" />
                <path d="M10.2 9.4q4.8-1.4 9.6 1.6" stroke="#FFFFFF" stroke-width="0.9" fill="none" />
              </svg>
            }
            @case ('en') {
              <svg viewBox="0 0 30 21" class="h-[18px] w-[26px] rounded-[3px]" aria-hidden="true">
                <rect width="30" height="21" fill="#FFFFFF" />
                <g fill="#B22234">
                  <rect y="0" width="30" height="1.62" />
                  <rect y="3.23" width="30" height="1.62" />
                  <rect y="6.46" width="30" height="1.62" />
                  <rect y="9.69" width="30" height="1.62" />
                  <rect y="12.92" width="30" height="1.62" />
                  <rect y="16.15" width="30" height="1.62" />
                  <rect y="19.38" width="30" height="1.62" />
                </g>
                <rect width="12" height="11.31" fill="#3C3B6E" />
              </svg>
            }
            @case ('es') {
              <svg viewBox="0 0 30 21" class="h-[18px] w-[26px] rounded-[3px]" aria-hidden="true">
                <rect width="30" height="21" fill="#AA151B" />
                <rect y="5.25" width="30" height="10.5" fill="#F1BF00" />
              </svg>
            }
          }
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