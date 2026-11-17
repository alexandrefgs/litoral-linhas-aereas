import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Report, Statistics } from './statistics.models';

@Injectable({ providedIn: 'root' })
export class StatisticsService {
  private readonly http = inject(HttpClient);

  get(): Promise<Statistics> {
    return firstValueFrom(this.http.get<Statistics>('/api/statistics'));
  }

  report(from: Date, to: Date): Promise<Report> {
    const params = new HttpParams().set('from', from.toISOString()).set('to', to.toISOString());
    return firstValueFrom(this.http.get<Report>('/api/admin/reports', { params }));
  }
}