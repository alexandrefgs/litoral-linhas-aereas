import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { BackToTop } from './shared/back-to-top/back-to-top';
import { Header } from './shared/header/header';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Header, BackToTop],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {}