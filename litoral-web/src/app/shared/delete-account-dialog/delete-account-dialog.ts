import { Component, input, output, signal } from '@angular/core';

@Component({
  selector: 'app-delete-account-dialog',
  host: { '(document:keydown.escape)': 'cancel()' },
  template: `
    <div class="fixed inset-0 z-[1200] flex items-center justify-center bg-black/60 p-4" (click)="cancel()">
      <div class="panel flex w-full max-w-md flex-col overflow-hidden shadow-2xl" (click)="$event.stopPropagation()" role="alertdialog" aria-modal="true">
        <div class="flex flex-col items-center gap-3 px-6 pb-2 pt-6 text-center">
          <span class="flex h-14 w-14 items-center justify-center rounded-full bg-danger/15 text-3xl text-danger">
            <i class="ti" [class]="step() === 1 ? 'ti-trash' : 'ti-alert-triangle'" aria-hidden="true"></i>
          </span>
          @if (step() === 1) {
            <h2 class="text-xl font-bold text-ink">{{ self() ? 'Delete your account?' : 'Delete ' + name() + '?' }}</h2>
            <p class="text-sm text-ink-muted">
              {{ self() ? 'Your account' : 'This account' }} will be <span class="font-semibold text-danger">permanently deleted</span>. There is no way to undo it.
            </p>
          } @else {
            <h2 class="text-xl font-bold text-ink">Are you sure?</h2>
            <p class="text-sm text-ink-muted">This is the last step. After this, the account can't be recovered.</p>
          }
        </div>

        <div class="flex flex-col gap-4 px-6 py-4">
          @if (step() === 1) {
            <ul class="flex flex-col gap-1.5 rounded-lg bg-danger/10 px-4 py-3 text-sm text-ink">
              @for (item of deleted; track item) {
                <li class="flex items-center gap-2"><i class="ti ti-x text-danger" aria-hidden="true"></i>{{ item }}</li>
              }
            </ul>
          } @else if (self()) {
            <div>
              <label for="deletePassword" class="field-label">Type your password to confirm</label>
              <input id="deletePassword" type="password" class="field-input" autocomplete="current-password"
                [value]="password()" (input)="password.set($any($event.target).value)" (keydown.enter)="submit()" />
            </div>
          }

          @if (error(); as message) {
            <p class="rounded-md bg-danger/10 px-3 py-2 text-sm text-danger">{{ message }}</p>
          }
        </div>

        <div class="flex gap-2 border-t border-line px-6 py-4">
          <button type="button" class="btn-secondary flex-1" [disabled]="busy()" (click)="cancel()">Cancel</button>
          @if (step() === 1) {
            <button type="button" class="flex-1 rounded-md bg-danger px-4 py-2 text-sm font-medium text-white hover:opacity-90" (click)="step.set(2)">
              Yes, delete
            </button>
          } @else {
            <button type="button" class="flex-1 rounded-md bg-danger px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
              [disabled]="busy() || (self() && !password())" (click)="submit()">
              {{ busy() ? 'Deleting...' : 'Delete permanently' }}
            </button>
          }
        </div>
      </div>
    </div>
  `
})
export class DeleteAccountDialog {
  readonly name = input('');
  readonly self = input(false);
  readonly busy = input(false);
  readonly error = input<string | null>(null);

  readonly confirmed = output<string | null>();
  readonly cancelled = output<void>();

  protected readonly step = signal<1 | 2>(1);
  protected readonly password = signal('');

  protected readonly deleted = [
    'Profile, photo and cover',
    'All flights, PIREPs and flight logs',
    'Hours, points and rank',
    'Monthly roster and bookings',
    'Login and email'
  ];

  protected submit(): void {
    if (this.busy() || (this.self() && !this.password())) return;
    this.confirmed.emit(this.self() ? this.password() : null);
  }

  protected cancel(): void {
    if (!this.busy()) this.cancelled.emit();
  }
}