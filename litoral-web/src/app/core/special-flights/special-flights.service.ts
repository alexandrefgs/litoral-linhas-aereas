import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { BookingService } from '../booking/booking.service';
import { PilotSpecialFlight } from './special-flights.models';

const BASE_URL = '/api/special-flights';

@Injectable({ providedIn: 'root' })
export class SpecialFlightsService {
  private readonly http = inject(HttpClient);
  private readonly booking = inject(BookingService);

  list(): Promise<PilotSpecialFlight[]> {
    return firstValueFrom(this.http.get<PilotSpecialFlight[]>(BASE_URL));
  }

  async take(id: string): Promise<PilotSpecialFlight> {
    const result = await firstValueFrom(this.http.post<PilotSpecialFlight>(`${BASE_URL}/${id}/take`, {}));
    await this.booking.refreshCurrent().catch(() => undefined);
    return result;
  }

  async bookNextLeg(id: string): Promise<PilotSpecialFlight> {
    const result = await firstValueFrom(this.http.post<PilotSpecialFlight>(`${BASE_URL}/${id}/next-leg`, {}));
    await this.booking.refreshCurrent().catch(() => undefined);
    return result;
  }
}