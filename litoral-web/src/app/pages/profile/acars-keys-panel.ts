import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { apiErrorMessage } from '../../core/api/api-error';
import { AcarsKey, AcarsKeysService, MAX_ACARS_KEYS } from '../../core/profile/acars-keys.service';
import { ConfirmService } from '../../shared/confirm-dialog/confirm.service';

@Component({
  selector: 'app-acars-keys-panel',
  imports: [DatePipe],
  template: `
    <div class="panel">
      <div class="flex flex-wrap items-center justify-between gap-3 border-b border-line px-6 py-4">
        <h2 class="flex items-center gap-2 text-lg font-semibold text-ink">
          <i class="ti ti-key text-primary" aria-hidden="true"></i>ACARS keys
        </h2>
        <span class="text-xs text-ink-muted">{{ keys().length }} of {{ max }}</span>
      </div>

      <div class="flex flex-col gap-4 px-6 py-5">
        <p class="text-sm text-ink-muted">
          The Litoral ACARS signs in with a personal key, so it never asks for your password or two-factor code.
          A key only works for flight tracking. Changing your password revokes all keys.
        </p>

        @if (created(); as secret) {
          <div class="flex flex-col gap-3 rounded-lg border border-highlight bg-highlight/15 p-4 text-sm">
            <p class="flex items-center gap-2 font-semibold text-ink">
              <i class="ti ti-alert-triangle" aria-hidden="true"></i>Copy the key now. It won't be shown again.
            </p>
            <div class="flex flex-wrap items-center gap-2">
              <code class="min-w-0 flex-1 select-all break-all rounded-md bg-surface px-3 py-2 font-mono text-xs text-ink">{{ secret }}</code>
              <button type="button" class="btn-secondary flex items-center gap-2" (click)="copy(secret)">
                <i class="ti ti-copy" aria-hidden="true"></i>{{ copied() ? 'Copied' : 'Copy' }}
              </button>
            </div>
            <button type="button" class="self-start text-xs font-semibold text-primary hover:underline" (click)="created.set(null)">I saved it</button>
          </div>
        }

        @if (error(); as message) {
          <p class="rounded-lg bg-danger/10 px-4 py-3 text-sm text-danger">{{ message }}</p>
        }

        @if (loading()) {
          <p class="text-sm text-ink-muted">Loading...</p>
        } @else {
          @for (k of keys(); track k.id) {
            <div class="flex flex-wrap items-center gap-3 rounded-lg border border-line px-4 py-3">
              <i class="ti ti-device-desktop text-xl text-ink-muted" aria-hidden="true"></i>
              <div class="min-w-0 flex-1">
                <p class="font-semibold text-ink">{{ k.name }} <span class="font-mono text-xs font-normal text-ink-muted">{{ k.prefix }}…</span></p>
                <p class="text-xs text-ink-muted">
                  Created {{ k.createdAt | date: 'd MMM y' }} ·
                  {{ k.lastUsedAt ? 'last used ' + (k.lastUsedAt | date: 'd MMM y, HH:mm') : 'never used' }}
                </p>
              </div>
              <button type="button" class="rounded-md border border-danger px-3 py-1.5 text-sm font-medium text-danger hover:bg-danger hover:text-white" (click)="revoke(k)">
                Revoke
              </button>
            </div>
          } @empty {
            <p class="rounded-lg bg-surface-alt px-4 py-3 text-sm text-ink-muted">You don't have any ACARS key yet.</p>
          }

          @if (keys().length < max) {
            <div class="flex flex-wrap items-end gap-2">
              <div class="min-w-[200px] flex-1">
                <label for="acarsKeyName" class="field-label">New key name</label>
                <input id="acarsKeyName" maxlength="40" class="field-input" placeholder="e.g. Home PC" [value]="name()" (input)="name.set($any($event.target).value)" />
              </div>
              <button type="button" class="btn-primary flex items-center gap-2" [disabled]="busy() || !name().trim()" (click)="create()">
                <i class="ti ti-plus" aria-hidden="true"></i>{{ busy() ? 'Creating...' : 'Create key' }}
              </button>
            </div>
          }
        }
      </div>
    </div>
  `
})
export class AcarsKeysPanel implements OnInit {
  private readonly api = inject(AcarsKeysService);
  private readonly confirm = inject(ConfirmService);

  protected readonly max = MAX_ACARS_KEYS;
  protected readonly keys = signal<AcarsKey[]>([]);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly name = signal('');
  protected readonly created = signal<string | null>(null);
  protected readonly copied = signal(false);

  async ngOnInit(): Promise<void> {
    try {
      this.keys.set(await this.api.list());
    } catch {
      this.error.set('Could not load your ACARS keys.');
    } finally {
      this.loading.set(false);
    }
  }

  protected async create(): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    try {
      const result = await this.api.create(this.name().trim());
      this.keys.update(list => [...list, result.key]);
      this.created.set(result.secret);
      this.copied.set(false);
      this.name.set('');
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Could not create the key.');
    } finally {
      this.busy.set(false);
    }
  }

  protected async revoke(key: AcarsKey): Promise<void> {
    const ok = await this.confirm.ask({
      title: `Revoke "${key.name}"?`,
      message: 'The ACARS using this key stops working right away. A flight in progress on it can\'t be sent until you sign in again with a new key.',
      confirmLabel: 'Revoke key',
      tone: 'danger',
      icon: 'ti-key-off'
    });
    if (!ok) return;

    this.error.set(null);
    try {
      await this.api.revoke(key.id);
      this.keys.update(list => list.filter(k => k.id !== key.id));
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Could not revoke the key.');
    }
  }

  protected async copy(secret: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(secret);
      this.copied.set(true);
    } catch {}
  }
}