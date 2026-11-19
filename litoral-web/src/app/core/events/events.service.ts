import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AdminEvent, EventPayload, PilotEvent } from './events.models';

const ADMIN_URL = '/api/admin/events';

@Injectable({ providedIn: 'root' })
export class EventsService {
  private readonly http = inject(HttpClient);

  list(): Promise<PilotEvent[]> {
    return firstValueFrom(this.http.get<PilotEvent[]>('/api/events'));
  }

  adminList(): Promise<AdminEvent[]> {
    return firstValueFrom(this.http.get<AdminEvent[]>(ADMIN_URL));
  }

  create(payload: EventPayload): Promise<AdminEvent> {
    return firstValueFrom(this.http.post<AdminEvent>(ADMIN_URL, payload));
  }

  update(id: string, payload: EventPayload): Promise<AdminEvent> {
    return firstValueFrom(this.http.put<AdminEvent>(`${ADMIN_URL}/${id}`, payload));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${ADMIN_URL}/${id}`));
  }

  uploadImage(id: string, file: File): Promise<AdminEvent> {
    const form = new FormData();
    form.append('file', file);
    return firstValueFrom(this.http.post<AdminEvent>(`${ADMIN_URL}/${id}/image`, form));
  }

  removeImage(id: string): Promise<AdminEvent> {
    return firstValueFrom(this.http.delete<AdminEvent>(`${ADMIN_URL}/${id}/image`));
  }
}