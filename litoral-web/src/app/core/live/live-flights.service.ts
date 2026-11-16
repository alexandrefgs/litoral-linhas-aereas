import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { FlightNetwork } from '../pirep/pirep.models';

export interface LiveFlight {
  id: string;
  callsign: string;
  pilotId: string;
  pilotName: string;
  registration: string;
  aircraftTypeIcao: string;
  departureIcao: string;
  arrivalIcao: string;
  network: FlightNetwork;
  latitude: number;
  longitude: number;
  altitudeFt: number;
  groundSpeedKt: number;
  heading: number;
  phase: string | null;
  lastReportAt: string;
}

@Injectable({ providedIn: 'root' })
export class LiveFlightsService {
  private readonly http = inject(HttpClient);

  list(): Promise<LiveFlight[]> {
    return firstValueFrom(this.http.get<LiveFlight[]>('/api/acars/live'));
  }
}