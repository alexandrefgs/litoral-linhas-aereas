import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { RecoveryCodes, TwoFactorSetup, TwoFactorStatus } from './auth.models';

@Injectable({ providedIn: 'root' })
export class TwoFactorService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/account/two-factor';

  status(): Promise<TwoFactorStatus> {
    return firstValueFrom(this.http.get<TwoFactorStatus>(this.base));
  }

  setup(): Promise<TwoFactorSetup> {
    return firstValueFrom(this.http.post<TwoFactorSetup>(`${this.base}/setup`, {}));
  }

  enable(code: string): Promise<RecoveryCodes> {
    return firstValueFrom(this.http.post<RecoveryCodes>(`${this.base}/enable`, { code }));
  }

  regenerateRecoveryCodes(password: string, code: string): Promise<RecoveryCodes> {
    return firstValueFrom(this.http.post<RecoveryCodes>(`${this.base}/recovery-codes`, { password, code }));
  }

  disable(password: string, code: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.base}/disable`, { password, code }));
  }
}