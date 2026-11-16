import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface AdminAirport {
  icao: string;
  name: string;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
  minimumRank: string;
  passenger: boolean;
  cargo: boolean;
  isHub: boolean;
}

export interface AirportPayload {
  name: string;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
  minimumRank: string;
  passenger: boolean;
  cargo: boolean;
  isHub: boolean;
}

const BASE_URL = '/api/admin/airports';

@Injectable({ providedIn: 'root' })
export class AdminAirportsService {
  private readonly http = inject(HttpClient);

  list(): Promise<AdminAirport[]> {
    return firstValueFrom(this.http.get<AdminAirport[]>(BASE_URL));
  }

  create(icao: string, payload: AirportPayload): Promise<AdminAirport> {
    return firstValueFrom(this.http.post<AdminAirport>(BASE_URL, { icao, ...payload }));
  }

  update(icao: string, payload: AirportPayload): Promise<AdminAirport> {
    return firstValueFrom(this.http.put<AdminAirport>(`${BASE_URL}/${encodeURIComponent(icao)}`, payload));
  }

  remove(icao: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${BASE_URL}/${encodeURIComponent(icao)}`));
  }
}