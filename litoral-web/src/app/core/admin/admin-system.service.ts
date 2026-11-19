import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { SystemStatus } from './admin-system.models';

@Injectable({ providedIn: 'root' })
export class AdminSystemService {
  private readonly http = inject(HttpClient);

  status(): Promise<SystemStatus> {
    return firstValueFrom(this.http.get<SystemStatus>('/api/admin/system'));
  }
}