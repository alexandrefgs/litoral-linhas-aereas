import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export type AircraftStatus = 'Available' | 'Reserved' | 'InFlight' | 'Maintenance' | 'Inactive';

export interface AdminAircraftType {
  id: string;
  icaoCode: string;
  name: string;
  isCargo: boolean;
  maxRangeNm: number;
  maintenanceIntervalHours: number;
  registrationPrefix: string | null;
  deliveryAirportIcao: string | null;
  aircraftCount: number;
  routeCount: number;
}

export interface AircraftTypePayload {
  icaoCode: string;
  name: string;
  isCargo: boolean;
  maxRangeNm: number;
  maintenanceIntervalHours: number;
  registrationPrefix: string;
  deliveryAirportIcao: string | null;
}

export interface RegistrationSuggestion {
  block: string;
  registration: string | null;
  free: number;
}

export interface AdminAircraft {
  id: string;
  registration: string;
  aircraftTypeId: string;
  aircraftTypeIcao: string;
  aircraftTypeName: string;
  isCargo: boolean;
  variant: string | null;
  photoUrl: string | null;
  photoCredit: string | null;
  currentAirportIcao: string;
  currentAirportCity: string;
  status: AircraftStatus;
  totalMinutes: number;
  cycles: number;
  minutesSinceMaintenance: number;
  maintenanceIntervalHours: number;
  maintenanceDue: boolean;
  maintenanceReason: string | null;
  maintenanceStartedAt: string | null;
  maintenanceUntil: string | null;
  lastMaintenanceAt: string | null;
  lastFlightAt: string | null;
}

export interface AircraftPayload {
  registration: string;
  aircraftTypeId: string;
  variant: string | null;
  currentAirportIcao: string;
}

export interface FleetAirport {
  icao: string;
  name: string;
  city: string;
  passenger: boolean;
  cargo: boolean;
}

const BASE_URL = '/api/admin/fleet';

@Injectable({ providedIn: 'root' })
export class AdminFleetService {
  private readonly http = inject(HttpClient);

  types(): Promise<AdminAircraftType[]> {
    return firstValueFrom(this.http.get<AdminAircraftType[]>(`${BASE_URL}/types`));
  }

  createType(payload: AircraftTypePayload): Promise<AdminAircraftType> {
    return firstValueFrom(this.http.post<AdminAircraftType>(`${BASE_URL}/types`, payload));
  }

  updateType(id: string, payload: AircraftTypePayload): Promise<AdminAircraftType> {
    return firstValueFrom(this.http.put<AdminAircraftType>(`${BASE_URL}/types/${id}`, payload));
  }

  removeType(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${BASE_URL}/types/${id}`));
  }

  airports(): Promise<FleetAirport[]> {
    return firstValueFrom(this.http.get<FleetAirport[]>(`${BASE_URL}/airports`));
  }

  aircraft(): Promise<AdminAircraft[]> {
    return firstValueFrom(this.http.get<AdminAircraft[]>(`${BASE_URL}/aircraft`));
  }

  suggestRegistration(typeId: string): Promise<RegistrationSuggestion> {
    const params = new HttpParams().set('typeId', typeId);
    return firstValueFrom(this.http.get<RegistrationSuggestion>(`${BASE_URL}/aircraft/next-registration`, { params }));
  }

  createAircraft(payload: AircraftPayload): Promise<AdminAircraft> {
    return firstValueFrom(this.http.post<AdminAircraft>(`${BASE_URL}/aircraft`, payload));
  }

  updateAircraft(id: string, payload: AircraftPayload): Promise<AdminAircraft> {
    return firstValueFrom(this.http.put<AdminAircraft>(`${BASE_URL}/aircraft/${id}`, payload));
  }

  removeAircraft(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${BASE_URL}/aircraft/${id}`));
  }

  uploadPhoto(id: string, file: File, credit: string | null): Promise<AdminAircraft> {
    const form = new FormData();
    form.append('file', file);
    if (credit) form.append('credit', credit);
    return firstValueFrom(this.http.post<AdminAircraft>(`${BASE_URL}/aircraft/${id}/photo`, form));
  }

  updatePhotoCredit(id: string, credit: string | null): Promise<AdminAircraft> {
    return firstValueFrom(this.http.put<AdminAircraft>(`${BASE_URL}/aircraft/${id}/photo/credit`, { credit }));
  }

  removePhoto(id: string): Promise<AdminAircraft> {
    return firstValueFrom(this.http.delete<AdminAircraft>(`${BASE_URL}/aircraft/${id}/photo`));
  }

  sendToMaintenance(id: string, reason: string, until: string | null): Promise<AdminAircraft> {
    return firstValueFrom(this.http.post<AdminAircraft>(`${BASE_URL}/aircraft/${id}/maintenance`, { reason, until }));
  }

  releaseFromMaintenance(id: string): Promise<AdminAircraft> {
    return firstValueFrom(this.http.delete<AdminAircraft>(`${BASE_URL}/aircraft/${id}/maintenance`));
  }

  setActive(id: string, active: boolean): Promise<AdminAircraft> {
    return firstValueFrom(this.http.put<AdminAircraft>(`${BASE_URL}/aircraft/${id}/active`, { active }));
  }
}