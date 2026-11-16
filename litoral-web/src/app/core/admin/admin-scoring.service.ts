import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ScoringCatalog, ScoringRule, ScoringRulePayload } from '../scoring/scoring.models';

const BASE_URL = '/api/admin/scoring';

@Injectable({ providedIn: 'root' })
export class AdminScoringService {
  private readonly http = inject(HttpClient);

  catalog(): Promise<ScoringCatalog> {
    return firstValueFrom(this.http.get<ScoringCatalog>(`${BASE_URL}/catalog`));
  }

  rules(): Promise<ScoringRule[]> {
    return firstValueFrom(this.http.get<ScoringRule[]>(`${BASE_URL}/rules`));
  }

  create(payload: ScoringRulePayload): Promise<ScoringRule> {
    return firstValueFrom(this.http.post<ScoringRule>(`${BASE_URL}/rules`, payload));
  }

  update(id: string, payload: ScoringRulePayload): Promise<ScoringRule> {
    return firstValueFrom(this.http.put<ScoringRule>(`${BASE_URL}/rules/${id}`, payload));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${BASE_URL}/rules/${id}`));
  }
}