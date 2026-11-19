import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AircraftMel, MelItem, MelItemPayload, MelOverview } from './mel.models';

const ADMIN_URL = '/api/admin/mel';

@Injectable({ providedIn: 'root' })
export class MelService {
  private readonly http = inject(HttpClient);

  forAircraft(ids: string[]): Promise<AircraftMel[]> {
    if (ids.length === 0) return Promise.resolve([]);
    let params = new HttpParams();
    for (const id of ids) params = params.append('ids', id);
    return firstValueFrom(this.http.get<AircraftMel[]>('/api/mel/aircraft', { params }));
  }

  catalog(): Promise<MelItem[]> {
    return firstValueFrom(this.http.get<MelItem[]>(`${ADMIN_URL}/catalog`));
  }

  create(payload: MelItemPayload): Promise<MelItem> {
    return firstValueFrom(this.http.post<MelItem>(`${ADMIN_URL}/catalog`, payload));
  }

  update(id: string, payload: MelItemPayload): Promise<MelItem> {
    return firstValueFrom(this.http.put<MelItem>(`${ADMIN_URL}/catalog/${id}`, payload));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${ADMIN_URL}/catalog/${id}`));
  }

  overview(): Promise<MelOverview> {
    return firstValueFrom(this.http.get<MelOverview>(`${ADMIN_URL}/open`));
  }

  open(aircraftId: string, melItemId: string, note: string | null): Promise<MelOverview> {
    return firstValueFrom(this.http.post<MelOverview>(`${ADMIN_URL}/open`, { aircraftId, melItemId, note }));
  }

  close(id: string): Promise<MelOverview> {
    return firstValueFrom(this.http.post<MelOverview>(`${ADMIN_URL}/open/${id}/close`, {}));
  }
}