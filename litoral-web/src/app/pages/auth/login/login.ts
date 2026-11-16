import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { apiErrorStatus } from '../../../core/api/api-error';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  template: `
    <section class="mx-auto flex max-w-md flex-col gap-6 px-6 py-16">
      <div class="text-center">
        <h1 class="text-3xl font-bold text-ink">{{ 'auth.login.title' | t }}</h1>
        <p class="mt-2 text-ink-muted">{{ 'auth.login.subtitle' | t }}</p>
      </div>

      <form [formGroup]="form" (ngSubmit)="submit()" class="card flex flex-col gap-4" novalidate>
        <div>
          <label for="email" class="field-label">{{ 'auth.fields.email' | t }}</label>
          <input id="email" type="email" formControlName="email" autocomplete="email" class="field-input" />
          @if (showError('email')) {
            <p class="field-error">{{ (form.controls.email.hasError('required') ? 'auth.errors.required' : 'auth.errors.email') | t }}</p>
          }
        </div>

        <div>
          <label for="password" class="field-label">{{ 'auth.fields.password' | t }}</label>
          <input id="password" type="password" formControlName="password" autocomplete="current-password" class="field-input" />
          @if (showError('password')) {
            <p class="field-error">{{ 'auth.errors.required' | t }}</p>
          }
        </div>

        @if (errorKey(); as key) {
          <p class="rounded-md bg-danger/10 px-3 py-2 text-sm text-danger">{{ key | t }}</p>
        }

        <button type="submit" class="btn-primary w-full py-2.5" [disabled]="loading()">
          {{ (loading() ? 'common.loading' : 'auth.login.submit') | t }}
        </button>
      </form>

      <p class="text-center text-sm text-ink-muted">
        {{ 'auth.login.noAccount' | t }}
        <a routerLink="/register" class="font-medium text-primary hover:underline">{{ 'auth.login.createAccount' | t }}</a>
      </p>
    </section>
  `
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly loading = signal(false);
  protected readonly errorKey = signal<string | null>(null);
  protected readonly submitted = signal(false);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  protected showError(name: 'email' | 'password'): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || this.submitted());
  }

  protected async submit(): Promise<void> {
    this.submitted.set(true);
    this.errorKey.set(null);
    if (this.form.invalid) return;

    this.loading.set(true);
    try {
      await this.auth.login(this.form.getRawValue());
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard';
      await this.router.navigateByUrl(returnUrl);
    } catch (error) {
      this.errorKey.set(apiErrorStatus(error) === 401 ? 'auth.errors.invalidCredentials' : 'auth.errors.generic');
    } finally {
      this.loading.set(false);
    }
  }
}