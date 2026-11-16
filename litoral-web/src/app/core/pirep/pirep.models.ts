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
  category: string;
  check: string;
  description: string;
  outcome: 'Points' | 'Reject';
  points: number;
  quantity: number;
  isManual: boolean;
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

export const PIREP_STATUS_CLASS: Record<PirepStatus, string> = {
  Pending: 'bg-highlight/20 text-ink',
  Approved: 'bg-onda-500/15 text-onda-600',
  Rejected: 'bg-danger/10 text-danger'
};