export type TourState = 'Draft' | 'ComingSoon' | 'Open' | 'Ended';
export type TourLegState = 'Booked' | 'InProgress' | 'PendingReview';

export interface TourAirport {
  icao: string;
  name: string;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
}

export interface TourLeg {
  order: number;
  callsign: string;
  departure: TourAirport;
  arrival: TourAirport;
  distanceNm: number;
  estimatedMinutes: number;
}

export interface TourAward {
  id: string;
  name: string;
  imageUrl: string | null;
}

export interface TourProgress {
  completedLegs: number;
  startedAt: string;
  completedAt: string | null;
  currentLegState: TourLegState | null;
}

export interface PilotTour {
  id: string;
  title: string;
  description: string;
  imageUrl: string | null;
  startsAt: string | null;
  endsAt: string | null;
  state: TourState;
  minimumRankName: string;
  isCargo: boolean;
  aircraftTypes: string[];
  bonusPoints: number;
  award: TourAward;
  legs: TourLeg[];
  totalDistanceNm: number;
  pilotsCompleted: number;
  progress: TourProgress | null;
  nextLegOrder: number | null;
  canBook: boolean;
  blockedReason: string | null;
}

export interface AdminTour {
  id: string;
  title: string;
  description: string;
  imageUrl: string | null;
  startsAt: string | null;
  endsAt: string | null;
  state: TourState;
  isPublished: boolean;
  minimumRankLevel: number;
  minimumRankName: string;
  isCargo: boolean;
  aircraftTypes: string[];
  bonusPoints: number;
  award: TourAward;
  legs: TourLeg[];
  pilotsStarted: number;
  pilotsCompleted: number;
  legsLocked: boolean;
}

export interface TourOptions {
  airports: { icao: string; name: string; city: string; minimumRankLevel: number }[];
  ranks: { code: string; name: string; level: number }[];
  aircraftTypes: { icaoCode: string; name: string; maxRangeNm: number; isCargo: boolean }[];
}

export interface TourPayload {
  title: string;
  description: string;
  startsAt: string | null;
  endsAt: string | null;
  minimumRank: string;
  isCargo: boolean;
  aircraftTypes: string[];
  bonusPoints: number;
  stops: string[];
  isPublished: boolean;
}

export const TOUR_STATE: Record<TourState, { label: string; class: string }> = {
  Draft: { label: 'Draft', class: 'bg-surface-alt text-ink-muted' },
  ComingSoon: { label: 'Coming soon', class: 'bg-highlight text-on-highlight' },
  Open: { label: 'Open', class: 'bg-onda-500 text-white' },
  Ended: { label: 'Ended', class: 'bg-ink-muted text-white' }
};