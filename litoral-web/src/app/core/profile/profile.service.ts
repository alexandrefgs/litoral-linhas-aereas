import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { FlightMap, PagedResult, PilotProfile, ProfileFlight, ProfileFlightsQuery } from './profile.models';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);

  get(pilotNumber: number): Promise<PilotProfile> {
    return firstValueFrom(this.http.get<PilotProfile>(`/api/profiles/${pilotNumber}`));
  }

  map(pilotNumber: number): Promise<FlightMap> {
    return firstValueFrom(this.http.get<FlightMap>(`/api/profiles/${pilotNumber}/map`));
  }

  flights(pilotNumber: number, query: ProfileFlightsQuery): Promise<PagedResult<ProfileFlight>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize)
      .set('sort', query.sort);
    if (query.search.trim()) params = params.set('search', query.search.trim());

    return firstValueFrom(this.http.get<PagedResult<ProfileFlight>>(`/api/profiles/${pilotNumber}/flights`, { params }));
  }
}