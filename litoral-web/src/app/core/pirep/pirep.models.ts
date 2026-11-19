import { FlightMelCheck } from '../mel/mel.models';
export type PirepStatus = 'Pending' | 'Approved' | 'Rejected';
export type FlightNetwork = 'Offline' | 'Vatsim' | 'Ivao';
export type SimulationScenario = 'clean' | 'violations' | 'rejected';

export interface PirepSummary {
  id: string;
  callsign: string;
  departureIcao: string;
  arrivalIcao: string;
  plannedArrivalIcao: string;
  registration: string;
  aircraftTypeIcao: string;
  source: 'Acars' | 'Manual';
  status: PirepStatus;
  network: FlightNetwork;
  blockMinutes: number;
  landingRateFpm: number | null;
  scorePoints: number;
  hasRejection: boolean;
  conformity: number;
  submittedAt: string;
}

export interface PirepScoreItem {
  ruleId: string | null;
  category: string;
  check: string;
  description: string;
  outcome: 'Points' | 'Reject';
  points: number;
  quantity: number;
  isManual: boolean;
  isVoided: boolean;
}

export interface PirepEvent {
  type: string;
  at: string;
  durationSeconds: number;
  value: number | null;
  detail: string | null;
}

export interface TrackPoint {
  at: string;
  latitude: number;
  longitude: number;
  altitudeFt: number;
  groundSpeedKt: number;
}

export interface Pirep {
  summary: PirepSummary;
  pilotId: string;
  pilotName: string;
  blockOffAt: string;
  takeoffAt: string | null;
  landingAt: string | null;
  blockOnAt: string;
  airMinutes: number | null;
  fuelUsedKg: number | null;
  fuelAtPushbackKg: number | null;
  distanceNm: number | null;
  flightRoute: string | null;
  cruiseAltitudeFt: number | null;
  onlinePercent: number | null;
  comments: string | null;
  reviewedAt: string | null;
  reviewNote: string | null;
  items: PirepScoreItem[];
  events: PirepEvent[];
  track: TrackPoint[];
}

export interface ManualPirepPayload {
  arrivalIcao: string;
  network: FlightNetwork;
  blockOffAt: string;
  takeoffAt: string | null;
  landingAt: string | null;
  blockOnAt: string;
  landingRateFpm: number | null;
  fuelUsedKg: number | null;
  flightRoute: string | null;
  comments: string;
}

export const NETWORK_LABELS: Record<FlightNetwork, string> = {
  Offline: 'Offline',
  Vatsim: 'VATSIM',
  Ivao: 'IVAO'
};

export const NETWORK_LOGOS: Partial<Record<FlightNetwork, string>> = {
  Vatsim: '/networks/vatsim.png',
  Ivao: '/networks/ivao.png'
};

export const NETWORK_LOGO_LIGHT_INVERT: Partial<Record<FlightNetwork, boolean>> = {
  Ivao: true,
  Vatsim: true
};

const NETWORK_TRACKERS: Partial<Record<FlightNetwork, { name: string; url: (callsign: string) => string }>> = {
  Vatsim: { name: 'VATSIM Radar', url: callsign => `https://vatsim-radar.com/?pilot=${encodeURIComponent(callsign)}` },
  Ivao: { name: 'Webeye', url: callsign => `https://webeye.ivao.aero/?callsign=${encodeURIComponent(callsign)}` }
};

export function networkTracker(network: FlightNetwork, callsign: string): { name: string; url: string } | null {
  const tracker = NETWORK_TRACKERS[network];
  return tracker ? { name: tracker.name, url: tracker.url(callsign) } : null;
}

export const PIREP_STATUS_CLASS: Record<PirepStatus, string> = {
  Pending: 'bg-highlight/20 text-ink',
  Approved: 'bg-onda-500/15 text-onda-600',
  Rejected: 'bg-danger/10 text-danger'
};

export interface AdminPirepListItem {
  summary: PirepSummary;
  pilotId: string;
  pilotName: string;
  isDiversion: boolean;
  reviewedAt: string | null;
}

export interface ReviewRule {
  id: string;
  category: string;
  description: string;
  outcome: 'Points' | 'Reject';
  points: number;
  mode: string;
}

export interface RejectionPreview {
  rejectionNumber: number;
  penalty: number;
  deactivatesAccount: boolean;
}

export interface AdminPirep {
  pirep: Pirep;
  pilotEmail: string;
  pilotPoints: number;
  pilotIsActive: boolean;
  reviewedByName: string | null;
  reviewRules: ReviewRule[];
  rejectionPreview: RejectionPreview;
  melChecks: FlightMelCheck[];
}