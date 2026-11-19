import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthResponse, LoginRequest, LoginResponse, LoginResult, PilotSummary, RegisterRequest, RegisterResponse, RegisterResult } from './auth.models';

const TOKEN_KEY = 'litoral.token';
const EXPIRES_KEY = 'litoral.tokenExpiresAt';
const TRUSTED_DEVICES_KEY = 'litoral.trustedDevices';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly token = signal<string | null>(this.readToken());
  readonly pilot = signal<PilotSummary | null>(null);
  readonly isAuthenticated = computed(() => this.token() !== null);
  readonly isStaff = computed(() => (this.pilot()?.staffPositions.length ?? 0) > 0);
  readonly needsTwoFactor = computed(() => {
    const pilot = this.pilot();
    return !!pilot && pilot.twoFactorRequired && !pilot.twoFactorEnabled;
  });

  async init(): Promise<void> {
    if (!this.token()) return;

    try {
      await this.refreshProfile();
    } catch {
      this.clear();
    }
  }

  async login(request: LoginRequest): Promise<LoginResult> {
    const email = request.email.trim().toLowerCase();
    const trustedDevice = this.trustedDevices()[email] ?? null;
    const response = await firstValueFrom(this.http.post<LoginResponse>('/api/auth/login', { ...request, trustedDevice }));

    if (response.session) {
      this.store(response.session);
      return { kind: 'session' };
    }

    if (response.application) return { kind: 'application', application: response.application };

    if (trustedDevice) this.setTrustedDevice(email, null);
    return { kind: 'two-factor', challengeToken: response.challengeToken! };
  }

  async loginWithTwoFactor(email: string, challengeToken: string, code: string, rememberDevice: boolean): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<AuthResponse>('/api/auth/login/two-factor', { challengeToken, code, rememberDevice })
    );

    if (response.trustedDevice) this.setTrustedDevice(email.trim().toLowerCase(), response.trustedDevice);
    this.store(response);
  }

  async register(request: RegisterRequest): Promise<RegisterResult> {
    const response = await firstValueFrom(this.http.post<RegisterResponse>('/api/auth/register', request));
    if (response.session) {
      this.store(response.session);
      return { kind: 'session' };
    }
    return { kind: 'application', application: response.application! };
  }

  async refreshProfile(): Promise<void> {
    const pilot = await firstValueFrom(this.http.get<PilotSummary>('/api/auth/me'));
    this.pilot.set(pilot);
  }

  updatePilot(pilot: PilotSummary): void {
    this.pilot.set(pilot);
  }

  forgetTrustedDevice(): void {
    const email = this.pilot()?.email;
    if (email) this.setTrustedDevice(email.toLowerCase(), null);
  }

  logout(): void {
    this.clear();
    this.router.navigate(['/login']);
  }

  private store(response: AuthResponse): void {
    try {
      localStorage.setItem(TOKEN_KEY, response.token);
      localStorage.setItem(EXPIRES_KEY, response.expiresAt);
    } catch {}

    this.token.set(response.token);
    this.pilot.set(response.pilot);
  }

  private clear(): void {
    try {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(EXPIRES_KEY);
    } catch {}

    this.token.set(null);
    this.pilot.set(null);
  }

  private trustedDevices(): Record<string, string> {
    try {
      const parsed = JSON.parse(localStorage.getItem(TRUSTED_DEVICES_KEY) ?? '{}');
      return parsed && typeof parsed === 'object' ? parsed : {};
    } catch {
      return {};
    }
  }

  private setTrustedDevice(email: string, token: string | null): void {
    const devices = this.trustedDevices();
    if (token) devices[email] = token;
    else delete devices[email];

    try {
      localStorage.setItem(TRUSTED_DEVICES_KEY, JSON.stringify(devices));
    } catch {}
  }

  private readToken(): string | null {
    try {
      const token = localStorage.getItem(TOKEN_KEY);
      const expiresAt = localStorage.getItem(EXPIRES_KEY);

      if (!token || !expiresAt || Date.parse(expiresAt) <= Date.now()) {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(EXPIRES_KEY);
        return null;
      }

      return token;
    } catch {
      return null;
    }
  }
}