import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { FlightDetail, RecentFlights } from './flight.models';

@Injectable({ providedIn: 'root' })
export class FlightService {
  private readonly http = inject(HttpClient);

  get(id: string): Promise<FlightDetail> {
    return firstValueFrom(this.http.get<FlightDetail>(`/api/flights/${id}`));
  }

  recent(take = 4): Promise<RecentFlights> {
    return firstValueFrom(this.http.get<RecentFlights>('/api/flights/recent', { params: new HttpParams().set('take', take) }));
  }
}