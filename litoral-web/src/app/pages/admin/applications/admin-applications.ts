import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { apiErrorMessage } from '../../../core/api/api-error';
import { ApplicationsService, PilotApplication } from '../../../core/applications/applications.service';
import { ConfirmService } from '../../../shared/confirm-dialog/confirm.service';

@Component({
  selector: 'app-admin-applications',
  imports: [DatePipe],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-ink">Applications</h1>
          <p class="text-ink-muted">Check the profile of each candidate on the network before you approve.</p>
        </div>

        @if (registrationOpen() !== null) {
          <div class="panel flex items-center gap-4 px-4 py-3">
            <div class="text-sm">
              <p class="font-semibold text-ink">{{ registrationOpen() ? 'Applications are open' : 'Applications are closed' }}</p>
              <p class="text-xs text-ink-muted">{{ registrationOpen() ? 'New pilots can apply.' : 'The Apply button is disabled.' }}</p>
            </div>
            <button type="button" role="switch" [attr.aria-checked]="registrationOpen()" aria-label="Accept new applications"
              class="relative h-7 w-12 shrink-0 rounded-full transition-colors disabled:opacity-50"
              [class]="registrationOpen() ? 'bg-onda-500' : 'bg-line'" [disabled]="busy()" (click)="toggleRegistration()">
              <span class="absolute top-1 h-5 w-5 rounded-full bg-white shadow transition-all" [class]="registrationOpen() ? 'left-6' : 'left-1'"></span>
            </button>
          </div>
        }
      </div>

      @if (loading()) {
        <p class="panel px-6 py-12 text-center text-ink-muted">Loading...</p>
      } @else if (loadError()) {
        <div class="panel flex flex-col items-center gap-3 px-6 py-12 text-center">
          <p class="text-danger">Something went wrong. Try again.</p>
          <button type="button" class="btn-secondary" (click)="load()">Try again</button>
        </div>
      } @else if (applications().length === 0) {
        <p class="panel px-6 py-12 text-center text-ink-muted">No applications waiting for review.</p>
      } @else {
        <div class="flex flex-col gap-4">
          @for (a of applications(); track a.id) {
            <article class="panel p-5">
              <div class="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <p class="text-lg font-semibold text-ink">{{ a.firstName }} {{ a.lastName }}</p>
                  <p class="text-sm text-ink-muted">{{ a.email }} · base {{ a.hubIcao }}</p>
                </div>
                <p class="text-xs text-ink-muted">Applied {{ a.createdAt | date: 'd MMM y, HH:mm' }}</p>
              </div>

              <div class="mt-4 grid gap-4 md:grid-cols-3">
                <div class="text-sm">
                  <p class="text-xs font-semibold uppercase tracking-wide text-ink-muted">Networks</p>
                  @if (a.vatsimId) {
                    <a [href]="'https://stats.vatsim.net/stats/' + a.vatsimId" target="_blank" rel="noopener"
                      class="mt-1 flex items-center gap-1.5 font-medium text-primary hover:underline">
                      VATSIM {{ a.vatsimId }} <i class="ti ti-external-link text-xs" aria-hidden="true"></i>
                    </a>
                    @if (a.vatsimInUse) { <p class="text-xs text-danger">Already used by a Litoral pilot</p> }
                  }
                  @if (a.ivaoId) {
                    <a [href]="'https://www.ivao.aero/Member.aspx?Id=' + a.ivaoId" target="_blank" rel="noopener"
                      class="mt-1 flex items-center gap-1.5 font-medium text-primary hover:underline">
                      IVAO {{ a.ivaoId }} <i class="ti ti-external-link text-xs" aria-hidden="true"></i>
                    </a>
                    @if (a.ivaoInUse) { <p class="text-xs text-danger">Already used by a Litoral pilot</p> }
                  }
                </div>
                <div class="text-sm">
                  <p class="text-xs font-semibold uppercase tracking-wide text-ink-muted">Simulator</p>
                  <p class="mt-1 text-ink">{{ a.simulator }}</p>
                </div>
                <div class="text-sm">
                  <p class="text-xs font-semibold uppercase tracking-wide text-ink-muted">Aircraft add-ons</p>
                  <div class="mt-1 flex flex-wrap gap-1">
                    @for (addOn of a.addOns; track addOn) {
                      <span class="rounded-full bg-primary/10 px-2 py-0.5 text-xs font-semibold text-primary">{{ addOn }}</span>
                    }
                  </div>
                </div>
              </div>

              <div class="mt-4">
                <p class="text-xs font-semibold uppercase tracking-wide text-ink-muted">Experience and motivation</p>
                <p class="mt-1 whitespace-pre-line rounded-md bg-surface-alt px-3 py-2 text-sm text-ink">{{ a.motivation }}</p>
              </div>

              @if (rejecting() === a.id) {
                <div class="mt-4 flex flex-col gap-2">
                  <textarea rows="2" maxlength="500" class="field-input" placeholder="Reason for the rejection (kept in the logs)"
                    [value]="reason()" (input)="reason.set($any($event.target).value)" aria-label="Reason"></textarea>
                  <div class="flex flex-wrap gap-2">
                    <button type="button" class="rounded-md bg-danger px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50"
                      [disabled]="busy() || !reason().trim()" (click)="reject(a)">Reject and delete</button>
                    <button type="button" class="btn-secondary" (click)="rejecting.set(null)">Cancel</button>
                  </div>
                </div>
              } @else {
                <div class="mt-4 flex flex-wrap gap-2">
                  <button type="button" class="btn-primary flex items-center gap-1.5 px-4" [disabled]="busy() || a.vatsimInUse || a.ivaoInUse" (click)="approve(a)">
                    <i class="ti ti-check" aria-hidden="true"></i>Approve
                  </button>
                  <button type="button" class="rounded-md border border-danger px-4 py-2 text-sm font-medium text-danger hover:bg-danger hover:text-white disabled:opacity-50"
                    [disabled]="busy()" (click)="startReject(a)">Reject</button>
                </div>
              }

              @if (rowError()?.id === a.id) {
                <p class="mt-2 text-sm text-danger">{{ rowError()!.message }}</p>
              }
            </article>
          }
        </div>
      }
    </div>
  `
})
export class AdminApplications implements OnInit {
  private readonly api = inject(ApplicationsService);
  private readonly confirm = inject(ConfirmService);

  protected readonly applications = signal<PilotApplication[]>([]);
  protected readonly registrationOpen = signal<boolean | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly busy = signal(false);
  protected readonly rejecting = signal<string | null>(null);
  protected readonly reason = signal('');
  protected readonly rowError = signal<{ id: string; message: string } | null>(null);

  ngOnInit(): void {
    this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(false);
    try {
      const result = await this.api.list();
      this.applications.set(result.applications);
      this.registrationOpen.set(result.registrationOpen);
    } catch {
      this.loadError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected async toggleRegistration(): Promise<void> {
    const open = !this.registrationOpen();
    const ok = await this.confirm.ask({
      title: open ? 'Open applications?' : 'Close applications?',
      message: open
        ? 'New pilots will be able to send applications from the Regulations page.'
        : 'The Apply button will be disabled. Applications already sent stay here for review.',
      confirmLabel: open ? 'Yes, open' : 'Yes, close',
      tone: open ? 'success' : 'warning',
      icon: open ? 'ti-door' : 'ti-door-off'
    });
    if (!ok) return;

    this.busy.set(true);
    try {
      await this.api.setRegistrationOpen(open);
      this.registrationOpen.set(open);
    } catch {
      await this.load();
    } finally {
      this.busy.set(false);
    }
  }

  protected async approve(a: PilotApplication): Promise<void> {
    this.rowError.set(null);
    const ok = await this.confirm.ask({
      title: `Approve ${a.firstName} ${a.lastName}?`,
      message: `The account is created as First Officer at ${a.hubIcao} and the pilot gets a pilot number. The pilot is notified.`,
      confirmLabel: 'Yes, approve',
      tone: 'success',
      icon: 'ti-user-check'
    });
    if (!ok) return;

    await this.run(a, () => this.api.approve(a.id));
  }

  protected startReject(a: PilotApplication): void {
    this.rowError.set(null);
    this.reason.set('');
    this.rejecting.set(a.id);
  }

  protected async reject(a: PilotApplication): Promise<void> {
    const ok = await this.confirm.ask({
      title: `Reject ${a.firstName} ${a.lastName}?`,
      message: 'The account and the application are deleted now. The same e-mail can apply again after 30 days.',
      confirmLabel: 'Yes, reject',
      tone: 'danger',
      icon: 'ti-user-x'
    });
    if (!ok) return;

    await this.run(a, () => this.api.reject(a.id, this.reason().trim()));
  }

  private async run(a: PilotApplication, action: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    try {
      await action();
      this.applications.update(list => list.filter(x => x.id !== a.id));
      this.rejecting.set(null);
    } catch (error) {
      this.rowError.set({ id: a.id, message: apiErrorMessage(error) ?? 'Something went wrong. Try again.' });
    } finally {
      this.busy.set(false);
    }
  }
}