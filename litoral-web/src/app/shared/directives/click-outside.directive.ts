import { Directive, ElementRef, inject, output } from '@angular/core';

@Directive({
  selector: '[appClickOutside]',
  host: { '(document:click)': 'onDocumentClick($event)' }
})
export class ClickOutsideDirective {
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly appClickOutside = output<void>();

  protected onDocumentClick(event: MouseEvent): void {
    if (!this.element.nativeElement.contains(event.target as Node)) {
      this.appClickOutside.emit();
    }
  }
}