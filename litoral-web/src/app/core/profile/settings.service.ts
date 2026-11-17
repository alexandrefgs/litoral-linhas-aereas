import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { PilotSettings, UpdateConfigPayload, UpdatePersonalPayload } from './settings.models';

const BASE_URL = '/api/pilots/me';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  get(): Promise<PilotSettings> {
    return firstValueFrom(this.http.get<PilotSettings>(`${BASE_URL}/settings`));
  }

  updatePersonal(payload: UpdatePersonalPayload): Promise<PilotSettings> {
    return this.withRefresh(this.http.put<PilotSettings>(`${BASE_URL}/settings/personal`, payload));
  }

  updateConfig(payload: UpdateConfigPayload): Promise<PilotSettings> {
    return this.withRefresh(this.http.put<PilotSettings>(`${BASE_URL}/settings/config`, payload));
  }

  deleteAccount(password: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${BASE_URL}/delete`, { password }));
  }

  changePassword(currentPassword: string, newPassword: string): Promise<void> {
    return firstValueFrom(this.http.put<void>(`${BASE_URL}/password`, { currentPassword, newPassword }));
  }

  uploadPhoto(file: File): Promise<PilotSettings> {
    return this.withRefresh(this.http.post<PilotSettings>(`${BASE_URL}/photo`, this.form(file)));
  }

  removePhoto(): Promise<PilotSettings> {
    return this.withRefresh(this.http.delete<PilotSettings>(`${BASE_URL}/photo`));
  }

  uploadCover(file: File): Promise<PilotSettings> {
    return this.withRefresh(this.http.post<PilotSettings>(`${BASE_URL}/cover`, this.form(file)));
  }

  removeCover(): Promise<PilotSettings> {
    return this.withRefresh(this.http.delete<PilotSettings>(`${BASE_URL}/cover`));
  }

  private form(file: File): FormData {
    const data = new FormData();
    data.append('file', file);
    return data;
  }

  private async withRefresh(request: Observable<PilotSettings>): Promise<PilotSettings> {
    const result = await firstValueFrom(request);
    await this.auth.refreshProfile().catch(() => undefined);
    return result;
  }
}