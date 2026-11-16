import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface AdminAirport {
  icao: string;
  iata: string | null;
  name: string;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
  minimumRank: string;
  passenger: boolean;
  cargo: boolean;
  isHub: boolean;
  photoUrl: string | null;
  photoCredit: string | null;
}

export interface AirportPayload {
  iata: string | null;
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

export interface AirportLookup {
  icao: string;
  iata: string | null;
  name: string;
  city: string;
  country: string;
  countryCode: string;
  continent: string;
  latitude: number;
  longitude: number;
}

const BASE_URL = '/api/admin/airports';

@Injectable({ providedIn: 'root' })
export class AdminAirportsService {
  private readonly http = inject(HttpClient);

  list(): Promise<AdminAirport[]> {
    return firstValueFrom(this.http.get<AdminAirport[]>(BASE_URL));
  }

  lookup(icao: string): Promise<AirportLookup> {
    return firstValueFrom(this.http.get<AirportLookup>(`${BASE_URL}/lookup/${encodeURIComponent(icao)}`));
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

    uploadPhoto(icao: string, file: File, credit: string | null): Promise<AdminAirport> {
    const form = new FormData();
    form.append('file', file);
    if (credit) form.append('credit', credit);
    return firstValueFrom(this.http.post<AdminAirport>(`${BASE_URL}/${encodeURIComponent(icao)}/photo`, form));
  }

  updatePhotoCredit(icao: string, credit: string | null): Promise<AdminAirport> {
    return firstValueFrom(this.http.put<AdminAirport>(`${BASE_URL}/${encodeURIComponent(icao)}/photo/credit`, { credit }));
  }

  removePhoto(icao: string): Promise<AdminAirport> {
    return firstValueFrom(this.http.delete<AdminAirport>(`${BASE_URL}/${encodeURIComponent(icao)}/photo`));
  }
}