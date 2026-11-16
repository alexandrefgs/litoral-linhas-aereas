import { Pipe, PipeTransform, inject } from '@angular/core';
import { LanguageService } from '../../core/i18n/language.service';

const LOCALES = { pt: 'pt-BR', en: 'en-US', es: 'es-ES' } as const;

@Pipe({ name: 'localeNumber', pure: false })
export class LocaleNumberPipe implements PipeTransform {
  private readonly language = inject(LanguageService);

  transform(value: number | null | undefined): string {
    return value == null ? '—' : value.toLocaleString(LOCALES[this.language.current()]);
  }
}