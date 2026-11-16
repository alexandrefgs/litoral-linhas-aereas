import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-pilot-avatar',
  host: { class: 'block shrink-0 overflow-hidden' },
  template: `
    @if (photoUrl(); as url) {
      <img [src]="url" [alt]="firstName() + ' ' + lastName()" class="h-full w-full object-cover" />
    } @else {
      <span class="flex h-full w-full items-center justify-center bg-primary font-bold text-on-primary" [style.font-size]="fontSize()">
        {{ initials() }}
      </span>
    }
  `
})
export class PilotAvatar {
  readonly photoUrl = input<string | null>(null);
  readonly firstName = input.required<string>();
  readonly lastName = input.required<string>();
  readonly fontSize = input('1.5rem');

  protected readonly initials = computed(() => `${this.firstName()[0] ?? ''}${this.lastName()[0] ?? ''}`.toUpperCase());
}