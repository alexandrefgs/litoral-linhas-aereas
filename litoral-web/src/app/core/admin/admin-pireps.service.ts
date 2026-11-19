import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AdminPirep, AdminPirepListItem, PirepStatus } from '../pirep/pirep.models';

const BASE_URL = '/api/admin/pireps';

@Injectable({ providedIn: 'root' })
export class AdminPirepsService {
  private readonly http = inject(HttpClient);

  list(status: PirepStatus | null): Promise<AdminPirepListItem[]> {
    const params = status ? new HttpParams().set('status', status) : undefined;
    return firstValueFrom(this.http.get<AdminPirepListItem[]>(BASE_URL, { params }));
  }

  get(id: string): Promise<AdminPirep> {
    return firstValueFrom(this.http.get<AdminPirep>(`${BASE_URL}/${id}`));
  }

  addItem(id: string, ruleId: string, quantity: number): Promise<AdminPirep> {
    return firstValueFrom(this.http.post<AdminPirep>(`${BASE_URL}/${id}/items`, { ruleId, quantity }));
  }

  toggleItem(id: string, ruleId: string): Promise<AdminPirep> {
    return firstValueFrom(this.http.post<AdminPirep>(`${BASE_URL}/${id}/items/${ruleId}/toggle`, {}));
  }

  approve(id: string, note: string | null): Promise<AdminPirep> {
    return firstValueFrom(this.http.post<AdminPirep>(`${BASE_URL}/${id}/approve`, { note }));
  }

  decideMel(id: string, checkId: string, complied: boolean): Promise<AdminPirep> {
    return firstValueFrom(this.http.post<AdminPirep>(`${BASE_URL}/${id}/mel/${checkId}`, { complied }));
  }

  reject(id: string, note: string): Promise<AdminPirep> {
    return firstValueFrom(this.http.post<AdminPirep>(`${BASE_URL}/${id}/reject`, { note }));
  }
}