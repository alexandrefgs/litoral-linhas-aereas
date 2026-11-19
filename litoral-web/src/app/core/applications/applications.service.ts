import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface RegistrationStatus {
  open: boolean;
  simulators: string[];
  addOns: string[];
}

export interface PilotApplication {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  hubIcao: string;
  vatsimId: string | null;
  ivaoId: string | null;
  simulator: string;
  addOns: string[];
  motivation: string;
  createdAt: string;
  vatsimInUse: boolean;
  ivaoInUse: boolean;
}

export interface AdminApplications {
  registrationOpen: boolean;
  applications: PilotApplication[];
}

@Injectable({ providedIn: 'root' })
export class ApplicationsService {
  private readonly http = inject(HttpClient);

  registrationStatus(): Promise<RegistrationStatus> {
    return firstValueFrom(this.http.get<RegistrationStatus>('/api/registration'));
  }

  list(): Promise<AdminApplications> {
    return firstValueFrom(this.http.get<AdminApplications>('/api/admin/applications'));
  }

  approve(id: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`/api/admin/applications/${id}/approve`, {}));
  }

  reject(id: string, reason: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`/api/admin/applications/${id}/reject`, { reason }));
  }

  setRegistrationOpen(open: boolean): Promise<void> {
    return firstValueFrom(this.http.put<void>('/api/admin/applications/registration', { open }));
  }
}