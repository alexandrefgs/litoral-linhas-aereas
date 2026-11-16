import { Component, OnInit, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { apiErrorStatus } from '../../../core/api/api-error';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Hub } from '../../../core/reference/reference.models';
import { ReferenceService } from '../../../core/reference/reference.service';

const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/;

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return password && confirm && password !== confirm ? { passwordMismatch: true } : null;
}

type Field = 'firstName' | 'lastName' | 'email' | 'password' | 'confirmPassword';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  template: `
    <section class="mx-auto flex max-w-lg flex-col gap-6 px-6 py-16">
      <div class="text-center">
        <h1 class="text-3xl font-bold text-ink">{{ 'auth.register.title' | t }}</h1>
        <p class="mt-2 text-ink-muted">{{ 'auth.register.subtitle' | t }}</p>
      </div>

      <form [formGroup]="form" (ngSubmit)="submit()" class="card flex flex-col gap-4" novalidate>
        <div class="grid gap-4 sm:grid-cols-2">
          <div>
            <label for="firstName" class="field-label">{{ 'auth.fields.firstName' | t }}</label>
            <input id="firstName" formControlName="firstName" autocomplete="given-name" class="field-input" />
            @if (showError('firstName')) {
              <p class="field-error">{{ 'auth.errors.required' | t }}</p>
            }
          </div>
          <div>
            <label for="lastName" class="field-label">{{ 'auth.fields.lastName' | t }}</label>
            <input id="lastName" formControlName="lastName" autocomplete="family-name" class="field-input" />
            @if (showError('lastName')) {
              <p class="field-error">{{ 'auth.errors.required' | t }}</p>
            }
          </div>
        </div>

        <div>
          <label for="email" class="field-label">{{ 'auth.fields.email' | t }}</label>
          <input id="email" type="email" formControlName="email" autocomplete="email" class="field-input" />
          @if (showError('email')) {
            <p class="field-error">{{ (form.controls.email.hasError('required') ? 'auth.errors.required' : 'auth.errors.email') | t }}</p>
          }
        </div>

        <div>
          <label for="password" class="field-label">{{ 'auth.fields.password' | t }}</label>
          <input id="password" type="password" formControlName="password" autocomplete="new-password" class="field-input" />
          <p class="mt-1 text-xs text-ink-muted">{{ 'auth.hints.password' | t }}</p>
          @if (showError('password')) {
            <p class="field-error">{{ (form.controls.password.hasError('required') ? 'auth.errors.required' : 'auth.errors.passwordWeak') | t }}</p>
          }
        </div>

        <div>
          <label for="confirmPassword" class="field-label">{{ 'auth.fields.confirmPassword' | t }}</label>
          <input id="confirmPassword" type="password" formControlName="confirmPassword" autocomplete="new-password" class="field-input" />
          @if (showError('confirmPassword')) {
            <p class="field-error">{{ 'auth.errors.required' | t }}</p>
          } @else if (form.hasError('passwordMismatch') && (form.controls.confirmPassword.touched || submitted())) {
            <p class="field-error">{{ 'auth.errors.passwordMismatch' | t }}</p>
          }
        </div>

        <div>
          <label for="hubIcao" class="field-label">{{ 'auth.fields.hub' | t }}</label>
          <select id="hubIcao" formControlName="hubIcao" class="field-input">
            @for (hub of hubs(); track hub.icao) {
              <option [value]="hub.icao">{{ hub.icao }} · {{ hub.city }}</option>
            }
          </select>
        </div>

        @if (errorKey(); as key) {
          <p class="rounded-md bg-danger/10 px-3 py-2 text-sm text-danger">{{ key | t }}</p>
        }

        <button type="submit" class="btn-primary w-full py-2.5" [disabled]="loading()">
          {{ (loading() ? 'common.loading' : 'auth.register.submit') | t }}
        </button>
      </form>

      <p class="text-center text-sm text-ink-muted">
        {{ 'auth.register.hasAccount' | t }}
        <a routerLink="/login" class="font-medium text-primary hover:underline">{{ 'auth.register.signIn' | t }}</a>
      </p>
    </section>
  `
})
export class Register implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly reference = inject(ReferenceService);
  private readonly router = inject(Router);

  protected readonly hubs = signal<Hub[]>([]);
  protected readonly loading = signal(false);
  protected readonly errorKey = signal<string | null>(null);
  protected readonly submitted = signal(false);

  protected readonly form = inject(FormBuilder).nonNullable.group(
    {
      firstName: ['', [Validators.required, Validators.maxLength(60)]],
      lastName: ['', [Validators.required, Validators.maxLength(60)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.pattern(PASSWORD_PATTERN)]],
      confirmPassword: ['', Validators.required],
      hubIcao: ['SBGR']
    },
    { validators: passwordsMatch }
  );

  async ngOnInit(): Promise<void> {
    try {
      const hubs = await this.reference.getHubs();
      this.hubs.set(hubs.filter(h => h.passenger));
    } catch {
      this.errorKey.set('auth.errors.generic');
    }
  }

  protected showError(name: Field): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || this.submitted());
  }

  protected async submit(): Promise<void> {
    this.submitted.set(true);
    this.errorKey.set(null);
    if (this.form.invalid) return;

    const { confirmPassword: _, ...request } = this.form.getRawValue();

    this.loading.set(true);
    try {
      await this.auth.register(request);
      await this.router.navigateByUrl('/dashboard');
    } catch (error) {
      this.errorKey.set(apiErrorStatus(error) === 409 ? 'auth.errors.emailTaken' : 'auth.errors.generic');
    } finally {
      this.loading.set(false);
    }
  }
}