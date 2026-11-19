import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export type PrivacyRequestType = 'Access' | 'Correction' | 'Deletion' | 'Portability' | 'ConsentWithdrawal' | 'Other';
export type PrivacyRequestStatus = 'Open' | 'Completed' | 'Rejected';

export const PRIVACY_REQUEST_TYPES: { value: PrivacyRequestType; label: string }[] = [
  { value: 'Access', label: 'Access my data' },
  { value: 'Correction', label: 'Correct my data' },
  { value: 'Deletion', label: 'Delete my data' },
  { value: 'Portability', label: 'Data portability' },
  { value: 'ConsentWithdrawal', label: 'Withdraw consent' },
  { value: 'Other', label: 'Other question' }
];

export interface PrivacyRequest {
  id: string;
  type: PrivacyRequestType;
  message: string;
  status: PrivacyRequestStatus;
  createdAt: string;
  dueAt: string;
  response: string | null;
  resolvedAt: string | null;
}

export interface AdminPrivacyRequest extends PrivacyRequest {
  pilotNumber: number | null;
  pilotLabel: string;
  email: string | null;
  resolvedBy: string | null;
}

export interface PrivacyOverview {
  currentTermsVersion: string;
  acceptedTermsVersion: string | null;
  acceptedTermsAt: string | null;
  requests: PrivacyRequest[];
}

@Injectable({ providedIn: 'root' })
export class PrivacyService {
  private readonly http = inject(HttpClient);

  overview(): Promise<PrivacyOverview> {
    return firstValueFrom(this.http.get<PrivacyOverview>('/api/account/privacy'));
  }

  acceptTerms(): Promise<void> {
    return firstValueFrom(this.http.post<void>('/api/account/privacy/terms/accept', {}));
  }

  createRequest(type: PrivacyRequestType, message: string): Promise<PrivacyRequest> {
    return firstValueFrom(this.http.post<PrivacyRequest>('/api/account/privacy/requests', { type, message }));
  }
  
  removeOptionalData(): Promise<void> {
    return firstValueFrom(this.http.post<void>('/api/account/privacy/remove-optional-data', {}));
  }

  async downloadExport(): Promise<void> {
    const response = await firstValueFrom(this.http.get('/api/account/privacy/export', { observe: 'response', responseType: 'blob' }));
    const name = /filename="?([^";]+)"?/.exec(response.headers.get('content-disposition') ?? '')?.[1] ?? 'litoral-data.json';
    const url = URL.createObjectURL(response.body!);
    const link = document.createElement('a');
    link.href = url;
    link.download = name;
    link.click();
    URL.revokeObjectURL(url);
  }

  adminList(openOnly: boolean): Promise<AdminPrivacyRequest[]> {
    const params = new HttpParams().set('openOnly', openOnly);
    return firstValueFrom(this.http.get<AdminPrivacyRequest[]>('/api/admin/privacy-requests', { params }));
  }

  resolve(id: string, status: 'Completed' | 'Rejected', response: string): Promise<AdminPrivacyRequest> {
    return firstValueFrom(this.http.post<AdminPrivacyRequest>(`/api/admin/privacy-requests/${id}/resolve`, { status, response }));
  }
}