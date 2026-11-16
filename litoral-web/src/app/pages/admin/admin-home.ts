import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { STAFF_POSITION_NAMES } from '../../core/staff/staff-positions';

@Component({
  selector: 'app-admin-home',
  template: `
    <div class="card flex flex-col gap-4">
      <div>
        <h1 class="text-2xl font-bold text-ink">Administration</h1>
        <p class="text-ink-muted">Areas available for your staff positions.</p>
      </div>
      <div class="flex flex-wrap gap-2">
        @for (position of auth.pilot()?.staffPositions ?? []; track position) {
          <span class="rounded-full bg-primary/10 px-3 py-1 text-sm font-medium text-primary">{{ staffNames[position] ?? position }}</span>
        }
      </div>
    </div>
  `
})
export class AdminHome {
  protected readonly auth = inject(AuthService);
  protected readonly staffNames = STAFF_POSITION_NAMES;
}