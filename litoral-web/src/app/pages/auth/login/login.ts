import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApplicationStatus } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { apiErrorMessage, apiErrorStatus } from '../../../core/api/api-error';

@Component({
  selector: 'app-login',
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  template: `
    <section class="mx-auto flex max-w-md flex-col gap-6 px-6 py-16">
      @if (application(); as app) {
        <div class="card flex flex-col items-center gap-4 text-center">
          <span class="flex h-14 w-14 items-center justify-center rounded-full bg-highlight/25 text-3xl text-ink">
            <i class="ti ti-hourglass" aria-hidden="true"></i>
          </span>
          <h1 class="text-2xl font-bold text-ink">Application under review</h1>
          <p class="text-ink-muted">
            Hi {{ app.firstName }}, your application was sent on {{ app.submittedAt | date: 'd MMM y' }} and is waiting for our HR team.
            When it is approved, you can sign in and fly with Litoral.
          </p>
          <button type="button" class="btn-secondary" (click)="application.set(null)">Back</button>
        </div>
      } @else if (challenge()) {
        <div class="text-center">
          <span class="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-primary/10 text-3xl text-primary">
            <i class="ti ti-shield-lock" aria-hidden="true"></i>
          </span>
          <h1 class="mt-4 text-3xl font-bold text-ink">Two-factor authentication</h1>
          <p class="mt-2 text-ink-muted">
            {{ useRecovery() ? 'Enter one of your recovery codes.' : 'Enter the 6-digit code from your authenticator app.' }}
          </p>
        </div>

        <form [formGroup]="codeForm" (ngSubmit)="verify()" class="card flex flex-col gap-4" novalidate>
          <div>
            <label for="code" class="field-label">{{ useRecovery() ? 'Recovery code' : 'Authentication code' }}</label>
            <input id="code" formControlName="code" class="field-input text-center font-mono text-xl tracking-[0.3em]"
              [attr.inputmode]="useRecovery() ? 'text' : 'numeric'" [attr.maxlength]="useRecovery() ? 11 : 6"
              [placeholder]="useRecovery() ? 'XXXXX-XXXXX' : '000000'" autocomplete="one-time-code" />
          </div>

          <label class="flex cursor-pointer items-center gap-3 text-sm text-ink">
            <input type="checkbox" formControlName="rememberDevice" class="h-4 w-4 accent-primary" />
            Trust this device for 30 days
          </label>

          @if (errorMessage(); as text) {
            <p class="rounded-md bg-danger/10 px-3 py-2 text-sm text-danger">{{ text }}</p>
          }

          <button type="submit" class="btn-primary w-full py-2.5" [disabled]="loading() || codeForm.invalid">
            {{ loading() ? 'Verifying...' : 'Verify' }}
          </button>

          <div class="flex items-center justify-between text-sm">
            <button type="button" class="font-medium text-primary hover:underline" (click)="toggleRecovery()">
              {{ useRecovery() ? 'Use the authenticator app' : 'Use a recovery code' }}
            </button>
            <button type="button" class="text-ink-muted hover:text-ink" (click)="backToPassword()">Back</button>
          </div>
        </form>

        <p class="text-center text-sm text-ink-muted">Lost access to your app and your recovery codes? Contact the staff.</p>
      } @else {
        <div class="text-center">
          <h1 class="text-3xl font-bold text-ink">Sign in</h1>
          <p class="mt-2 text-ink-muted">Access your pilot account.</p>
        </div>

        <form [formGroup]="form" (ngSubmit)="submit()" class="card flex flex-col gap-4" novalidate>
          <div>
            <label for="email" class="field-label">Email</label>
            <input id="email" type="email" formControlName="email" autocomplete="email" class="field-input" />
            @if (showError('email')) {
              <p class="field-error">{{ form.controls.email.hasError('required') ? 'Required field.' : 'Invalid email.' }}</p>
            }
          </div>

          <div>
            <label for="password" class="field-label">Password</label>
            <input id="password" type="password" formControlName="password" autocomplete="current-password" class="field-input" />
            @if (showError('password')) {
              <p class="field-error">Required field.</p>
            }
          </div>

          @if (errorMessage(); as text) {
            <p class="rounded-md bg-danger/10 px-3 py-2 text-sm text-danger">{{ text }}</p>
          }

          <button type="submit" class="btn-primary w-full py-2.5" [disabled]="loading()">
            {{ loading() ? 'Loading...' : 'Sign in' }}
          </button>
        </form>

        <p class="text-center text-sm text-ink-muted">
          Want to fly with us?
          <a routerLink="/regulations" fragment="join" class="font-medium text-primary hover:underline">Read the Regulations and apply</a>
        </p>
      }
    </section>
  `
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder).nonNullable;

  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly submitted = signal(false);
  protected readonly challenge = signal<string | null>(null);
  protected readonly useRecovery = signal(false);
  protected readonly application = signal<ApplicationStatus | null>(null);

  protected readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  protected readonly codeForm = this.fb.group({
    code: ['', [Validators.required, Validators.minLength(6)]],
    rememberDevice: [false]
  });

  protected showError(name: 'email' | 'password'): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || this.submitted());
  }

  protected async submit(): Promise<void> {
    this.submitted.set(true);
    this.errorMessage.set(null);
    if (this.form.invalid) return;

    this.loading.set(true);
    try {
      const result = await this.auth.login(this.form.getRawValue());
      if (result.kind === 'session') {
        await this.goToReturnUrl();
      } else if (result.kind === 'application') {
        this.application.set(result.application);
      } else {
        this.codeForm.reset();
        this.useRecovery.set(false);
        this.challenge.set(result.challengeToken);
      }
    } catch (error) {
      this.errorMessage.set(apiErrorStatus(error) === 401 ? (apiErrorMessage(error) ?? 'Invalid email or password.') : 'Something went wrong. Try again.');
    } finally {
      this.loading.set(false);
    }
  }

  protected async verify(): Promise<void> {
    const challenge = this.challenge();
    if (!challenge || this.codeForm.invalid) return;

    this.errorMessage.set(null);
    this.loading.set(true);
    try {
      const { code, rememberDevice } = this.codeForm.getRawValue();
      await this.auth.loginWithTwoFactor(this.form.controls.email.value, challenge, code, rememberDevice);
      await this.goToReturnUrl();
    } catch (error) {
      const status = apiErrorStatus(error);
      if (status === 401) {
        this.backToPassword();
        this.errorMessage.set(apiErrorMessage(error) ?? 'Your sign-in expired. Sign in again.');
      } else {
        this.errorMessage.set(status === 400 ? (apiErrorMessage(error) ?? 'Invalid code.') : 'Something went wrong. Try again.');
      }
    } finally {
      this.loading.set(false);
    }
  }

  protected toggleRecovery(): void {
    this.useRecovery.update(v => !v);
    this.codeForm.controls.code.reset();
    this.errorMessage.set(null);
  }

  protected backToPassword(): void {
    this.challenge.set(null);
    this.form.controls.password.reset();
    this.submitted.set(false);
    this.errorMessage.set(null);
  }

  private async goToReturnUrl(): Promise<void> {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard';
    await this.router.navigateByUrl(returnUrl);
  }
}