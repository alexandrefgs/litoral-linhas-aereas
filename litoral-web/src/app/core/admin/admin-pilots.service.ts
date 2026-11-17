import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface AdminPilot {
  id: string;
  pilotNumber: number;
  displayId: string;
  fullName: string;
  email: string;
  rankCode: string;
  photoUrl: string | null;
  baseIcao: string;
  locationIcao: string;
  flightMinutes: number;
  points: number;
  flights: number;
  lastFlightAt: string | null;
  joinedAt: string;
  isActive: boolean;
  staffPositions: string[];
  canDelete: boolean;
}

const BASE_URL = '/api/admin/pilots';

@Injectable({ providedIn: 'root' })
export class AdminPilotsService {
  private readonly http = inject(HttpClient);

  list(): Promise<AdminPilot[]> {
    return firstValueFrom(this.http.get<AdminPilot[]>(BASE_URL));
  }

  deactivate(id: string): Promise<AdminPilot> {
    return firstValueFrom(this.http.post<AdminPilot>(`${BASE_URL}/${id}/deactivate`, {}));
  }

  activate(id: string): Promise<AdminPilot> {
    return firstValueFrom(this.http.post<AdminPilot>(`${BASE_URL}/${id}/activate`, {}));
  }

  remove(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${BASE_URL}/${id}`));
  }
}