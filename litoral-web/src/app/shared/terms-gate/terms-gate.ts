import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { apiErrorMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { PrivacyService } from '../../core/privacy/privacy.service';

@Component({
  selector: 'app-terms-gate',
  imports: [RouterLink],
  template: `
    @if (visible()) {
      <div class="fixed inset-0 z-[1250] flex items-center justify-center bg-black/60 p-4" role="dialog" aria-modal="true" aria-labelledby="terms-title">
        <div class="panel w-full max-w-lg p-6">
          <span class="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-2xl text-primary">
            <i class="ti ti-file-text" aria-hidden="true"></i>
          </span>
          <h2 id="terms-title" class="mt-4 text-center text-xl font-bold text-ink">Our rules were updated</h2>
          <p class="mt-2 text-center text-sm text-ink-muted">
            To keep using Litoral Linhas Aéreas, read and accept the current versions of the Regulations and the Privacy Policy.
          </p>

          <div class="mt-5 flex flex-col gap-2">
            <a routerLink="/regulations" target="_blank" class="flex items-center justify-between rounded-lg border border-line px-4 py-3 text-sm font-medium text-ink hover:bg-surface-alt">
              <span class="flex items-center gap-2"><i class="ti ti-book text-primary" aria-hidden="true"></i>Regulations</span>
              <i class="ti ti-external-link text-ink-muted" aria-hidden="true"></i>
            </a>
            <a routerLink="/privacy" target="_blank" class="flex items-center justify-between rounded-lg border border-line px-4 py-3 text-sm font-medium text-ink hover:bg-surface-alt">
              <span class="flex items-center gap-2"><i class="ti ti-lock text-primary" aria-hidden="true"></i>Privacy Policy</span>
              <i class="ti ti-external-link text-ink-muted" aria-hidden="true"></i>
            </a>
          </div>

          <label class="mt-5 flex cursor-pointer items-start gap-3 text-sm text-ink">
            <input type="checkbox" class="mt-0.5 h-4 w-4 shrink-0 accent-primary" [checked]="agreed()" (change)="agreed.set(!agreed())" />
            I have read and agree to the Regulations and the Privacy Policy.
          </label>

          @if (error(); as text) {
            <p class="mt-3 rounded-md bg-danger/10 px-3 py-2 text-sm text-danger">{{ text }}</p>
          }

          <div class="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <button type="button" class="btn-secondary" (click)="auth.logout()">Sign out</button>
            <button type="button" class="btn-primary px-5" [disabled]="!agreed() || busy()" (click)="accept()">
              {{ busy() ? 'Saving...' : 'Accept and continue' }}
            </button>
          </div>
        </div>
      </div>
    }
  `
})
export class TermsGate {
  protected readonly auth = inject(AuthService);
  private readonly privacy = inject(PrivacyService);

  protected readonly agreed = signal(false);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly visible = computed(() => {
    const pilot = this.auth.pilot();
    return !!pilot && !pilot.termsAccepted;
  });

  protected async accept(): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    try {
      await this.privacy.acceptTerms();
      await this.auth.refreshProfile();
      this.agreed.set(false);
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.busy.set(false);
    }
  }
}