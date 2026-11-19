import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AdminAward, AwardHolder, AwardPayload, AwardPilotOption, PilotAward } from './awards.models';

const ADMIN_URL = '/api/admin/awards';

@Injectable({ providedIn: 'root' })
export class AwardsService {
  private readonly http = inject(HttpClient);

  forPilot(pilotNumber: number): Promise<PilotAward[]> {
    return firstValueFrom(this.http.get<PilotAward[]>(`/api/profiles/${pilotNumber}/awards`));
  }

  list(): Promise<AdminAward[]> {
    return firstValueFrom(this.http.get<AdminAward[]>(ADMIN_URL));
  }

  pilots(): Promise<AwardPilotOption[]> {
    return firstValueFrom(this.http.get<AwardPilotOption[]>(`${ADMIN_URL}/pilots`));
  }

  create(payload: AwardPayload): Promise<AdminAward> {
    return firstValueFrom(this.http.post<AdminAward>(ADMIN_URL, payload));
  }

  update(id: string, payload: AwardPayload): Promise<AdminAward> {
    return firstValueFrom(this.http.put<AdminAward>(`${ADMIN_URL}/${id}`, payload));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${ADMIN_URL}/${id}`));
  }

  uploadImage(id: string, file: File): Promise<AdminAward> {
    const form = new FormData();
    form.append('file', file);
    return firstValueFrom(this.http.post<AdminAward>(`${ADMIN_URL}/${id}/image`, form));
  }

  removeImage(id: string): Promise<AdminAward> {
    return firstValueFrom(this.http.delete<AdminAward>(`${ADMIN_URL}/${id}/image`));
  }

  holders(id: string): Promise<AwardHolder[]> {
    return firstValueFrom(this.http.get<AwardHolder[]>(`${ADMIN_URL}/${id}/holders`));
  }

  grant(id: string, pilotId: string, note: string | null): Promise<AwardHolder[]> {
    return firstValueFrom(this.http.post<AwardHolder[]>(`${ADMIN_URL}/${id}/holders`, { pilotId, note }));
  }

  revoke(id: string, pilotId: string): Promise<AwardHolder[]> {
    return firstValueFrom(this.http.delete<AwardHolder[]>(`${ADMIN_URL}/${id}/holders/${pilotId}`));
  }
}