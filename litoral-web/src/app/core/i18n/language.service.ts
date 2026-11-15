import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export type Language = 'pt' | 'en' | 'es';

const SUPPORTED: Language[] = ['pt', 'en', 'es'];
const DEFAULT_LANGUAGE: Language = 'pt';
const STORAGE_KEY = 'litoral.language';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly http = inject(HttpClient);
  private readonly translations = signal<Record<string, string>>({});

  readonly current = signal<Language>(DEFAULT_LANGUAGE);
  readonly alternatives = computed(() => SUPPORTED.filter(l => l !== this.current()));

  async init(): Promise<void> {
    await this.use(this.getSaved() ?? this.detect(), false);
  }

  async use(language: Language, persist = true): Promise<void> {
    const data = await firstValueFrom(this.http.get<Record<string, string>>(`i18n/${language}.json`));
    this.translations.set(data);
    this.current.set(language);
    document.documentElement.lang = language === 'pt' ? 'pt-BR' : language;

    if (persist) {
      try {
        localStorage.setItem(STORAGE_KEY, language);
      } catch {}
    }
  }

  translate(key: string): string {
    return this.translations()[key] ?? key;
  }

  private detect(): Language {
    const tags = navigator.languages?.length ? navigator.languages : [navigator.language];

    for (const tag of tags) {
      const code = tag.slice(0, 2).toLowerCase() as Language;
      if (SUPPORTED.includes(code)) return code;
    }

    return DEFAULT_LANGUAGE;
  }

  private getSaved(): Language | null {
    try {
      const saved = localStorage.getItem(STORAGE_KEY) as Language | null;
      return saved && SUPPORTED.includes(saved) ? saved : null;
    } catch {
      return null;
    }
  }
}