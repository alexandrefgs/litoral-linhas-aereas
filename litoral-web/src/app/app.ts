import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslatePipe } from './core/i18n/translate.pipe';
import { LanguageSwitcher } from './shared/language-switcher/language-switcher';
import { ThemeToggle } from './shared/theme-toggle/theme-toggle';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, TranslatePipe, LanguageSwitcher, ThemeToggle],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {}