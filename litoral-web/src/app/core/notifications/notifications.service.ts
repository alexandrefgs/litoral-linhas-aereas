import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface AppNotification {
  id: string;
  type: string;
  title: string;
  message: string;
  link: string | null;
  createdAt: string;
  isRead: boolean;
}

const POLL_MS = 60_000;

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly http = inject(HttpClient);
  private timer?: ReturnType<typeof setInterval>;

  readonly unread = signal(0);
  readonly items = signal<AppNotification[]>([]);
  readonly loading = signal(false);

  start(): void {
    this.stop();
    this.refreshCount();
    this.timer = setInterval(() => this.refreshCount(), POLL_MS);
  }

  stop(): void {
    clearInterval(this.timer);
    this.timer = undefined;
    this.unread.set(0);
    this.items.set([]);
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await firstValueFrom(this.http.get<{ unread: number; items: AppNotification[] }>('/api/notifications'));
      this.items.set(result.items);
      this.unread.set(result.unread);
    } catch {
      this.items.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  async markRead(item: AppNotification): Promise<void> {
    if (item.isRead) return;
    this.items.update(list => list.map(n => (n.id === item.id ? { ...n, isRead: true } : n)));
    this.unread.update(n => Math.max(0, n - 1));
    try {
      await firstValueFrom(this.http.post(`/api/notifications/${item.id}/read`, null));
    } catch {
      this.refreshCount();
    }
  }

  async markAllRead(): Promise<void> {
    this.items.update(list => list.map(n => ({ ...n, isRead: true })));
    this.unread.set(0);
    try {
      await firstValueFrom(this.http.post('/api/notifications/read-all', null));
    } catch {
      this.refreshCount();
    }
  }

  private async refreshCount(): Promise<void> {
    try {
      this.unread.set(await firstValueFrom(this.http.get<number>('/api/notifications/unread')));
    } catch {
      // ignore transient errors, the next poll will retry
    }
  }
}