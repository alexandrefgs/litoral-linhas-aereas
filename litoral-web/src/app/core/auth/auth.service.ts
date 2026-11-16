import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthResponse, LoginRequest, PilotSummary, RegisterRequest } from './auth.models';

const TOKEN_KEY = 'litoral.token';
const EXPIRES_KEY = 'litoral.tokenExpiresAt';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly token = signal<string | null>(this.readToken());
  readonly pilot = signal<PilotSummary | null>(null);
  readonly isAuthenticated = computed(() => this.token() !== null);
  readonly isStaff = computed(() => (this.pilot()?.staffPositions.length ?? 0) > 0);

  async init(): Promise<void> {
    if (!this.token()) return;

    try {
      await this.refreshProfile();
    } catch {
      this.clear();
    }
  }

  async login(request: LoginRequest): Promise<void> {
    const response = await firstValueFrom(this.http.post<AuthResponse>('/api/auth/login', request));
    this.store(response);
  }

  async register(request: RegisterRequest): Promise<void> {
    const response = await firstValueFrom(this.http.post<AuthResponse>('/api/auth/register', request));
    this.store(response);
  }

  async refreshProfile(): Promise<void> {
    const pilot = await firstValueFrom(this.http.get<PilotSummary>('/api/auth/me'));
    this.pilot.set(pilot);
  }

  updatePilot(pilot: PilotSummary): void {
    this.pilot.set(pilot);
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