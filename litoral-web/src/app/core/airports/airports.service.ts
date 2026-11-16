import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { AirportAircraft, AirportDeparture, AirportDirectory, JumpseatResult } from './airports.models';

const BASE_URL = '/api/airports';

@Injectable({ providedIn: 'root' })
export class AirportsService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  list(): Promise<AirportDirectory> {
    return firstValueFrom(this.http.get<AirportDirectory>(BASE_URL));
  }

  aircraft(icao: string): Promise<AirportAircraft[]> {
    return firstValueFrom(this.http.get<AirportAircraft[]>(`${BASE_URL}/${icao}/aircraft`));
  }

  departures(icao: string): Promise<AirportDeparture[]> {
    return firstValueFrom(this.http.get<AirportDeparture[]>(`${BASE_URL}/${icao}/departures`));
  }

  async jumpseat(icao: string): Promise<JumpseatResult> {
    const result = await firstValueFrom(this.http.post<JumpseatResult>(`${BASE_URL}/${icao}/jumpseat`, {}));
    await this.auth.refreshProfile().catch(() => undefined);
    return result;
  }
}