import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export type StaffTeam = 'Executive' | 'Operations' | 'Safety';

export interface StaffMember {
  pilotId: string;
  pilotNumber: number;
  displayId: string;
  name: string;
  photoUrl: string | null;
}

export interface StaffPosition {
  code: string;
  name: string;
  team: StaffTeam;
  singleHolder: boolean;
  editable: boolean;
  holders: StaffMember[];
}

export interface StaffOverview {
  positions: StaffPosition[];
  candidates: StaffMember[];
}

const BASE_URL = '/api/admin/staff';

@Injectable({ providedIn: 'root' })
export class AdminStaffService {
  private readonly http = inject(HttpClient);

  get(): Promise<StaffOverview> {
    return firstValueFrom(this.http.get<StaffOverview>(BASE_URL));
  }

  assign(code: string, pilotId: string): Promise<StaffOverview> {
    return firstValueFrom(this.http.post<StaffOverview>(`${BASE_URL}/${code}`, { pilotId }));
  }

  remove(code: string, pilotId: string): Promise<StaffOverview> {
    return firstValueFrom(this.http.delete<StaffOverview>(`${BASE_URL}/${code}/${pilotId}`));
  }
}