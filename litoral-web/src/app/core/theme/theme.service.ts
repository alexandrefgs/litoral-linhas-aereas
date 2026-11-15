import { Injectable, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

const STORAGE_KEY = 'litoral.theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly current = signal<Theme>(this.getSaved());

  toggle(): void {
    this.set(this.current() === 'light' ? 'dark' : 'light');
  }

  set(theme: Theme): void {
    this.current.set(theme);
    document.documentElement.classList.toggle('dark', theme === 'dark');

    try {
      localStorage.setItem(STORAGE_KEY, theme);
    } catch {}
  }

  private getSaved(): Theme {
    try {
      return localStorage.getItem(STORAGE_KEY) === 'dark' ? 'dark' : 'light';
    } catch {
      return 'light';
    }
  }
}