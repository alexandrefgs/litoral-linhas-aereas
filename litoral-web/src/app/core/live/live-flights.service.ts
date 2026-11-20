import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { FlightNetwork, TrackPoint } from '../pirep/pirep.models';

export interface LiveAirport {
  icao: string;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
}

export interface LiveFlight {
  id: string;
  callsign: string;
  pilotId: string;
  pilotName: string;
  pilotPhotoUrl: string | null;
  registration: string;
  aircraftTypeIcao: string;
  aircraftTypeName: string;
  photoUrl: string | null;
  photoCredit: string | null;
  isCargo: boolean;
  departure: LiveAirport;
  arrival: LiveAirport;
  network: FlightNetwork;
  simulator: string | null;
  latitude: number;
  longitude: number;
  altitudeFt: number;
  groundSpeedKt: number;
  heading: number;
  phase: string | null;
  distanceNm: number;
  remainingNm: number;
  progress: number;
  minutesToArrival: number | null;
  lastReportAt: string;
}

@Injectable({ providedIn: 'root' })
export class LiveFlightsService {
  private readonly http = inject(HttpClient);

  list(): Promise<LiveFlight[]> {
    return firstValueFrom(this.http.get<LiveFlight[]>('/api/acars/live'));
  }

  track(id: string): Promise<TrackPoint[]> {
    return firstValueFrom(this.http.get<TrackPoint[]>(`/api/acars/live/${id}/track`));
  }
}