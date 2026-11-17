import { Injectable, signal } from '@angular/core';

export type ConfirmTone = 'danger' | 'warning' | 'success' | 'primary';

export interface ConfirmOptions {
  title: string;
  message?: string;
  details?: string[];
  confirmLabel?: string;
  cancelLabel?: string;
  tone?: ConfirmTone;
  icon?: string;
}

interface ConfirmRequest extends ConfirmOptions {
  resolve: (value: boolean) => void;
}

@Injectable({ providedIn: 'root' })
export class ConfirmService {
  readonly current = signal<ConfirmRequest | null>(null);

  ask(options: ConfirmOptions): Promise<boolean> {
    this.current()?.resolve(false);
    return new Promise(resolve => this.current.set({ ...options, resolve }));
  }

  close(result: boolean): void {
    const request = this.current();
    if (!request) return;
    this.current.set(null);
    request.resolve(result);
  }
}