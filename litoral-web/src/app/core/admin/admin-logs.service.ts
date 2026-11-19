import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export type AuditCategory = 'Account' | 'Pilots' | 'Staff' | 'Pireps' | 'Scoring' | 'Fleet' | 'Routes' | 'Airports' | 'Operations' | 'Awards' | 'Bookings' | 'Logins' | 'Privacy';

export type AuditSeverity = 'Critical' | 'Warning' | 'Info';

export interface AuditLogEntry {
  id: string;
  at: string;
  category: AuditCategory;
  severity: AuditSeverity;
  action: string;
  actorName: string;
  targetType: string | null;
  targetName: string | null;
  details: string | null;
  ipAddress: string | null;
}

export interface AuditLogPage {
  items: AuditLogEntry[];
  total: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class AdminLogsService {
  private readonly http = inject(HttpClient);

  list(category: AuditCategory | null, search: string, criticalOnly: boolean, page: number, pageSize: number): Promise<AuditLogPage> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (criticalOnly) params = params.set('criticalOnly', true);
    if (category) params = params.set('category', category);
    if (search.trim()) params = params.set('search', search.trim());
    return firstValueFrom(this.http.get<AuditLogPage>('/api/admin/logs', { params }));
  }
}