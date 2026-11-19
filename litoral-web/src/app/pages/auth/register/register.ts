import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { apiErrorMessage, apiErrorStatus } from '../../../core/api/api-error';
import { ApplicationsService, RegistrationStatus } from '../../../core/applications/applications.service';
import { ApplicationStatus } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { Hub } from '../../../core/reference/reference.models';
import { ReferenceService } from '../../../core/reference/reference.service';

const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/;
const NETWORK_ID_PATTERN = /^\d{4,10}$/;

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return password && confirm && password !== confirm ? { passwordMismatch: true } : null;
}

function networkRequired(group: AbstractControl): ValidationErrors | null {
  const vatsim = (group.get('vatsimId')?.value ?? '').trim();
  const ivao = (group.get('ivaoId')?.value ?? '').trim();
  return vatsim || ivao ? null : { networkRequired: true };
}

type Field = 'firstName' | 'lastName' | 'email' | 'password' | 'confirmPassword' | 'acceptTerms' | 'vatsimId' | 'ivaoId' | 'simulator' | 'motivation';

@Component({
  selector: 'app-register',
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  template: `
    <section class="mx-auto flex max-w-2xl flex-col gap-6 px-6 py-16">
      @if (sent(); as application) {
        <div class="card flex flex-col items-center gap-4 text-center">
          <span class="flex h-14 w-14 items-center justify-center rounded-full bg-onda-500/15 text-3xl text-onda-600">
            <i class="ti ti-mail-check" aria-hidden="true"></i>
          </span>
          <h1 class="text-2xl font-bold text-ink">Application sent</h1>
          <p class="text-ink-muted">
            Thank you, {{ application.firstName }}. Our HR team will check your profile and answer soon.
            Sign in with your e-mail and password at any time to see the status of your application.
          </p>
          <p class="text-xs text-ink-muted">Sent on {{ application.submittedAt | date: 'd MMM y, HH:mm' }}</p>
          <a routerLink="/login" class="btn-primary px-6">Go to sign in</a>
        </div>
      } @else if (!status()) {
        <p class="text-center text-ink-muted">{{ statusError() ? 'Something went wrong. Try again.' : 'Loading...' }}</p>
      } @else if (!status()!.open) {
        <div class="card flex flex-col items-center gap-4 text-center">
          <span class="flex h-14 w-14 items-center justify-center rounded-full bg-surface-alt text-3xl text-ink-muted">
            <i class="ti ti-door-off" aria-hidden="true"></i>
          </span>
          <h1 class="text-2xl font-bold text-ink">Applications are closed</h1>
          <p class="text-ink-muted">Litoral is not accepting new pilots right now. Follow our news and come back later.</p>
          <a routerLink="/regulations" class="btn-secondary">Read the Regulations</a>
        </div>
      } @else {
        <div class="text-center">
          <h1 class="text-3xl font-bold text-ink">Apply to join</h1>
          <p class="mt-2 text-ink-muted">Every application is reviewed by our HR team before the account is created.</p>
        </div>

        <form [formGroup]="form" (ngSubmit)="submit()" class="card flex flex-col gap-5" novalidate>
          <p class="text-sm font-semibold uppercase tracking-wide text-primary">Account</p>

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

          <div class="grid gap-4 sm:grid-cols-2">
            <div>
              <label for="password" class="field-label">Password</label>
              <input id="password" type="password" formControlName="password" autocomplete="new-password" class="field-input" />
              @if (showError('password')) {
                <p class="field-error">{{ form.controls.password.hasError('required') ? 'Required field.' : 'The password does not meet the requirements.' }}</p>
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
          </div>
          <p class="-mt-3 text-xs text-ink-muted">At least 8 characters, with uppercase, lowercase and a number.</p>

          <div>
            <label for="hubIcao" class="field-label">Base</label>
            <select id="hubIcao" formControlName="hubIcao" class="field-input">
              @for (hub of hubs(); track hub.icao) {
                <option [value]="hub.icao">{{ hub.icao }} · {{ hub.city }}</option>
              }
            </select>
          </div>

          <p class="mt-2 text-sm font-semibold uppercase tracking-wide text-primary">Your flight simulation</p>

          <div class="grid gap-4 sm:grid-cols-2">
            <div>
              <label for="vatsimId" class="field-label">VATSIM ID</label>
              <input id="vatsimId" formControlName="vatsimId" inputmode="numeric" maxlength="10" class="field-input" />
              @if (showError('vatsimId')) {
                <p class="field-error">Use only numbers (4 to 10 digits).</p>
              }
            </div>
            <div>
              <label for="ivaoId" class="field-label">IVAO ID</label>
              <input id="ivaoId" formControlName="ivaoId" inputmode="numeric" maxlength="10" class="field-input" />
              @if (showError('ivaoId')) {
                <p class="field-error">Use only numbers (4 to 10 digits).</p>
              }
            </div>
          </div>
          @if (form.hasError('networkRequired') && submitted()) {
            <p class="field-error -mt-3">Enter your VATSIM ID, your IVAO ID or both.</p>
          } @else {
            <p class="-mt-3 text-xs text-ink-muted">At least one. HR uses it to check your network profile.</p>
          }

          <div>
            <label for="simulator" class="field-label">Simulator</label>
            <select id="simulator" formControlName="simulator" class="field-input">
              <option value="" disabled>Choose your simulator</option>
              @for (sim of status()!.simulators; track sim) {
                <option [value]="sim">{{ sim }}</option>
              }
            </select>
            @if (showError('simulator')) {
              <p class="field-error">Required field.</p>
            }
          </div>

          <div>
            <p class="field-label">737 add-ons you have</p>
            <div class="mt-1 grid gap-2 sm:grid-cols-2">
              @for (addOn of status()!.addOns; track addOn) {
                <label class="flex cursor-pointer items-center gap-2.5 rounded-md border border-line px-3 py-2 text-sm text-ink hover:bg-surface-alt">
                  <input type="checkbox" class="h-4 w-4 accent-primary" [checked]="addOns().has(addOn)" (change)="toggleAddOn(addOn)" />
                  {{ addOn === 'Other' ? 'Other quality 737 add-on' : addOn }}
                </label>
              }
            </div>
            @if (addOns().size === 0 && submitted()) {
              <p class="field-error">Choose at least one add-on. Default simulator aircraft are not accepted.</p>
            }
          </div>

          <div>
            <label for="motivation" class="field-label">Your experience and why you want to join</label>
            <textarea id="motivation" formControlName="motivation" rows="4" maxlength="1000" class="field-input"
              placeholder="How long have you been flying online, which airlines or events you flew, and what you expect from Litoral."></textarea>
            <div class="flex justify-between text-xs">
              <span class="field-error">{{ showError('motivation') ? 'Write between 30 and 1000 characters.' : '' }}</span>
              <span class="text-ink-muted">{{ motivationLength() }}/1000</span>
            </div>
          </div>

          <div>
            <label class="flex cursor-pointer items-start gap-3 text-sm text-ink">
              <input type="checkbox" formControlName="acceptTerms" class="mt-0.5 h-4 w-4 shrink-0 accent-primary" />
              <span>
                I am at least 16 years old and I agree to the
                <a routerLink="/regulations" target="_blank" class="font-medium text-primary hover:underline">Regulations</a>
                and the
                <a routerLink="/privacy" target="_blank" class="font-medium text-primary hover:underline">Privacy Policy</a>.
              </span>
            </label>
            @if (showError('acceptTerms')) {
              <p class="field-error">You must accept the Regulations and the Privacy Policy.</p>
            }
          </div>

          @if (errorMessage(); as text) {
            <p class="rounded-md bg-danger/10 px-3 py-2 text-sm text-danger">{{ text }}</p>
          }

          <button type="submit" class="btn-primary w-full py-2.5" [disabled]="loading()">
            {{ loading() ? 'Sending...' : 'Send application' }}
          </button>
        </form>

        <p class="text-center text-sm text-ink-muted">
          Already have an account?
          <a routerLink="/login" class="font-medium text-primary hover:underline">Sign in</a>
        </p>
      }
    </section>
  `
})
export class Register implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly reference = inject(ReferenceService);
  private readonly applications = inject(ApplicationsService);
  private readonly router = inject(Router);

  protected readonly hubs = signal<Hub[]>([]);
  protected readonly status = signal<RegistrationStatus | null>(null);
  protected readonly statusError = signal(false);
  protected readonly addOns = signal(new Set<string>());
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly submitted = signal(false);
  protected readonly sent = signal<ApplicationStatus | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group(
    {
      firstName: ['', [Validators.required, Validators.maxLength(60)]],
      lastName: ['', [Validators.required, Validators.maxLength(60)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.pattern(PASSWORD_PATTERN)]],
      confirmPassword: ['', Validators.required],
      hubIcao: ['SBGR'],
      vatsimId: ['', Validators.pattern(NETWORK_ID_PATTERN)],
      ivaoId: ['', Validators.pattern(NETWORK_ID_PATTERN)],
      simulator: ['', Validators.required],
      motivation: ['', [Validators.required, Validators.minLength(30), Validators.maxLength(1000)]],
      acceptTerms: [false, Validators.requiredTrue]
    },
    { validators: [passwordsMatch, networkRequired] }
  );

  private readonly motivationValue = signal('');
  protected readonly motivationLength = computed(() => this.motivationValue().trim().length);

  constructor() {
    this.form.controls.motivation.valueChanges.subscribe(v => this.motivationValue.set(v));
  }

  async ngOnInit(): Promise<void> {
    try {
      const [hubs, status] = await Promise.all([this.reference.getHubs(), this.applications.registrationStatus()]);
      this.hubs.set(hubs.filter(h => h.passenger));
      this.status.set(status);
    } catch {
      this.statusError.set(true);
    }
  }

  protected showError(name: Field): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || this.submitted());
  }

  protected toggleAddOn(addOn: string): void {
    this.addOns.update(set => {
      const next = new Set(set);
      if (next.has(addOn)) next.delete(addOn);
      else next.add(addOn);
      return next;
    });
  }

  protected async submit(): Promise<void> {
    this.submitted.set(true);
    this.errorMessage.set(null);
    if (this.form.invalid || this.addOns().size === 0) return;

    const { confirmPassword: _, acceptTerms, vatsimId, ivaoId, motivation, ...data } = this.form.getRawValue();

    this.loading.set(true);
    try {
      const result = await this.auth.register({
        ...data,
        acceptedTerms: acceptTerms,
        vatsimId: vatsimId.trim() || null,
        ivaoId: ivaoId.trim() || null,
        addOns: [...this.addOns()],
        motivation: motivation.trim()
      });

      if (result.kind === 'session') await this.router.navigateByUrl('/dashboard');
      else this.sent.set(result.application);
    } catch (error) {
      const status = apiErrorStatus(error);
      this.errorMessage.set(status === 400 || status === 409 ? (apiErrorMessage(error) ?? 'Check the form and try again.') : 'Something went wrong. Try again.');
    } finally {
      this.loading.set(false);
    }
  }
}