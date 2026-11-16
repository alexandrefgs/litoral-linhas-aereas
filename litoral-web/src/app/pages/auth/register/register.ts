import { Component, OnInit, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { apiErrorStatus } from '../../../core/api/api-error';
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
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <section class="mx-auto flex max-w-lg flex-col gap-6 px-6 py-16">
      <div class="text-center">
        <h1 class="text-3xl font-bold text-ink">Create account</h1>
        <p class="mt-2 text-ink-muted">Join the Litoral crew.</p>
      </div>

      <form [formGroup]="form" (ngSubmit)="submit()" class="card flex flex-col gap-4" novalidate>
        <div class="grid gap-4 sm:grid-cols-2">
          <div>
            <label for="firstName" class="field-label">First name</label>
            <input id="firstName" formControlName="firstName" autocomplete="given-name" class="field-input" />
            @if (showError('firstName')) {
              <p class="field-error">Required field.</p>
            }
          </div>
          <div>
            <label for="lastName" class="field-label">Last name</label>
            <input id="lastName" formControlName="lastName" autocomplete="family-name" class="field-input" />
            @if (showError('lastName')) {
              <p class="field-error">Required field.</p>
            }
          </div>
        </div>

        <div>
          <label for="email" class="field-label">Email</label>
          <input id="email" type="email" formControlName="email" autocomplete="email" class="field-input" />
          @if (showError('email')) {
            <p class="field-error">{{ form.controls.email.hasError('required') ? 'Required field.' : 'Invalid email.' }}</p>
          }
        </div>

        <div>
          <label for="password" class="field-label">Password</label>
          <input id="password" type="password" formControlName="password" autocomplete="new-password" class="field-input" />
          <p class="mt-1 text-xs text-ink-muted">At least 8 characters, with uppercase, lowercase and a number.</p>
          @if (showError('password')) {
            <p class="field-error">{{ form.controls.password.hasError('required') ? 'Required field.' : 'The password doesn\'t meet the requirements.' }}</p>
          }
        </div>

        <div>
          <label for="confirmPassword" class="field-label">Confirm password</label>
          <input id="confirmPassword" type="password" formControlName="confirmPassword" autocomplete="new-password" class="field-input" />
          @if (showError('confirmPassword')) {
            <p class="field-error">Required field.</p>
          } @else if (form.hasError('passwordMismatch') && (form.controls.confirmPassword.touched || submitted())) {
            <p class="field-error">Passwords don't match.</p>
          }
        </div>

        <div>
          <label for="hubIcao" class="field-label">Starting hub</label>
          <select id="hubIcao" formControlName="hubIcao" class="field-input">
            @for (hub of hubs(); track hub.icao) {
              <option [value]="hub.icao">{{ hub.icao }} · {{ hub.city }}</option>
            }
          </select>
        </div>

        @if (errorMessage(); as text) {
          <p class="rounded-md bg-danger/10 px-3 py-2 text-sm text-danger">{{ text }}</p>
        }

        <button type="submit" class="btn-primary w-full py-2.5" [disabled]="loading()">
          {{ loading() ? 'Loading...' : 'Create account' }}
        </button>
      </form>

      <p class="text-center text-sm text-ink-muted">
        Already have an account?
        <a routerLink="/login" class="font-medium text-primary hover:underline">Sign in</a>
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
  protected readonly errorMessage = signal<string | null>(null);
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
      this.errorMessage.set('Something went wrong. Try again.');
    }
  }

  protected showError(name: Field): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || this.submitted());
  }

  protected async submit(): Promise<void> {
    this.submitted.set(true);
    this.errorMessage.set(null);
    if (this.form.invalid) return;

    const { confirmPassword: _, ...request } = this.form.getRawValue();

    this.loading.set(true);
    try {
      await this.auth.register(request);
      await this.router.navigateByUrl('/dashboard');
    } catch (error) {
      this.errorMessage.set(apiErrorStatus(error) === 409 ? 'This email is already registered.' : 'Something went wrong. Try again.');
    } finally {
      this.loading.set(false);
    }
  }
}