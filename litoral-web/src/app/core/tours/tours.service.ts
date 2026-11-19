import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AircraftOptions, Booking } from '../booking/booking.models';
import { BookingService } from '../booking/booking.service';
import { AdminTour, PilotTour, TourOptions, TourPayload } from './tours.models';

const ADMIN_URL = '/api/admin/tours';

@Injectable({ providedIn: 'root' })
export class ToursService {
  private readonly http = inject(HttpClient);
  private readonly booking = inject(BookingService);

  list(): Promise<PilotTour[]> {
    return firstValueFrom(this.http.get<PilotTour[]>('/api/tours'));
  }

  aircraft(id: string): Promise<AircraftOptions> {
    return firstValueFrom(this.http.get<AircraftOptions>(`/api/tours/${id}/aircraft`));
  }

  async book(id: string, aircraftId: string | null): Promise<Booking> {
    const result = await firstValueFrom(this.http.post<Booking>(`/api/tours/${id}/book`, { aircraftId }));
    await this.booking.refreshCurrent().catch(() => undefined);
    return result;
  }

  options(): Promise<TourOptions> {
    return firstValueFrom(this.http.get<TourOptions>(`${ADMIN_URL}/options`));
  }

  adminList(): Promise<AdminTour[]> {
    return firstValueFrom(this.http.get<AdminTour[]>(ADMIN_URL));
  }

  create(payload: TourPayload): Promise<AdminTour> {
    return firstValueFrom(this.http.post<AdminTour>(ADMIN_URL, payload));
  }

  update(id: string, payload: TourPayload): Promise<AdminTour> {
    return firstValueFrom(this.http.put<AdminTour>(`${ADMIN_URL}/${id}`, payload));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${ADMIN_URL}/${id}`));
  }

  uploadImage(id: string, file: File): Promise<AdminTour> {
    const form = new FormData();
    form.append('file', file);
    return firstValueFrom(this.http.post<AdminTour>(`${ADMIN_URL}/${id}/image`, form));
  }

  removeImage(id: string): Promise<AdminTour> {
    return firstValueFrom(this.http.delete<AdminTour>(`${ADMIN_URL}/${id}/image`));
  }
}