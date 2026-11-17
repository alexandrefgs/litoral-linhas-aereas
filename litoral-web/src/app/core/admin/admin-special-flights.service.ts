import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { SpecialFlight, SpecialFlightType } from '../special-flights/special-flights.models';

export interface OperationsAircraft {
  id: string;
  registration: string;
  typeIcao: string;
  typeName: string;
  isCargo: boolean;
  locationIcao: string;
  status: string;
  deliveryAirportIcao: string | null;
  inSpecialFlight: boolean;
}

export interface OperationsAirport {
  icao: string;
  name: string;
  city: string;
}

export interface OperationsRank {
  code: string;
  name: string;
  level: number;
}

export interface SpecialFlightOptions {
  maintenanceHubIcao: string;
  aircraft: OperationsAircraft[];
  airports: OperationsAirport[];
  ranks: OperationsRank[];
}

export interface SpecialFlightPayload {
  title: string;
  description: string | null;
  type: SpecialFlightType;
  difficulty: number;
  aircraftId: string;
  minimumRank: string;
  startsAt: string;
  endsAt: string;
  stops: string[];
}

const BASE_URL = '/api/admin/special-flights';

@Injectable({ providedIn: 'root' })
export class AdminSpecialFlightsService {
  private readonly http = inject(HttpClient);

  options(): Promise<SpecialFlightOptions> {
    return firstValueFrom(this.http.get<SpecialFlightOptions>(`${BASE_URL}/options`));
  }

  list(): Promise<SpecialFlight[]> {
    return firstValueFrom(this.http.get<SpecialFlight[]>(BASE_URL));
  }

  create(payload: SpecialFlightPayload): Promise<SpecialFlight> {
    return firstValueFrom(this.http.post<SpecialFlight>(BASE_URL, payload));
  }

  update(id: string, payload: SpecialFlightPayload): Promise<SpecialFlight> {
    return firstValueFrom(this.http.put<SpecialFlight>(`${BASE_URL}/${id}`, payload));
  }

  cancel(id: string): Promise<SpecialFlight> {
    return firstValueFrom(this.http.post<SpecialFlight>(`${BASE_URL}/${id}/cancel`, {}));
  }

  remove(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${BASE_URL}/${id}`));
  }

  uploadPhoto(id: string, file: File, credit: string | null): Promise<SpecialFlight> {
    const form = new FormData();
    form.append('file', file);
    if (credit) form.append('credit', credit);
    return firstValueFrom(this.http.post<SpecialFlight>(`${BASE_URL}/${id}/photo`, form));
  }

  updatePhotoCredit(id: string, credit: string | null): Promise<SpecialFlight> {
    return firstValueFrom(this.http.put<SpecialFlight>(`${BASE_URL}/${id}/photo/credit`, { credit }));
  }

  removePhoto(id: string): Promise<SpecialFlight> {
    return firstValueFrom(this.http.delete<SpecialFlight>(`${BASE_URL}/${id}/photo`));
  }
}