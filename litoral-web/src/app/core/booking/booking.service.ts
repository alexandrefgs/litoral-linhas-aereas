import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import {
  AircraftOptions,
  Booking,
  BookingOverview,
  Briefing,
  Destination,
  FleetAircraft,
  FlightDuration,
  Metar,
  RandomProposal,
  RosterStatus,
  SimBriefOfp,
  TransferQuote
} from './booking.models';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  readonly current = signal<Booking | null>(null);

  async refreshCurrent(): Promise<BookingOverview | null> {
    try {
      const overview = await this.overview();
      this.current.set(overview.current);
      return overview;
    } catch {
      this.current.set(null);
      return null;
    }
  }

  overview(): Promise<BookingOverview> {
    return firstValueFrom(this.http.get<BookingOverview>('/api/bookings'));
  }

  destinations(): Promise<Destination[]> {
    return firstValueFrom(this.http.get<Destination[]>('/api/bookings/destinations'));
  }

  routeAircraft(routeId: string): Promise<AircraftOptions> {
    return firstValueFrom(this.http.get<AircraftOptions>(`/api/bookings/routes/${routeId}/aircraft`));
  }

  proposeRandom(duration: FlightDuration): Promise<RandomProposal> {
    const params = new HttpParams().set('duration', duration);
    return firstValueFrom(this.http.get<RandomProposal>('/api/bookings/random', { params }));
  }

  bookRandom(routeId: string, aircraftId: string): Promise<Booking> {
    return this.afterBooking(this.http.post<Booking>('/api/bookings/random', { routeId, aircraftId }));
  }

  bookCustom(routeId: string, aircraftId: string | null): Promise<Booking> {
    return this.afterBooking(this.http.post<Booking>('/api/bookings/custom', { routeId, aircraftId }));
  }

  async cancel(): Promise<void> {
    await firstValueFrom(this.http.delete<void>('/api/bookings/current'));
    this.current.set(null);
    await this.auth.refreshProfile();
  }

  briefing(): Promise<Briefing> {
    return firstValueFrom(this.http.get<Briefing>('/api/bookings/current/briefing'));
  }

  simBrief(): Promise<SimBriefOfp> {
    return firstValueFrom(this.http.get<SimBriefOfp>('/api/bookings/current/simbrief'));
  }

  weather(): Promise<Metar[]> {
    return firstValueFrom(this.http.get<Metar[]>('/api/bookings/current/weather'));
  }

  simBriefGenerate(): Promise<{ url: string }> {
    return firstValueFrom(this.http.post<{ url: string }>('/api/bookings/current/simbrief/generate', {}));
  }

  simBriefPdf(): Promise<Blob> {
    return firstValueFrom(this.http.get('/api/bookings/current/simbrief/pdf', { responseType: 'blob' }));
  }

  roster(): Promise<RosterStatus> {
    return firstValueFrom(this.http.get<RosterStatus>('/api/roster'));
  }

  createRoster(flightsPerDay: number): Promise<RosterStatus> {
    return firstValueFrom(this.http.post<RosterStatus>('/api/roster', { flightsPerDay }));
  }

  legAircraft(legId: string): Promise<AircraftOptions> {
    return firstValueFrom(this.http.get<AircraftOptions>(`/api/roster/legs/${legId}/aircraft`));
  }

  bookLeg(legId: string, aircraftId: string | null): Promise<Booking> {
    return this.afterBooking(this.http.post<Booking>(`/api/roster/legs/${legId}/book`, { aircraftId }));
  }

  fleet(): Promise<FleetAircraft[]> {
    return firstValueFrom(this.http.get<FleetAircraft[]>('/api/fleet'));
  }

  transferQuote(aircraftId: string): Promise<TransferQuote> {
    return firstValueFrom(this.http.get<TransferQuote>(`/api/fleet/${aircraftId}/transfer`));
  }

  async transfer(aircraftId: string): Promise<FleetAircraft> {
    const result = await firstValueFrom(this.http.post<FleetAircraft>(`/api/fleet/${aircraftId}/transfer`, {}));
    await this.auth.refreshProfile();
    return result;
  }

  private async afterBooking(request: Observable<Booking>): Promise<Booking> {
    const booking = await firstValueFrom(request);
    this.current.set(booking);
    await this.auth.refreshProfile();
    return booking;
  }
}