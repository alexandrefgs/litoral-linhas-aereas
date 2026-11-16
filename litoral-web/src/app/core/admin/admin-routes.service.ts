import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface AdminRoute {
  id: string;
  flightNumber: number;
  callsign: string;
  block: string;
  departureIcao: string;
  departureCity: string;
  arrivalIcao: string;
  arrivalCity: string;
  isCargo: boolean;
  aircraftTypeIds: string[];
  aircraftTypes: string[];
  distanceNm: number;
  estimatedMinutes: number;
  isActive: boolean;
}

export interface AircraftTypeOption {
  id: string;
  icaoCode: string;
  name: string;
  isCargo: boolean;
  maxRangeNm: number;
}

export interface FlightNumberSuggestion {
  block: string;
  flightNumber: number;
  returnFlightNumber: number | null;
}

export interface CreateRoutePayload {
  flightNumber: number;
  departureIcao: string;
  arrivalIcao: string;
  aircraftTypeIds: string[];
  estimatedMinutes: number | null;
  createReturn: boolean;
  returnFlightNumber: number | null;
}

export interface UpdateRoutePayload {
  flightNumber: number;
  departureIcao: string;
  arrivalIcao: string;
  aircraftTypeIds: string[];
  estimatedMinutes: number | null;
  isActive: boolean;
}

const BASE_URL = '/api/admin/routes';

@Injectable({ providedIn: 'root' })
export class AdminRoutesService {
  private readonly http = inject(HttpClient);

  list(): Promise<AdminRoute[]> {
    return firstValueFrom(this.http.get<AdminRoute[]>(BASE_URL));
  }

  aircraftTypes(): Promise<AircraftTypeOption[]> {
    return firstValueFrom(this.http.get<AircraftTypeOption[]>(`${BASE_URL}/aircraft-types`));
  }

  suggestNumber(departure: string, arrival: string, cargo: boolean): Promise<FlightNumberSuggestion> {
    const params = new HttpParams().set('departure', departure).set('arrival', arrival).set('cargo', cargo);
    return firstValueFrom(this.http.get<FlightNumberSuggestion>(`${BASE_URL}/next-number`, { params }));
  }

  create(payload: CreateRoutePayload): Promise<AdminRoute[]> {
    return firstValueFrom(this.http.post<AdminRoute[]>(BASE_URL, payload));
  }

  update(id: string, payload: UpdateRoutePayload): Promise<AdminRoute> {
    return firstValueFrom(this.http.put<AdminRoute>(`${BASE_URL}/${id}`, payload));
  }

  remove(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${BASE_URL}/${id}`));
  }
}