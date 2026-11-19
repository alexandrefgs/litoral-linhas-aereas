import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { apiErrorMessage } from '../../core/api/api-error';
import { PRIVACY_REQUEST_TYPES, PrivacyOverview, PrivacyRequestStatus, PrivacyRequestType, PrivacyService } from '../../core/privacy/privacy.service';

const STATUS_STYLE: Record<PrivacyRequestStatus, string> = {
  Open: 'bg-highlight/25 text-ink',
  Completed: 'bg-onda-500/15 text-onda-600',
  Rejected: 'bg-danger/10 text-danger'
};

@Component({
  selector: 'app-privacy-panel',
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  template: `
    <div class="panel">
      <h2 class="flex items-center gap-2 border-b border-line px-6 py-4 text-lg font-semibold text-ink">
        <i class="ti ti-lock text-primary" aria-hidden="true"></i>Privacy and your data
      </h2>

      <div class="grid gap-6 px-6 py-5 lg:grid-cols-2">
        <div class="flex flex-col gap-5">
          <div>
            <p class="font-semibold text-ink">Terms you accepted</p>
            @if (overview(); as o) {
              <p class="mt-1 text-sm text-ink-muted">
                @if (o.acceptedTermsAt) {
                  Version {{ o.acceptedTermsVersion }}, accepted on {{ o.acceptedTermsAt | date: 'd MMM y, HH:mm' }}.
                } @else {
                  Not accepted yet.
                }
              </p>
            }
            <p class="mt-1 text-sm">
              <a routerLink="/regulations" class="font-medium text-primary hover:underline">Regulations</a>
              <span class="text-ink-muted"> · </span>
              <a routerLink="/privacy" class="font-medium text-primary hover:underline">Privacy Policy</a>
            </p>
          </div>

          <div>
            <p class="font-semibold text-ink">Download your data</p>
            <p class="mt-1 text-sm text-ink-muted">A JSON file with your account, profile, bookings, flights, points, notifications and activity log.</p>
            <button type="button" class="btn-secondary mt-3 flex items-center gap-2" [disabled]="exporting()" (click)="export()">
              <i class="ti ti-download" aria-hidden="true"></i>{{ exporting() ? 'Preparing...' : 'Download my data' }}
            </button>
          </div>

          <form [formGroup]="form" (ngSubmit)="send()" class="flex flex-col gap-3" novalidate>
            <div>
              <p class="font-semibold text-ink">Make a privacy request</p>
              <p class="mt-1 text-sm text-ink-muted">Ask to access, correct or delete your data, or anything else about your privacy. The staff answers within 15 days.</p>
            </div>
            <select formControlName="type" class="field-input" aria-label="Type of request">
              @for (t of types; track t.value) {
                <option [value]="t.value">{{ t.label }}</option>
              }
            </select>
            <textarea formControlName="message" rows="3" maxlength="2000" class="field-input" placeholder="Describe your request" aria-label="Request"></textarea>
            <div>
              <button type="submit" class="btn-primary px-5" [disabled]="form.invalid || sending()">
                {{ sending() ? 'Sending...' : 'Send request' }}
              </button>
            </div>
          </form>

          @if (message(); as m) {
            <p class="rounded-md px-3 py-2 text-sm" [class]="m.ok ? 'bg-accent/15 text-ink' : 'bg-danger/10 text-danger'">{{ m.text }}</p>
          }
        </div>

        <div>
          <p class="font-semibold text-ink">Your requests</p>
          @if (!overview()) {
            <p class="mt-2 text-sm text-ink-muted">{{ loadError() ? 'Could not load your requests.' : 'Loading...' }}</p>
          } @else if (requests().length === 0) {
            <p class="mt-2 text-sm text-ink-muted">You have not made any request.</p>
          } @else {
            <ul class="mt-2 flex flex-col gap-3">
              @for (r of requests(); track r.id) {
                <li class="rounded-lg border border-line p-4 text-sm">
                  <div class="flex flex-wrap items-center justify-between gap-2">
                    <span class="font-semibold text-ink">{{ typeLabel(r.type) }}</span>
                    <span class="rounded-full px-2.5 py-0.5 text-xs font-semibold" [class]="statusStyle[r.status]">{{ r.status }}</span>
                  </div>
                  <p class="mt-1 text-xs text-ink-muted">
                    Sent {{ r.createdAt | date: 'd MMM y' }}
                    @if (r.status === 'Open') { · answer by {{ r.dueAt | date: 'd MMM y' }} }
                    @if (r.resolvedAt) { · answered {{ r.resolvedAt | date: 'd MMM y' }} }
                  </p>
                  <p class="mt-2 whitespace-pre-line text-ink">{{ r.message }}</p>
                  @if (r.response) {
                    <p class="mt-2 whitespace-pre-line rounded-md bg-surface-alt px-3 py-2 text-ink">{{ r.response }}</p>
                  }
                </li>
              }
            </ul>
          }
        </div>
      </div>
    </div>
  `
})
export class PrivacyPanel implements OnInit {
  private readonly api = inject(PrivacyService);

  protected readonly types = PRIVACY_REQUEST_TYPES;
  protected readonly statusStyle = STATUS_STYLE;
  protected readonly overview = signal<PrivacyOverview | null>(null);
  protected readonly loadError = signal(false);
  protected readonly exporting = signal(false);
  protected readonly sending = signal(false);
  protected readonly message = signal<{ ok: boolean; text: string } | null>(null);
  protected readonly requests = computed(() => this.overview()?.requests ?? []);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    type: ['Access' as PrivacyRequestType, Validators.required],
    message: ['', [Validators.required, Validators.maxLength(2000)]]
  });

  async ngOnInit(): Promise<void> {
    try {
      this.overview.set(await this.api.overview());
    } catch {
      this.loadError.set(true);
    }
  }

  protected typeLabel(type: PrivacyRequestType): string {
    return this.types.find(t => t.value === type)?.label ?? type;
  }

  protected async export(): Promise<void> {
    this.exporting.set(true);
    this.message.set(null);
    try {
      await this.api.downloadExport();
    } catch {
      this.message.set({ ok: false, text: 'Could not prepare your data. Try again.' });
    } finally {
      this.exporting.set(false);
    }
  }

  protected async send(): Promise<void> {
    if (this.form.invalid) return;

    this.sending.set(true);
    this.message.set(null);
    try {
      const { type, message } = this.form.getRawValue();
      const created = await this.api.createRequest(type, message.trim());
      this.overview.update(o => (o ? { ...o, requests: [created, ...o.requests] } : o));
      this.form.reset({ type: 'Access', message: '' });
      this.message.set({ ok: true, text: 'Request sent. You will get a notification when the staff answers.' });
    } catch (error) {
      this.message.set({ ok: false, text: apiErrorMessage(error) ?? 'Something went wrong. Try again.' });
    } finally {
      this.sending.set(false);
    }
  }
}