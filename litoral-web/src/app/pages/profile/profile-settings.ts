import { Component, OnInit, computed, inject, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { apiErrorMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { DEFAULT_COVER_URL } from '../../core/profile/profile.models';
import { COUNTRIES, PASSWORD_RULES, PilotSettings } from '../../core/profile/settings.models';
import { SettingsService } from '../../core/profile/settings.service';
import { DeleteAccountDialog } from '../../shared/delete-account-dialog/delete-account-dialog';
import { PilotAvatar } from './pilot-avatar';

type Section = 'media' | 'personal' | 'config' | 'password';
type PasswordField = 'current' | 'next' | 'confirm';

interface Feedback {
  ok: boolean;
  text: string;
}

const DIGITS = Validators.pattern(/^\d*$/);

@Component({
  selector: 'app-profile-settings',
  imports: [ReactiveFormsModule, PilotAvatar, DeleteAccountDialog],
  template: `
    @if (settings(); as s) {
      <div class="flex flex-col gap-6">
        <div class="panel">
          <h2 class="border-b border-line px-6 py-4 text-lg font-semibold text-ink">Profile details</h2>

          <div class="grid gap-6 border-b border-line px-6 py-5 md:grid-cols-2">
            <div class="flex items-center gap-4">
              <app-pilot-avatar [photoUrl]="s.photoUrl" [firstName]="s.firstName" [lastName]="s.lastName" class="h-20 w-20 rounded-xl" />
              <div class="flex flex-col gap-2">
                <div class="flex flex-wrap gap-2">
                  <label class="btn-primary cursor-pointer" [class.pointer-events-none]="busy() === 'media'">
                    <i class="ti ti-upload mr-1.5" aria-hidden="true"></i> Upload photo
                    <input type="file" accept="image/jpeg,image/png,image/webp" class="hidden" (change)="upload('photo', $event)" />
                  </label>
                  @if (s.photoUrl) {
                    <button type="button" class="btn-secondary" [disabled]="busy() === 'media'" (click)="remove('photo')">Remove</button>
                  }
                </div>
                <p class="text-xs text-ink-muted">Square image, JPG, PNG or WEBP up to 5 MB.</p>
              </div>
            </div>

            <div class="flex items-center gap-4">
              <div class="h-20 w-36 shrink-0 rounded-xl bg-cover bg-center" [style.background-image]="cover(s.coverUrl)"></div>
              <div class="flex flex-col gap-2">
                <div class="flex flex-wrap gap-2">
                  <label class="btn-primary cursor-pointer" [class.pointer-events-none]="busy() === 'media'">
                    <i class="ti ti-photo mr-1.5" aria-hidden="true"></i> Upload cover
                    <input type="file" accept="image/jpeg,image/png,image/webp" class="hidden" (change)="upload('cover', $event)" />
                  </label>
                  @if (s.coverUrl) {
                    <button type="button" class="btn-secondary" [disabled]="busy() === 'media'" (click)="remove('cover')">Use default</button>
                  }
                </div>
                <p class="text-xs text-ink-muted">Wide image (about 1600 × 400), up to 5 MB.</p>
              </div>
            </div>

            @if (feedback()['media']; as f) {
              <p class="rounded-md px-3 py-2 text-sm md:col-span-2" [class]="f.ok ? 'bg-accent/15 text-ink' : 'bg-danger/10 text-danger'">{{ f.text }}</p>
            }
          </div>

          <form [formGroup]="personal" (ngSubmit)="savePersonal()" class="grid gap-4 px-6 py-5 md:grid-cols-2" novalidate>
            <div>
              <label for="firstName" class="field-label">First name</label>
              <input id="firstName" formControlName="firstName" class="field-input" maxlength="60" />
            </div>
            <div>
              <label for="lastName" class="field-label">Last name</label>
              <input id="lastName" formControlName="lastName" class="field-input" maxlength="60" />
            </div>
            <div>
              <label for="nameFormat" class="field-label">Name shown on the website</label>
              <select id="nameFormat" formControlName="nameFormat" class="field-input">
                @for (option of nameOptions(); track option.value) {
                  <option [value]="option.value">{{ displayId() }} - {{ option.example }}</option>
                }
              </select>
            </div>
            <div>
              <label for="email" class="field-label">Email</label>
              <input id="email" [value]="s.email" class="field-input opacity-70" disabled />
              <p class="mt-1 text-xs text-ink-muted">Contact the staff to change your email.</p>
            </div>
            <div>
              <label for="birthDay" class="field-label">Birth date</label>
              <div class="grid grid-cols-[1fr_1.6fr_1.2fr] gap-2">
                <select id="birthDay" formControlName="birthDay" class="field-input">
                  <option value="">Day</option>
                  @for (d of days; track d) { <option [value]="d">{{ d }}</option> }
                </select>
                <select formControlName="birthMonth" class="field-input" aria-label="Birth month">
                  <option value="">Month</option>
                  @for (m of months; track m.value) { <option [value]="m.value">{{ m.label }}</option> }
                </select>
                <select formControlName="birthYear" class="field-input" aria-label="Birth year">
                  <option value="">Year</option>
                  @for (y of years; track y) { <option [value]="y">{{ y }}</option> }
                </select>
              </div>
              <p class="mt-1 text-xs text-ink-muted">Only you and the staff can see it. Other pilots see your age.</p>
            </div>
            <div>
              <label for="countryCode" class="field-label">Country</label>
              <select id="countryCode" formControlName="countryCode" class="field-input">
                <option value="">Not informed</option>
                @for (c of countries; track c.code) {
                  <option [value]="c.code">{{ c.name }}</option>
                }
              </select>
            </div>
            <div>
              <label for="state" class="field-label">State</label>
              <input id="state" formControlName="state" class="field-input" maxlength="60" placeholder="SC" />
            </div>
            <div>
              <label for="city" class="field-label">City</label>
              <input id="city" formControlName="city" class="field-input" maxlength="80" />
            </div>
            <div>
              <label for="vatsimId" class="field-label">VATSIM ID</label>
              <input id="vatsimId" formControlName="vatsimId" class="field-input font-mono" inputmode="numeric" maxlength="10" />
            </div>
            <div>
              <label for="ivaoId" class="field-label">IVAO ID</label>
              <input id="ivaoId" formControlName="ivaoId" class="field-input font-mono" inputmode="numeric" maxlength="10" />
            </div>
            <div class="md:col-span-2">
              <label for="discordId" class="field-label">
                Discord user ID
                <a href="https://support.discord.com/hc/en-us/articles/206346498" target="_blank" rel="noopener" class="ml-1 text-xs font-normal text-primary hover:underline">Where do I find my ID?</a>
              </label>
              <input id="discordId" formControlName="discordId" class="field-input font-mono md:w-1/2" inputmode="numeric" maxlength="20" />
            </div>

            <p class="text-xs text-ink-muted md:col-span-2">
              <i class="ti ti-info-circle" aria-hidden="true"></i>
              Flights logged on VATSIM or IVAO with an ID that isn't registered here are rejected.
            </p>

            @if (feedback()['personal']; as f) {
              <p class="rounded-md px-3 py-2 text-sm md:col-span-2" [class]="f.ok ? 'bg-accent/15 text-ink' : 'bg-danger/10 text-danger'">{{ f.text }}</p>
            }

            <div class="md:col-span-2">
              <button type="submit" class="btn-primary px-5 py-2.5" [disabled]="busy() === 'personal'">
                {{ busy() === 'personal' ? 'Saving...' : 'Save changes' }}
              </button>
            </div>
          </form>
        </div>

        <div class="grid gap-6 lg:grid-cols-2">
          <form [formGroup]="config" (ngSubmit)="saveConfig()" class="panel flex flex-col" novalidate>
            <h2 class="border-b border-line px-6 py-4 text-lg font-semibold text-ink">Config</h2>
            <div class="flex flex-1 flex-col gap-5 px-6 py-5">
              <div>
                <p class="field-label">Aircraft in your simulator</p>
                <div class="mt-2 grid gap-2 sm:grid-cols-2">
                  @for (type of s.aircraftTypes; track type.icaoCode) {
                    <label class="flex cursor-pointer items-center gap-2.5 rounded-md border border-line px-3 py-2 text-sm text-ink hover:bg-surface-alt">
                      <input type="checkbox" class="h-4 w-4 accent-primary" [checked]="selectedAircraft().has(type.icaoCode)" (change)="toggleAircraft(type.icaoCode)" />
                      {{ type.name }} <span class="ml-auto font-mono text-xs text-ink-muted">{{ type.icaoCode }}</span>
                    </label>
                  }
                </div>
              </div>

              <div>
                <label for="simBriefUsername" class="field-label">SimBrief username</label>
                <input id="simBriefUsername" formControlName="simBriefUsername" class="field-input" maxlength="60" />
                <p class="mt-1 text-xs text-ink-muted">The same username you use to sign in to SimBrief. Used to load your OFP in the briefing.</p>
              </div>

              @if (feedback()['config']; as f) {
                <p class="rounded-md px-3 py-2 text-sm" [class]="f.ok ? 'bg-accent/15 text-ink' : 'bg-danger/10 text-danger'">{{ f.text }}</p>
              }

              <div class="mt-auto">
                <button type="submit" class="btn-primary px-5 py-2.5" [disabled]="busy() === 'config'">
                  {{ busy() === 'config' ? 'Saving...' : 'Save config' }}
                </button>
              </div>
            </div>
          </form>

          <form [formGroup]="password" (ngSubmit)="savePassword()" class="panel flex flex-col" novalidate>
            <h2 class="border-b border-line px-6 py-4 text-lg font-semibold text-ink">Change password</h2>
            <div class="flex flex-1 flex-col gap-4 px-6 py-5">
              <div>
                <label for="currentPassword" class="field-label">Current password</label>
                <div class="relative">
                  <input id="currentPassword" formControlName="current" class="field-input pr-10" autocomplete="current-password"
                    [type]="visible().has('current') ? 'text' : 'password'" />
                  <button type="button" class="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-lg text-ink-muted hover:text-ink" (click)="toggleVisible('current')">
                    <i class="ti" [class]="visible().has('current') ? 'ti-eye' : 'ti-eye-off'" aria-hidden="true"></i>
                  </button>
                </div>
              </div>
              <div class="grid gap-4 sm:grid-cols-2">
                <div>
                  <label for="newPassword" class="field-label">New password</label>
                  <div class="relative">
                    <input id="newPassword" formControlName="next" class="field-input pr-10" autocomplete="new-password"
                      [type]="visible().has('next') ? 'text' : 'password'" />
                    <button type="button" class="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-lg text-ink-muted hover:text-ink" (click)="toggleVisible('next')">
                      <i class="ti" [class]="visible().has('next') ? 'ti-eye' : 'ti-eye-off'" aria-hidden="true"></i>
                    </button>
                  </div>
                </div>
                <div>
                  <label for="confirmPassword" class="field-label">Confirm new password</label>
                  <div class="relative">
                    <input id="confirmPassword" formControlName="confirm" class="field-input pr-10" autocomplete="new-password"
                      [type]="visible().has('confirm') ? 'text' : 'password'" />
                    <button type="button" class="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-lg text-ink-muted hover:text-ink" (click)="toggleVisible('confirm')">
                      <i class="ti" [class]="visible().has('confirm') ? 'ti-eye' : 'ti-eye-off'" aria-hidden="true"></i>
                    </button>
                  </div>
                </div>
              </div>

              <div>
                <p class="text-sm font-medium text-ink">Password requirements</p>
                <ul class="mt-2 flex flex-col gap-1.5 text-sm">
                  @for (rule of passwordChecks(); track rule.label) {
                    <li class="flex items-center gap-2" [class]="rule.ok ? 'text-onda-500' : 'text-ink-muted'">
                      <i class="ti" [class]="rule.ok ? 'ti-circle-check' : 'ti-circle'" aria-hidden="true"></i> {{ rule.label }}
                    </li>
                  }
                </ul>
              </div>

              @if (feedback()['password']; as f) {
                <p class="rounded-md px-3 py-2 text-sm" [class]="f.ok ? 'bg-accent/15 text-ink' : 'bg-danger/10 text-danger'">{{ f.text }}</p>
              }

              <div class="mt-auto flex gap-2">
                <button type="submit" class="btn-primary px-5 py-2.5" [disabled]="!canChangePassword() || busy() === 'password'">
                  {{ busy() === 'password' ? 'Saving...' : 'Change password' }}
                </button>
                <button type="button" class="btn-secondary" (click)="password.reset(); feedback.set({})">Reset</button>
              </div>
            </div>
          </form>
        </div>

        <div class="panel flex flex-wrap items-center justify-between gap-4 border-danger/40 px-6 py-5">
          <div>
            <h2 class="text-lg font-semibold text-danger">Delete account</h2>
            <p class="text-sm text-ink-muted">Permanently delete your account and all your flights, points and history. This can't be undone.</p>
          </div>
          <button type="button" class="rounded-md border border-danger px-4 py-2 text-sm font-medium text-danger hover:bg-danger hover:text-white"
            (click)="deleteError.set(null); deleting.set(true)">
            Delete my account
          </button>
        </div>
      </div>

      @if (deleting()) {
        <app-delete-account-dialog [self]="true" [busy]="deleteBusy()" [error]="deleteError()"
          (confirmed)="deleteAccount($event)" (cancelled)="deleting.set(false)" />
      }
    } @else if (loadError()) {
      <div class="panel flex flex-col items-center gap-3 px-6 py-16 text-center">
        <p class="text-danger">Something went wrong. Try again.</p>
        <button type="button" class="btn-secondary" (click)="load()">Try again</button>
      </div>
    } @else {
      <p class="py-16 text-center text-ink-muted">Loading...</p>
    }
  `
})
export class ProfileSettings implements OnInit {
  readonly changed = output<void>();

  private readonly api = inject(SettingsService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder).nonNullable;

  protected readonly countries = COUNTRIES;
  protected readonly days = Array.from({ length: 31 }, (_, i) => String(i + 1));
  protected readonly months = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December']
    .map((label, i) => ({ value: String(i + 1), label }));
  protected readonly years = Array.from({ length: 88 }, (_, i) => String(new Date().getFullYear() - 13 - i));

  protected readonly settings = signal<PilotSettings | null>(null);
  protected readonly loadError = signal(false);
  protected readonly busy = signal<Section | null>(null);
  protected readonly feedback = signal<Partial<Record<Section, Feedback>>>({});
  protected readonly selectedAircraft = signal(new Set<string>());
  protected readonly visible = signal(new Set<PasswordField>());
  protected readonly deleting = signal(false);
  protected readonly deleteBusy = signal(false);
  protected readonly deleteError = signal<string | null>(null);
  private readonly newPassword = signal('');
  private readonly confirmPassword = signal('');
  private readonly currentPassword = signal('');

  protected readonly displayId = computed(() => this.auth.pilot()?.displayId ?? '');
  protected readonly nameOptions = computed(() => this.settings()?.nameFormats ?? []);

  protected readonly passwordChecks = computed(() => {
    const value = this.newPassword();
    return [
      ...PASSWORD_RULES.map(rule => ({ label: rule.label, ok: rule.test(value) })),
      { label: 'Passwords match', ok: value.length > 0 && value === this.confirmPassword() }
    ];
  });

  protected readonly canChangePassword = computed(() => this.currentPassword().length > 0 && this.passwordChecks().every(r => r.ok));

  protected readonly personal = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(60)]],
    lastName: ['', [Validators.required, Validators.maxLength(60)]],
    nameFormat: ['FirstInitialAndLast', Validators.required],
    birthDay: [''],
    birthMonth: [''],
    birthYear: [''],
    countryCode: [''],
    state: ['', Validators.maxLength(60)],
    city: ['', Validators.maxLength(80)],
    vatsimId: ['', DIGITS],
    ivaoId: ['', DIGITS],
    discordId: ['', DIGITS]
  });

  protected readonly config = this.fb.group({
    simBriefUsername: ['', Validators.maxLength(60)]
  });

  protected readonly password = this.fb.group({
    current: [''],
    next: [''],
    confirm: ['']
  });

  constructor() {
    this.password.controls.current.valueChanges.subscribe(v => this.currentPassword.set(v ?? ''));
    this.password.controls.next.valueChanges.subscribe(v => this.newPassword.set(v ?? ''));
    this.password.controls.confirm.valueChanges.subscribe(v => this.confirmPassword.set(v ?? ''));
  }

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loadError.set(false);
    try {
      this.apply(await this.api.get());
    } catch {
      this.loadError.set(true);
    }
  }

  protected cover(url: string | null): string {
    return `url('${url ?? DEFAULT_COVER_URL}'), linear-gradient(135deg, #0F3768 0%, #1D6FD8 55%, #2EC4B6 100%)`;
  }

  protected toggleAircraft(code: string): void {
    this.selectedAircraft.update(set => {
      const next = new Set(set);
      if (next.has(code)) next.delete(code);
      else next.add(code);
      return next;
    });
  }

  protected toggleVisible(field: PasswordField): void {
    this.visible.update(set => {
      const next = new Set(set);
      if (next.has(field)) next.delete(field);
      else next.add(field);
      return next;
    });
  }

  protected async upload(kind: 'photo' | 'cover', event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
      this.setFeedback('media', false, 'The image must be 5 MB or smaller.');
      return;
    }

    await this.run('media', async () => {
      this.apply(kind === 'photo' ? await this.api.uploadPhoto(file) : await this.api.uploadCover(file));
      this.changed.emit();
      return kind === 'photo' ? 'Photo updated.' : 'Cover updated.';
    });
  }

  protected async remove(kind: 'photo' | 'cover'): Promise<void> {
    await this.run('media', async () => {
      this.apply(kind === 'photo' ? await this.api.removePhoto() : await this.api.removeCover());
      this.changed.emit();
      return kind === 'photo' ? 'Photo removed.' : 'Default cover restored.';
    });
  }

  protected async savePersonal(): Promise<void> {
    if (this.personal.invalid) {
      this.personal.markAllAsTouched();
      this.setFeedback('personal', false, 'Check the fields. Names are required and the IDs accept only numbers.');
      return;
    }

    const v = this.personal.getRawValue();
    const birthDate = this.composeBirthDate(v.birthDay, v.birthMonth, v.birthYear);
    if (birthDate === undefined) {
      this.setFeedback('personal', false, 'Select the full birth date (day, month and year) or leave all three empty.');
      return;
    }
    await this.run('personal', async () => {
      this.apply(await this.api.updatePersonal({
        firstName: v.firstName.trim(),
        lastName: v.lastName.trim(),
        nameFormat: v.nameFormat,
        birthDate,
        countryCode: v.countryCode || null,
        state: v.state.trim() || null,
        city: v.city.trim() || null,
        vatsimId: v.vatsimId.trim() || null,
        ivaoId: v.ivaoId.trim() || null,
        discordId: v.discordId.trim() || null
      }));
      this.changed.emit();
      return 'Profile updated.';
    });
  }

  protected async saveConfig(): Promise<void> {
    await this.run('config', async () => {
      this.apply(await this.api.updateConfig({
        simBriefUsername: this.config.getRawValue().simBriefUsername.trim() || null,
        simAircraft: [...this.selectedAircraft()]
      }));
      this.changed.emit();
      return 'Config saved.';
    });
  }

  protected async savePassword(): Promise<void> {
    if (!this.canChangePassword()) return;

    const v = this.password.getRawValue();
    await this.run('password', async () => {
      await this.api.changePassword(v.current, v.next);
      this.password.reset();
      return 'Password changed.';
    });
  }

  protected async deleteAccount(password: string | null): Promise<void> {
    this.deleteBusy.set(true);
    this.deleteError.set(null);
    try {
      await this.api.deleteAccount(password ?? '');
      this.deleting.set(false);
      this.auth.logout();
    } catch (error) {
      this.deleteError.set(apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.deleteBusy.set(false);
    }
  }

  private composeBirthDate(day: string, month: string, year: string): string | null | undefined {
    if (!day && !month && !year) return null;
    if (!day || !month || !year) return undefined;

    const date = new Date(Date.UTC(+year, +month - 1, +day));
    if (date.getUTCMonth() !== +month - 1) return undefined;

    return `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
  }

  private apply(settings: PilotSettings): void {
    this.settings.set(settings);
    this.personal.reset({
      firstName: settings.firstName,
      lastName: settings.lastName,
      nameFormat: settings.nameFormat,
      birthDay: settings.birthDate ? String(+settings.birthDate.slice(8, 10)) : '',
      birthMonth: settings.birthDate ? String(+settings.birthDate.slice(5, 7)) : '',
      birthYear: settings.birthDate ? settings.birthDate.slice(0, 4) : '',
      countryCode: settings.countryCode ?? '',
      state: settings.state ?? '',
      city: settings.city ?? '',
      vatsimId: settings.vatsimId ?? '',
      ivaoId: settings.ivaoId ?? '',
      discordId: settings.discordId ?? ''
    });
    this.config.reset({ simBriefUsername: settings.simBriefUsername ?? '' });
    this.selectedAircraft.set(new Set(settings.simAircraft));
  }

  private async run(section: Section, action: () => Promise<string>): Promise<void> {
    this.busy.set(section);
    this.feedback.update(f => ({ ...f, [section]: undefined }));
    try {
      this.setFeedback(section, true, await action());
    } catch (error) {
      this.setFeedback(section, false, apiErrorMessage(error) ?? 'Something went wrong. Try again.');
    } finally {
      this.busy.set(null);
    }
  }

  private setFeedback(section: Section, ok: boolean, text: string): void {
    this.feedback.update(f => ({ ...f, [section]: { ok, text } }));
  }
}