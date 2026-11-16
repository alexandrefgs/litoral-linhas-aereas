import { Injectable, computed, effect, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark' | 'system';

const STORAGE_KEY = 'litoral.theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly media = window.matchMedia('(prefers-color-scheme: dark)');
  private readonly systemDark = signal(this.media.matches);

  readonly mode = signal<ThemeMode>(this.getSaved());
  readonly isDark = computed(() => this.mode() === 'dark' || (this.mode() === 'system' && this.systemDark()));

  constructor() {
    this.media.addEventListener('change', event => this.systemDark.set(event.matches));
    effect(() => document.documentElement.classList.toggle('dark', this.isDark()));
  }

  setMode(mode: ThemeMode): void {
    this.mode.set(mode);

    try {
      localStorage.setItem(STORAGE_KEY, mode);
    } catch {}
  }

  private getSaved(): ThemeMode {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      return saved === 'dark' || saved === 'system' ? saved : 'light';
    } catch {
      return 'light';
    }
  }
}