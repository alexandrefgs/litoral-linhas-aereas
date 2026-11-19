import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface AcarsKey {
  id: string;
  name: string;
  prefix: string;
  createdAt: string;
  lastUsedAt: string | null;
}

export interface CreatedAcarsKey {
  key: AcarsKey;
  secret: string;
}

export const MAX_ACARS_KEYS = 3;

const BASE_URL = '/api/pilots/me/acars-keys';

@Injectable({ providedIn: 'root' })
export class AcarsKeysService {
  private readonly http = inject(HttpClient);

  list(): Promise<AcarsKey[]> {
    return firstValueFrom(this.http.get<AcarsKey[]>(BASE_URL));
  }

  create(name: string): Promise<CreatedAcarsKey> {
    return firstValueFrom(this.http.post<CreatedAcarsKey>(BASE_URL, { name }));
  }

  revoke(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${BASE_URL}/${id}`));
  }
}