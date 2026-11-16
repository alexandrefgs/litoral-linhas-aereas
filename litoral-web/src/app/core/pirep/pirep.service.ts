import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { BookingService } from '../booking/booking.service';
import { ManualPirepPayload, Pirep, PirepSummary, SimulationScenario } from './pirep.models';

const BASE_URL = '/api/pireps';

@Injectable({ providedIn: 'root' })
export class PirepService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly booking = inject(BookingService);

  list(): Promise<PirepSummary[]> {
    return firstValueFrom(this.http.get<PirepSummary[]>(BASE_URL));
  }

  get(id: string): Promise<Pirep> {
    return firstValueFrom(this.http.get<Pirep>(`${BASE_URL}/${id}`));
  }

  fileManual(payload: ManualPirepPayload): Promise<Pirep> {
    return this.afterFiling(firstValueFrom(this.http.post<Pirep>(`${BASE_URL}/manual`, payload)));
  }

  simulate(scenario: SimulationScenario): Promise<Pirep> {
    return this.afterFiling(firstValueFrom(this.http.post<Pirep>(`${BASE_URL}/simulate`, { scenario })));
  }

  private async afterFiling(request: Promise<Pirep>): Promise<Pirep> {
    const pirep = await request;
    this.booking.current.set(null);
    await this.auth.refreshProfile();
    return pirep;
  }
}