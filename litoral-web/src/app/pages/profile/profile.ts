import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { PilotSummary } from '../../core/auth/auth.models';
import { AuthService } from '../../core/auth/auth.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { STAFF_POSITION_NAMES } from '../../core/staff/staff-positions';

@Component({
  selector: 'app-profile',
  imports: [ReactiveFormsModule, TranslatePipe],
  template: `
    @if (auth.pilot(); as pilot) {
      <section class="mx-auto flex max-w-3xl flex-col gap-6 px-6 py-10">
        <h1 class="text-3xl font-bold text-ink">{{ 'profile.title' | t }}</h1>

        <div class="card grid gap-4 sm:grid-cols-2">
          <div>
            <p class="text-sm text-ink-muted">{{ 'profile.pilotId' | t }}</p>
            <p class="font-medium text-ink">{{ pilot.displayId }}</p>
          </div>
          <div>
            <p class="text-sm text-ink-muted">{{ 'profile.name' | t }}</p>
            <p class="font-medium text-ink">{{ pilot.firstName }} {{ pilot.lastName }}</p>
          </div>
          <div>
            <p class="text-sm text-ink-muted">{{ 'auth.fields.email' | t }}</p>
            <p class="font-medium text-ink">{{ pilot.email }}</p>
          </div>
          <div>
            <p class="text-sm text-ink-muted">{{ 'dashboard.rank' | t }}</p>
            <p class="font-medium text-ink">{{ pilot.rankCode }} · {{ pilot.rankName }}</p>
          </div>
          @if (pilot.staffPositions.length) {
            <div class="sm:col-span-2">
              <p class="text-sm text-ink-muted">{{ 'dashboard.staff' | t }}</p>
              <p class="font-medium text-ink">
                @for (position of pilot.staffPositions; track position; let last = $last) {
                  {{ staffNames[position] ?? position }}@if (!last) {, }
                }
              </p>
            </div>
          }
        </div>

        <form [formGroup]="form" (ngSubmit)="save()" class="card flex flex-col gap-4" novalidate>
          <div>
            <label for="simBriefUsername" class="field-label">{{ 'profile.simbrief' | t }}</label>
            <input id="simBriefUsername" formControlName="simBriefUsername" class="field-input" />
            <p class="mt-1 text-xs text-ink-muted">{{ 'profile.simbriefHint' | t }}</p>
          </div>

          @if (messageKey(); as key) {
            <p class="rounded-md px-3 py-2 text-sm" [class]="success() ? 'bg-accent/15 text-ink' : 'bg-danger/10 text-danger'">{{ key | t }}</p>
          }

          <div>
            <button type="submit" class="btn-primary px-5 py-2.5" [disabled]="loading()">
              {{ (loading() ? 'common.loading' : 'common.save') | t }}
            </button>
          </div>
        </form>
      </section>
    }
  `
})
export class Profile implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly http = inject(HttpClient);

  protected readonly staffNames = STAFF_POSITION_NAMES;
  protected readonly loading = signal(false);
  protected readonly success = signal(false);
  protected readonly messageKey = signal<string | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    simBriefUsername: ['', Validators.maxLength(60)]
  });

  ngOnInit(): void {
    this.form.patchValue({ simBriefUsername: this.auth.pilot()?.simBriefUsername ?? '' });
  }

  protected async save(): Promise<void> {
    this.messageKey.set(null);
    if (this.form.invalid) return;

    this.loading.set(true);
    try {
      const pilot = await firstValueFrom(
        this.http.put<PilotSummary>('/api/pilots/me/simbrief', this.form.getRawValue())
      );
      this.auth.updatePilot(pilot);
      this.success.set(true);
      this.messageKey.set('profile.saved');
    } catch {
      this.success.set(false);
      this.messageKey.set('auth.errors.generic');
    } finally {
      this.loading.set(false);
    }
  }
}