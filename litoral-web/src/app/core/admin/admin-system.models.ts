export type HealthState = 'Ok' | 'Failing' | 'Late' | 'Running' | 'Waiting' | 'Idle';

export interface SystemStatus {
  generatedAt: string;
  api: {
    version: string;
    environment: string;
    runtime: string;
    startedAt: string;
    uptimeSeconds: number;
    memoryMb: number;
  };
  database: {
    online: boolean;
    latencyMs: number | null;
    sizeBytes: number | null;
    pendingMigrations: number | null;
    error: string | null;
  };
  traffic: {
    requestsLastHour: number;
    serverErrorsLastHour: number;
    averageMsLastHour: number | null;
    requestsSinceStart: number;
    serverErrorsSinceStart: number;
    minutes: { minute: string; requests: number; errors: number }[];
  };
  integrations: {
    name: string;
    description: string;
    state: HealthState;
    lastSuccessAt: string | null;
    lastErrorAt: string | null;
    lastError: string | null;
    lastLatencyMs: number | null;
    calls: number;
    failures: number;
  }[];
  jobs: {
    name: string;
    intervalMinutes: number;
    state: HealthState;
    lastRunAt: string | null;
    lastSuccessAt: string | null;
    lastDurationMs: number | null;
    lastErrorAt: string | null;
    lastError: string | null;
    nextRunAt: string | null;
    runs: number;
    failures: number;
  }[];
  errors: {
    at: string;
    method: string;
    path: string;
    status: number;
    type: string;
    message: string;
    traceId: string;
  }[];
  numbers: {
    flightsInProgress: number;
    liveSessions: number;
    openBookings: number;
    pendingPireps: number;
    aircraftInMaintenance: number;
    aircraftAog: number;
    activePilots: number;
    pendingApplications: number;
    openPrivacyRequests: number;
  } | null;
  storage: {
    uploadsBytes: number;
    uploadsFiles: number;
    downloadsBytes: number;
    downloadsFiles: number;
  };
}

export const HEALTH_STATE_CLASS: Record<HealthState, string> = {
  Ok: 'bg-onda-500/15 text-onda-600',
  Failing: 'bg-danger/15 text-danger',
  Late: 'bg-highlight/25 text-ink',
  Running: 'bg-primary/15 text-primary',
  Waiting: 'bg-surface-alt text-ink-muted',
  Idle: 'bg-surface-alt text-ink-muted'
};