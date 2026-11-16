import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ScoringSection } from './scoring.models';

@Injectable({ providedIn: 'root' })
export class ScoringService {
  private readonly http = inject(HttpClient);

  table(): Promise<ScoringSection[]> {
    return firstValueFrom(this.http.get<ScoringSection[]>('/api/scoring'));
  }
}