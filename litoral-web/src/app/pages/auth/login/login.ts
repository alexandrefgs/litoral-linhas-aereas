import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { apiErrorStatus } from '../../../core/api/api-error';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <section class="mx-auto flex max-w-md flex-col gap-6 px-6 py-16">
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
        Don't have an account yet?
        <a routerLink="/register" class="font-medium text-primary hover:underline">Create account</a>
      </p>
    </section>
  `
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
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
    this.errorMessage.set(null);
    if (this.form.invalid) return;

    this.loading.set(true);
    try {
      await this.auth.login(this.form.getRawValue());
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard';
      await this.router.navigateByUrl(returnUrl);
    } catch (error) {
      this.errorMessage.set(apiErrorStatus(error) === 401 ? 'Invalid email or password.' : 'Something went wrong. Try again.');
    } finally {
      this.loading.set(false);
    }
  }
}