import { NgTemplateOutlet } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toDataURL } from 'qrcode';
import { apiErrorMessage } from '../../core/api/api-error';
import { TwoFactorSetup, TwoFactorStatus } from '../../core/auth/auth.models';
import { AuthService } from '../../core/auth/auth.service';
import { TwoFactorService } from '../../core/auth/two-factor.service';

type Mode = 'idle' | 'setup' | 'codes' | 'regenerate' | 'disable';

@Component({
  selector: 'app-two-factor-panel',
  imports: [NgTemplateOutlet, ReactiveFormsModule],
  template: `
    <div class="panel">
      <div class="flex flex-wrap items-center justify-between gap-3 border-b border-line px-6 py-4">
        <h2 class="flex items-center gap-2 text-lg font-semibold text-ink">
          <i class="ti ti-shield-lock text-primary" aria-hidden="true"></i>Two-factor authentication
        </h2>
        @if (status(); as s) {
          @if (s.enabled) {
            <span class="flex items-center gap-1 rounded-full bg-onda-500/15 px-2.5 py-1 text-xs font-semibold text-onda-600">
              <i class="ti ti-shield-check" aria-hidden="true"></i>Enabled
            </span>
          } @else {
            <span class="rounded-full bg-surface-alt px-2.5 py-1 text-xs font-semibold text-ink-muted">Disabled</span>
          }
        }
      </div>

      <div class="flex flex-col gap-5 px-6 py-5">
        @if (!status()) {
          <p class="text-sm text-ink-muted">{{ loadError() ? 'Could not load the two-factor settings.' : 'Loading...' }}</p>
        } @else {
          @let s = status()!;

          @if (s.required && !s.enabled) {
            <p class="flex gap-2 rounded-lg bg-highlight/20 px-4 py-3 text-sm text-ink">
              <i class="ti ti-alert-triangle mt-0.5 shrink-0" aria-hidden="true"></i>
              Two-factor authentication is required for staff members. Enable it to access the Admin area.
            </p>
          }

          @switch (mode()) {
            @case ('setup') {
              @if (setup(); as st) {
                <div class="grid gap-6 md:grid-cols-[auto_1fr]">
                  <div class="flex flex-col items-center gap-2">
                    @if (qrCode()) {
                      <img [src]="qrCode()" alt="QR code for the authenticator app" class="h-44 w-44 rounded-lg border border-line bg-white p-2" />
                    }
                  </div>
                  <div class="flex flex-col gap-4 text-sm">
                    <div>
                      <p class="font-semibold text-ink">1. Scan the QR code</p>
                      <p class="text-ink-muted">Use Google Authenticator, Microsoft Authenticator, Authy or any other authenticator app.</p>
                      <p class="mt-2 text-ink-muted">Can't scan it? Enter this key in the app:</p>
                      <p class="mt-1 select-all rounded-md bg-surface-alt px-3 py-2 font-mono text-ink">{{ st.sharedKey }}</p>
                    </div>
                    <form [formGroup]="enableForm" (ngSubmit)="enable()" class="flex flex-col gap-2" novalidate>
                      <label for="enableCode" class="font-semibold text-ink">2. Enter the 6-digit code from the app</label>
                      <div class="flex flex-wrap gap-2">
                        <input id="enableCode" formControlName="code" inputmode="numeric" maxlength="6" autocomplete="one-time-code"
                          placeholder="000000" class="field-input w-40 text-center font-mono text-lg tracking-[0.3em]" />
                        <button type="submit" class="btn-primary px-5" [disabled]="busy() || enableForm.invalid">
                          {{ busy() ? 'Verifying...' : 'Verify and enable' }}
                        </button>
                        <button type="button" class="btn-secondary" (click)="cancel()">Cancel</button>
                      </div>
                    </form>
                  </div>
                </div>
              }
            }

            @case ('codes') {
              <div class="flex flex-col gap-4">
                <p class="flex gap-2 rounded-lg bg-highlight/20 px-4 py-3 text-sm text-ink">
                  <i class="ti ti-key mt-0.5 shrink-0" aria-hidden="true"></i>
                  Save these recovery codes in a safe place. Each code works only once and lets you sign in if you lose your phone.
                  They will not be shown again.
                </p>
                <ul class="grid grid-cols-2 gap-2 sm:grid-cols-5">
                  @for (code of codes(); track code) {
                    <li class="rounded-md bg-surface-alt px-3 py-2 text-center font-mono text-sm text-ink">{{ code }}</li>
                  }
                </ul>
                <div class="flex flex-wrap gap-2">
                  <button type="button" class="btn-secondary flex items-center gap-2" (click)="copyCodes()">
                    <i class="ti ti-copy" aria-hidden="true"></i>{{ copied() ? 'Copied' : 'Copy' }}
                  </button>
                  <button type="button" class="btn-secondary flex items-center gap-2" (click)="downloadCodes()">
                    <i class="ti ti-download" aria-hidden="true"></i>Download
                  </button>
                  <button type="button" class="btn-primary px-5" (click)="finishCodes()">I saved my codes</button>
                </div>
              </div>
            }

            @case ('regenerate') {
              <ng-container [ngTemplateOutlet]="confirmForm" />
            }

            @case ('disable') {
              <ng-container [ngTemplateOutlet]="confirmForm" />
            }

            @default {
              @if (s.enabled) {
                <div class="flex flex-wrap items-center justify-between gap-4">
                  <div class="text-sm">
                    <p class="text-ink">Your account asks for a code from your authenticator app when you sign in on a new device.</p>
                    <p class="mt-1" [class]="s.recoveryCodesLeft <= 3 ? 'font-medium text-danger' : 'text-ink-muted'">
                      {{ s.recoveryCodesLeft }} recovery {{ s.recoveryCodesLeft === 1 ? 'code' : 'codes' }} left.
                    </p>
                  </div>
                  <div class="flex flex-wrap gap-2">
                    <button type="button" class="btn-secondary" (click)="open('regenerate')">New recovery codes</button>
                    <button type="button" class="rounded-md border border-danger px-4 py-2 text-sm font-medium text-danger hover:bg-danger hover:text-white"
                      (click)="open('disable')">Disable</button>
                  </div>
                </div>
              } @else {
                <div class="flex flex-wrap items-center justify-between gap-4">
                  <p class="max-w-2xl text-sm text-ink">
                    Protect your account with a code from an authenticator app on your phone, asked every time you sign in on a new device.
                  </p>
                  <button type="button" class="btn-primary px-5 py-2.5" [disabled]="busy()" (click)="startSetup()">
                    {{ busy() ? 'Loading...' : 'Enable two-factor' }}
                  </button>
                </div>
              }
            }
          }

          @if (message(); as m) {
            <p class="rounded-md px-3 py-2 text-sm" [class]="m.ok ? 'bg-accent/15 text-ink' : 'bg-danger/10 text-danger'">{{ m.text }}</p>
          }
        }
      </div>
    </div>

    <ng-template #confirmForm>
      <form [formGroup]="confirmFormGroup" (ngSubmit)="confirmAction()" class="flex flex-col gap-4" novalidate>
        <p class="text-sm text-ink">
          {{ mode() === 'disable'
            ? 'Confirm your password and a code to disable two-factor authentication. Trusted devices will need a code again if you enable it later.'
            : 'Confirm your password and a code to create 10 new recovery codes. The old codes stop working.' }}
        </p>
        <div class="grid gap-4 sm:grid-cols-2">
          <div>
            <label for="tfPassword" class="field-label">Password</label>
            <input id="tfPassword" type="password" formControlName="password" autocomplete="current-password" class="field-input" />
          </div>
          <div>
            <label for="tfCode" class="field-label">Code from the app or a recovery code</label>
            <input id="tfCode" formControlName="code" autocomplete="one-time-code" maxlength="11" class="field-input font-mono" />
          </div>
        </div>
        <div class="flex flex-wrap gap-2">
          @if (mode() === 'disable') {
            <button type="submit" class="rounded-md bg-danger px-5 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50"
              [disabled]="busy() || confirmFormGroup.invalid">{{ busy() ? 'Disabling...' : 'Disable two-factor' }}</button>
          } @else {
            <button type="submit" class="btn-primary px-5" [disabled]="busy() || confirmFormGroup.invalid">
              {{ busy() ? 'Creating...' : 'Create new codes' }}
            </button>
          }
          <button type="button" class="btn-secondary" (click)="cancel()">Cancel</button>
        </div>
      </form>
    </ng-template>
  `
})
export class TwoFactorPanel implements OnInit {
  private readonly api = inject(TwoFactorService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder).nonNullable;

  protected readonly status = signal<TwoFactorStatus | null>(null);
  protected readonly loadError = signal(false);
  protected readonly mode = signal<Mode>('idle');
  protected readonly setup = signal<TwoFactorSetup | null>(null);
  protected readonly qrCode = signal<string | null>(null);
  protected readonly codes = signal<string[]>([]);
  protected readonly busy = signal(false);
  protected readonly copied = signal(false);
  protected readonly message = signal<{ ok: boolean; text: string } | null>(null);

  protected readonly enableForm = this.fb.group({
    code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
  });

  protected readonly confirmFormGroup = this.fb.group({
    password: ['', Validators.required],
    code: ['', [Validators.required, Validators.minLength(6)]]
  });

  async ngOnInit(): Promise<void> {
    try {
      this.status.set(await this.api.status());
    } catch {
      this.loadError.set(true);
    }
  }

  protected async startSetup(): Promise<void> {
    await this.run(async () => {
      const setup = await this.api.setup();
      this.setup.set(setup);
      this.qrCode.set(await toDataURL(setup.authenticatorUri, { width: 352, margin: 1 }));
      this.enableForm.reset();
      this.mode.set('setup');
    });
  }

  protected async enable(): Promise<void> {
    await this.run(async () => {
      const result = await this.api.enable(this.enableForm.controls.code.value);
      this.codes.set(result.recoveryCodes);
      this.setup.set(null);
      this.qrCode.set(null);
      this.mode.set('codes');
      await this.reload();
    });
  }

  protected open(mode: 'regenerate' | 'disable'): void {
    this.confirmFormGroup.reset();
    this.message.set(null);
    this.mode.set(mode);
  }

  protected async confirmAction(): Promise<void> {
    const { password, code } = this.confirmFormGroup.getRawValue();

    await this.run(async () => {
      if (this.mode() === 'disable') {
        await this.api.disable(password, code);
        this.auth.forgetTrustedDevice();
        this.mode.set('idle');
        await this.reload();
        this.message.set({ ok: true, text: 'Two-factor authentication is disabled.' });
      } else {
        const result = await this.api.regenerateRecoveryCodes(password, code);
        this.codes.set(result.recoveryCodes);
        this.mode.set('codes');
        await this.reload();
      }
    });
  }

  protected cancel(): void {
    this.mode.set('idle');
    this.setup.set(null);
    this.qrCode.set(null);
    this.message.set(null);
  }

  protected async copyCodes(): Promise<void> {
    try {
      await navigator.clipboard.writeText(this.codes().join('\n'));
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    } catch {}
  }

  protected downloadCodes(): void {
    const text = `Litoral Linhas Aéreas - recovery codes\n${this.auth.pilot()?.email ?? ''}\n\n${this.codes().join('\n')}\n`;
    const url = URL.createObjectURL(new Blob([text], { type: 'text/plain' }));
    const link = document.createElement('a');
    link.href = url;
    link.download = 'litoral-recovery-codes.txt';
    link.click();
    URL.revokeObjectURL(url);
  }

  protected finishCodes(): void {
    this.codes.set([]);
    this.mode.set('idle');
  }

  private async reload(): Promise<void> {
    this.status.set(await this.api.status());
    await this.auth.refreshProfile();
  }

  private async run(action: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    this.message.set(null);
    try {
      await action();
    } catch (error) {
      this.message.set({ ok: false, text: apiErrorMessage(error) ?? 'Something went wrong. Try again.' });
    } finally {
      this.busy.set(false);
    }
  }
}