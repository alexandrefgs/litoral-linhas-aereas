import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AdminDownloadItem, DownloadItem, DownloadPayload } from './downloads.models';

const ADMIN_URL = '/api/admin/downloads';

@Injectable({ providedIn: 'root' })
export class DownloadsService {
  private readonly http = inject(HttpClient);

  list(): Promise<DownloadItem[]> {
    return firstValueFrom(this.http.get<DownloadItem[]>('/api/downloads'));
  }

  async download(item: DownloadItem): Promise<void> {
    if (!item.isFile) {
      const { url } = await firstValueFrom(this.http.post<{ url: string }>(`/api/downloads/${item.id}/link`, {}));
      window.open(url, '_blank', 'noopener');
      return;
    }

    const blob = await firstValueFrom(this.http.get(`/api/downloads/${item.id}/file`, { responseType: 'blob' }));
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = item.fileName ?? 'file';
    link.click();
    URL.revokeObjectURL(url);
  }

  adminList(): Promise<AdminDownloadItem[]> {
    return firstValueFrom(this.http.get<AdminDownloadItem[]>(ADMIN_URL));
  }

  create(payload: DownloadPayload): Promise<AdminDownloadItem> {
    return firstValueFrom(this.http.post<AdminDownloadItem>(ADMIN_URL, this.form(payload)));
  }

  update(id: string, payload: DownloadPayload): Promise<AdminDownloadItem> {
    return firstValueFrom(this.http.put<AdminDownloadItem>(`${ADMIN_URL}/${id}`, this.form(payload)));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${ADMIN_URL}/${id}`));
  }

  private form(payload: DownloadPayload): FormData {
    const form = new FormData();
    form.append('title', payload.title);
    form.append('category', payload.category);
    if (payload.description) form.append('description', payload.description);
    if (payload.aircraftType) form.append('aircraftType', payload.aircraftType);
    if (payload.version) form.append('version', payload.version);
    if (payload.file) form.append('file', payload.file);
    else if (payload.externalUrl) form.append('externalUrl', payload.externalUrl);
    return form;
  }
}