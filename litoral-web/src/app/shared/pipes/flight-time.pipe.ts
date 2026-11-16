import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'flightTime' })
export class FlightTimePipe implements PipeTransform {
  transform(totalMinutes: number | null | undefined): string {
    const minutes = Math.max(0, Math.floor(totalMinutes ?? 0));
    const hours = Math.floor(minutes / 60);
    const rest = minutes % 60;
    return `${hours}h ${rest.toString().padStart(2, '0')}m`;
  }
}