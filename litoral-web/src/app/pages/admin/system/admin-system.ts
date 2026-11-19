import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { apiErrorMessage } from '../../../core/api/api-error';
import { HEALTH_STATE_CLASS, HealthState, SystemStatus } from '../../../core/admin/admin-system.models';
import { AdminSystemService } from '../../../core/admin/admin-system.service';

const REFRESH_MS = 30 * 1000;

@Component({
  selector: 'app-admin-system',
  imports: [DatePipe, DecimalPipe],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-ink">System</h1>
          <p class="text-ink-muted">API, database, integrations and background jobs. Refreshes every 30 seconds.</p>
        </div>
        <div class="flex items-center gap-3">
          @if (status(); as s) {
            <span class="flex items-center gap-2 text-sm text-ink-muted">
              <span class="h-2 w-2 rounded-full" [class]="healthy() ? 'bg-onda-500' : 'bg-danger'"></span>
              Updated {{ s.generatedAt | date: 'HH:mm:ss' : 'UTC' }}Z
            </span>
          }
          <button type="button" class="btn-secondary flex items-center gap-2" [disabled]="loading()" (click)="load()">
            <i class="ti ti-refresh" [class.animate-spin]="loading()" aria-hidden="true"></i>Refresh
          </button>
        </div>
      </div>

      @if (error(); as message) {
        <p class="rounded-lg bg-danger/10 px-4 py-3 text-sm text-danger">
          <i class="ti ti-alert-circle mr-1" aria-hidden="true"></i>{{ message }}
        </p>
      }

      @if (!status() && loading()) {
        <p class="panel px-6 py-16 text-center text-ink-muted">Loading...</p>
      } @else if (status(); as s) {
        <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <div class="panel card-hover flex flex-col gap-2 p-5">
            <p class="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-ink-muted"><i class="ti ti-server text-base" aria-hidden="true"></i>API</p>
            <p class="text-2xl font-bold text-onda-600">Online</p>
            <p class="text-sm text-ink">Up {{ uptime(s.api.uptimeSeconds) }}</p>
            <p class="text-xs text-ink-muted">{{ s.api.version }} · {{ s.api.environment }}</p>
            <p class="text-xs text-ink-muted">{{ s.api.runtime }} · {{ s.api.memoryMb | number }} MB</p>
          </div>

          <div class="panel card-hover flex flex-col gap-2 p-5">
            <p class="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-ink-muted"><i class="ti ti-database text-base" aria-hidden="true"></i>Database</p>
            <p class="text-2xl font-bold" [class]="s.database.online ? 'text-onda-600' : 'text-danger'">{{ s.database.online ? 'Online' : 'Offline' }}</p>
            @if (s.database.online) {
              <p class="text-sm text-ink">{{ s.database.latencyMs }} ms to connect</p>
              <p class="text-xs text-ink-muted">{{ s.database.sizeBytes !== null ? bytes(s.database.sizeBytes) : 'Size unavailable' }}</p>
              <p class="text-xs" [class]="s.database.pendingMigrations ? 'font-semibold text-danger' : 'text-ink-muted'">
                {{ s.database.pendingMigrations ? s.database.pendingMigrations + ' pending migration(s)' : 'Migrations up to date' }}
              </p>
            } @else {
              <p class="text-xs text-danger">{{ s.database.error }}</p>
            }
          </div>

          <div class="panel card-hover flex flex-col gap-2 p-5">
            <p class="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-ink-muted"><i class="ti ti-activity text-base" aria-hidden="true"></i>Last hour</p>
            <p class="text-2xl font-bold text-ink">{{ s.traffic.requestsLastHour | number }} <span class="text-sm font-medium text-ink-muted">requests</span></p>
            <p class="text-sm" [class]="s.traffic.serverErrorsLastHour ? 'font-semibold text-danger' : 'text-ink'">{{ s.traffic.serverErrorsLastHour }} server errors</p>
            <p class="text-xs text-ink-muted">{{ s.traffic.averageMsLastHour !== null ? s.traffic.averageMsLastHour + ' ms average' : 'No requests yet' }}</p>
            <p class="text-xs text-ink-muted">{{ s.traffic.requestsSinceStart | number }} since start · {{ s.traffic.serverErrorsSinceStart }} errors</p>
          </div>

          <div class="panel card-hover flex flex-col gap-2 p-5">
            <p class="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-ink-muted"><i class="ti ti-folders text-base" aria-hidden="true"></i>Storage</p>
            <p class="text-2xl font-bold text-ink">{{ bytes(s.storage.uploadsBytes + s.storage.downloadsBytes) }}</p>
            <p class="text-sm text-ink">Uploads {{ bytes(s.storage.uploadsBytes) }} · {{ s.storage.uploadsFiles | number }} files</p>
            <p class="text-sm text-ink">Downloads {{ bytes(s.storage.downloadsBytes) }} · {{ s.storage.downloadsFiles | number }} files</p>
            <p class="text-xs text-ink-muted">Checked every 5 minutes</p>
          </div>
        </div>

        <div class="panel flex flex-col gap-3 p-5">
          <div class="flex items-center justify-between">
            <h2 class="font-semibold text-ink">Requests per minute</h2>
            <span class="flex items-center gap-3 text-xs text-ink-muted">
              <span class="flex items-center gap-1"><span class="h-2 w-2 rounded-sm bg-primary"></span>Requests</span>
              <span class="flex items-center gap-1"><span class="h-2 w-2 rounded-sm bg-danger"></span>Server errors</span>
            </span>
          </div>
          <div class="flex h-28 items-end gap-[2px]">
            @for (m of s.traffic.minutes; track m.minute) {
              <div class="flex h-full flex-1 flex-col justify-end" [title]="(m.minute | date: 'HH:mm' : 'UTC') + 'Z · ' + m.requests + ' requests' + (m.errors ? ', ' + m.errors + ' errors' : '')">
                @if (m.errors) {
                  <div class="w-full rounded-t-sm bg-danger" [style.height.%]="barHeight(m.errors)"></div>
                }
                <div class="w-full bg-primary/70" [class.rounded-t-sm]="!m.errors" [style.height.%]="barHeight(m.requests - m.errors)"></div>
              </div>
            }
          </div>
          <div class="flex justify-between text-[11px] text-ink-muted">
            <span>{{ s.traffic.minutes[0]?.minute | date: 'HH:mm' : 'UTC' }}Z</span>
            <span>Now</span>
          </div>
        </div>

        @if (s.numbers; as n) {
          <div class="grid grid-cols-2 gap-4 md:grid-cols-3">
            @for (kpi of kpis(); track kpi.label) {
              <div class="panel card-hover flex items-center gap-3 p-4">
                <span class="stat-icon h-10 w-10 text-xl"><i class="ti {{ kpi.icon }}" aria-hidden="true"></i></span>
                <div>
                  <p class="text-xl font-bold" [class]="kpi.alert ? 'text-danger' : 'text-ink'">{{ kpi.value | number }}</p>
                  <p class="text-xs text-ink-muted">{{ kpi.label }}</p>
                </div>
              </div>
            }
          </div>
        }

        <div class="grid gap-6 xl:grid-cols-2">
          <div class="panel flex flex-col">
            <h2 class="flex items-center gap-2 border-b border-line px-5 py-4 font-semibold text-ink">
              <i class="ti ti-plug-connected text-lg text-primary" aria-hidden="true"></i>Integrations
            </h2>
            <div class="divide-y divide-line">
              @for (i of s.integrations; track i.name) {
                <div class="flex flex-col gap-1 px-5 py-3">
                  <div class="flex items-center justify-between gap-3">
                    <p class="font-semibold text-ink">{{ i.name }} <span class="text-xs font-normal text-ink-muted">· {{ i.description }}</span></p>
                    <span class="rounded-full px-2 py-0.5 text-[11px] font-bold" [class]="stateClass(i.state)">{{ i.state }}</span>
                  </div>
                  <p class="text-xs text-ink-muted">
                    @if (i.calls) {
                      {{ i.calls | number }} calls · {{ i.failures | number }} failed · last {{ i.lastLatencyMs }} ms
                      @if (i.lastSuccessAt) { · OK {{ ago(i.lastSuccessAt) }} }
                    } @else {
                      Not called since the API started
                    }
                  </p>
                  @if (i.lastError) {
                    <p class="text-xs" [class]="i.state === 'Failing' ? 'text-danger' : 'text-ink-muted'">Last error {{ ago(i.lastErrorAt!) }}: {{ i.lastError }}</p>
                  }
                </div>
              }
            </div>
            <p class="border-t border-line px-5 py-3 text-xs text-ink-muted">VATSIM and IVAO are opened by the pilot's browser, so the API doesn't call them.</p>
          </div>

          <div class="panel flex flex-col">
            <h2 class="flex items-center gap-2 border-b border-line px-5 py-4 font-semibold text-ink">
              <i class="ti ti-clock-cog text-lg text-primary" aria-hidden="true"></i>Background jobs
            </h2>
            <div class="divide-y divide-line">
              @for (j of s.jobs; track j.name) {
                <div class="flex flex-col gap-1 px-5 py-3">
                  <div class="flex items-center justify-between gap-3">
                    <p class="font-semibold text-ink">{{ j.name }} <span class="text-xs font-normal text-ink-muted">· every {{ every(j.intervalMinutes) }}</span></p>
                    <span class="rounded-full px-2 py-0.5 text-[11px] font-bold" [class]="stateClass(j.state)">{{ j.state }}</span>
                  </div>
                  <p class="text-xs text-ink-muted">
                    @if (j.lastRunAt) {
                      Last run {{ ago(j.lastRunAt) }}{{ j.lastDurationMs !== null ? ' in ' + j.lastDurationMs + ' ms' : '' }}
                      · {{ j.runs | number }} runs · {{ j.failures | number }} failed
                      @if (j.nextRunAt) { · next {{ j.nextRunAt | date: 'HH:mm' : 'UTC' }}Z }
                    } @else {
                      Waiting for the first run
                    }
                  </p>
                  @if (j.lastError) {
                    <p class="text-xs" [class]="j.state === 'Failing' ? 'text-danger' : 'text-ink-muted'">Last error {{ ago(j.lastErrorAt!) }}: {{ j.lastError }}</p>
                  }
                </div>
              } @empty {
                <p class="px-5 py-6 text-center text-sm text-ink-muted">No jobs reported yet.</p>
              }
            </div>
          </div>
        </div>

        <div class="panel flex flex-col">
          <h2 class="flex items-center gap-2 border-b border-line px-5 py-4 font-semibold text-ink">
            <i class="ti ti-bug text-lg text-danger" aria-hidden="true"></i>Recent server errors
            <span class="text-sm font-normal text-ink-muted">· last 50 since the API started</span>
          </h2>
          @if (s.errors.length) {
            <div class="divide-y divide-line">
              @for (e of s.errors; track $index) {
                <div class="flex flex-col gap-1 px-5 py-3 text-sm">
                  <p class="flex flex-wrap items-center gap-2">
                    <span class="rounded bg-danger/15 px-1.5 py-0.5 font-mono text-[11px] font-bold text-danger">{{ e.status }}</span>
                    <span class="font-mono text-xs font-semibold text-ink">{{ e.method }} {{ e.path }}</span>
                    <span class="text-xs text-ink-muted">{{ e.at | date: 'd MMM, HH:mm:ss' : 'UTC' }}Z</span>
                  </p>
                  <p class="text-ink"><span class="font-semibold">{{ e.type }}:</span> {{ e.message }}</p>
                  <p class="font-mono text-[11px] text-ink-muted">Trace {{ e.traceId }}</p>
                </div>
              }
            </div>
          } @else {
            <p class="flex items-center justify-center gap-2 px-5 py-8 text-sm text-ink-muted">
              <i class="ti ti-circle-check text-lg text-onda-500" aria-hidden="true"></i>No server errors since the API started.
            </p>
          }
        </div>
      }
    </div>
  `
})
export class AdminSystem implements OnInit {
  private readonly api = inject(AdminSystemService);
  private timer?: ReturnType<typeof setInterval>;

  protected readonly status = signal<SystemStatus | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly healthy = computed(() => {
    const s = this.status();
    return !!s && s.database.online
      && !s.jobs.some(j => j.state === 'Failing' || j.state === 'Late')
      && !s.integrations.some(i => i.state === 'Failing');
  });

  protected readonly peak = computed(() => Math.max(1, ...(this.status()?.traffic.minutes.map(m => m.requests) ?? [])));

  protected readonly kpis = computed(() => {
    const n = this.status()?.numbers;
    if (!n) return [];
    return [
      { label: 'Flights in progress', value: n.flightsInProgress, icon: 'ti-plane-departure', alert: false },
      { label: 'Live ACARS sessions', value: n.liveSessions, icon: 'ti-broadcast', alert: false },
      { label: 'Open bookings', value: n.openBookings, icon: 'ti-ticket', alert: false },
      { label: 'PIREPs pending', value: n.pendingPireps, icon: 'ti-clipboard-check', alert: false },
      { label: 'Active pilots', value: n.activePilots, icon: 'ti-users', alert: false },
      { label: 'Aircraft in maintenance', value: n.aircraftInMaintenance, icon: 'ti-tool', alert: false },
      { label: 'Aircraft AOG', value: n.aircraftAog, icon: 'ti-alert-octagon', alert: n.aircraftAog > 0 },
      { label: 'Applications waiting', value: n.pendingApplications, icon: 'ti-user-plus', alert: false },
      { label: 'Open privacy requests', value: n.openPrivacyRequests, icon: 'ti-lock', alert: false }
    ];
  });

  constructor() {
    inject(DestroyRef).onDestroy(() => clearInterval(this.timer));
  }

  ngOnInit(): void {
    this.load();
    this.timer = setInterval(() => {
      if (!document.hidden) this.load();
    }, REFRESH_MS);
  }

  protected async load(): Promise<void> {
    if (this.loading()) return;
    this.loading.set(true);
    try {
      this.status.set(await this.api.status());
      this.error.set(null);
    } catch (error) {
      this.error.set(apiErrorMessage(error) ?? 'Could not reach the API. It may be offline.');
    } finally {
      this.loading.set(false);
    }
  }

  protected stateClass(state: HealthState): string {
    return HEALTH_STATE_CLASS[state] ?? HEALTH_STATE_CLASS.Idle;
  }

  protected barHeight(value: number): number {
    return value <= 0 ? 0 : Math.max(3, (value / this.peak()) * 100);
  }

  protected uptime(seconds: number): string {
    const days = Math.floor(seconds / 86400);
    const hours = Math.floor((seconds % 86400) / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    if (days > 0) return `${days}d ${hours}h`;
    if (hours > 0) return `${hours}h ${minutes}m`;
    return `${minutes}m`;
  }

  protected every(minutes: number): string {
    if (minutes >= 1440 && minutes % 1440 === 0) return minutes === 1440 ? 'day' : `${minutes / 1440} days`;
    if (minutes >= 60 && minutes % 60 === 0) return minutes === 60 ? 'hour' : `${minutes / 60} hours`;
    return minutes === 1 ? 'minute' : `${minutes} minutes`;
  }

  protected ago(value: string): string {
    const seconds = Math.max(0, Math.round((Date.now() - new Date(value).getTime()) / 1000));
    if (seconds < 60) return `${seconds}s ago`;
    if (seconds < 3600) return `${Math.floor(seconds / 60)} min ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)} h ago`;
    return `${Math.floor(seconds / 86400)} d ago`;
  }

  protected bytes(value: number): string {
    if (value < 1024) return `${value} B`;
    const units = ['KB', 'MB', 'GB', 'TB'];
    let size = value / 1024;
    let unit = 0;
    while (size >= 1024 && unit < units.length - 1) {
      size /= 1024;
      unit++;
    }
    return `${size.toFixed(size >= 100 ? 0 : 1)} ${units[unit]}`;
  }
}