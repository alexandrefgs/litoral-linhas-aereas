import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { PilotSummary } from '../../core/auth/auth.models';
import { AuthService } from '../../core/auth/auth.service';
import { STAFF_POSITION_NAMES } from '../../core/staff/staff-positions';

@Component({
  selector: 'app-profile',
  imports: [ReactiveFormsModule],
  template: `
    @if (auth.pilot(); as pilot) {
      <section class="mx-auto flex max-w-3xl flex-col gap-6 px-6 py-10">
        <h1 class="text-3xl font-bold text-ink">Profile</h1>

        <div class="card grid gap-4 sm:grid-cols-2">
          <div>
            <p class="text-sm text-ink-muted">Pilot ID</p>
            <p class="font-medium text-ink">{{ pilot.displayId }}</p>
          </div>
          <div>
            <p class="text-sm text-ink-muted">Name</p>
            <p class="font-medium text-ink">{{ pilot.firstName }} {{ pilot.lastName }}</p>
          </div>
          <div>
            <p class="text-sm text-ink-muted">Email</p>
            <p class="font-medium text-ink">{{ pilot.email }}</p>
          </div>
          <div>
            <p class="text-sm text-ink-muted">Rank</p>
            <p class="font-medium text-ink">{{ pilot.rankCode }} · {{ pilot.rankName }}</p>
          </div>
          @if (pilot.staffPositions.length) {
            <div class="sm:col-span-2">
              <p class="text-sm text-ink-muted">Staff positions</p>
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
            <label for="simBriefUsername" class="field-label">SimBrief username</label>
            <input id="simBriefUsername" formControlName="simBriefUsername" class="field-input" />
            <p class="mt-1 text-xs text-ink-muted">The same username you use to sign in to SimBrief.</p>
          </div>

          @if (message(); as text) {
            <p class="rounded-md px-3 py-2 text-sm" [class]="success() ? 'bg-accent/15 text-ink' : 'bg-danger/10 text-danger'">{{ text }}</p>
          }

          <div>
            <button type="submit" class="btn-primary px-5 py-2.5" [disabled]="loading()">
              {{ loading() ? 'Loading...' : 'Save' }}
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
  protected readonly message = signal<string | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    simBriefUsername: ['', Validators.maxLength(60)]
  });

  ngOnInit(): void {
    this.form.patchValue({ simBriefUsername: this.auth.pilot()?.simBriefUsername ?? '' });
  }

  protected async save(): Promise<void> {
    this.message.set(null);
    if (this.form.invalid) return;

    this.loading.set(true);
    try {
      const pilot = await firstValueFrom(
        this.http.put<PilotSummary>('/api/pilots/me/simbrief', this.form.getRawValue())
      );
      this.auth.updatePilot(pilot);
      this.success.set(true);
      this.message.set('Profile updated.');
    } catch {
      this.success.set(false);
      this.message.set('Something went wrong. Try again.');
    } finally {
      this.loading.set(false);
    }
  }
}