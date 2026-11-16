import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'localeNumber' })
export class LocaleNumberPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    return value == null ? '—' : value.toLocaleString('en-US');
  }
}