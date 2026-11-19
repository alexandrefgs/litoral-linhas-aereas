import { Component } from '@angular/core';

@Component({
  selector: 'app-aircraft-illustration',
  host: { class: 'block' },
  template: `
    <svg viewBox="0 0 400 150" class="h-auto w-full" role="img" aria-label="Litoral aircraft">
      <path d="M30 92 Q20 92 22 84 Q30 72 70 70 L330 70 Q350 70 362 60 L388 22 L398 22 L392 70 Q392 92 370 96 L60 100 Q34 100 30 92 Z" fill="#FFFFFF" />
      <path d="M340 70 L366 22 L396 22 L392 70 Z" fill="#1D6FD8" />
      <path d="M346 66 Q366 46 394 40 L393 50 Q370 54 352 70 Z" fill="#2EC4B6" />
      <path d="M366 22 L396 22 L395 28 L363 28 Z" fill="#FFD166" />
      <path d="M24 90 Q120 96 200 88 Q290 78 368 80 L366 86 Q290 84 200 94 Q120 102 30 96 Z" fill="#2EC4B6" />
      <rect x="150" y="96" width="70" height="16" rx="6" fill="#E3E7EE" />
      <rect x="150" y="104" width="22" height="8" rx="3" fill="#1D6FD8" />
      <g fill="#0A1F3C" opacity="0.75">
        <rect x="58" y="76" width="6" height="5" rx="1" />
        @for (x of windows; track x) {
          <rect [attr.x]="x" y="76" width="4" height="4" rx="1" />
        }
      </g>
      <text x="128" y="82" font-size="13" font-weight="800" fill="#1D6FD8" font-family="inherit">LITORAL</text>
      <path d="M28 82 Q34 76 44 76 L46 82 Z" fill="#0A1F3C" />
    </svg>
  `
})
export class AircraftIllustration {
  protected readonly windows = [80, 90, 100, 110, 230, 240, 250, 260, 270, 280, 290, 300];
}