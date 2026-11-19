import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { Airport, CareerRank, Hub, PublicStaffPosition, RankInfo } from './reference.models';
import { PublicStats } from './public-stats';

@Injectable({ providedIn: 'root' })
export class ReferenceService {
  private readonly http = inject(HttpClient);

  getHubs(): Promise<Hub[]> {
    return firstValueFrom(this.http.get<Hub[]>('/api/hubs'));
  }

  getRanks(): Promise<RankInfo[]> {
    return firstValueFrom(this.http.get<RankInfo[]>('/api/ranks'));
  }

  getAirport(icao: string): Promise<Airport> {
    return firstValueFrom(this.http.get<Airport>(`/api/airports/${encodeURIComponent(icao)}`));
  }

  getPublicStats(): Promise<PublicStats> {
    return firstValueFrom(this.http.get<PublicStats>('/api/public/stats'));
  }

  getStaff(): Promise<PublicStaffPosition[]> {
    return firstValueFrom(this.http.get<PublicStaffPosition[]>('/api/public/staff'));
  }

  getCareer(): Promise<CareerRank[]> {
    return firstValueFrom(this.http.get<CareerRank[]>('/api/public/career'));
  }
}