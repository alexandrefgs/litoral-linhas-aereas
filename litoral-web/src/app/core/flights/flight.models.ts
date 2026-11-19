import { FlightNetwork, PirepEvent, PirepScoreItem, PirepStatus, TrackPoint } from '../pirep/pirep.models';

export type BookingType = 'Random' | 'Custom' | 'Monthly' | 'Special' | 'Tour';

export interface FlightCrewMember {
  pilotNumber: number | null;
  displayId: string | null;
  name: string;
  rankCode: string;
  rankName: string;
  photoUrl: string | null;
  flights: number;
  minutes: number;
  points: number;
  isVirtual: boolean;
}

export interface FlightAirport {
  icao: string;
  iata: string | null;
  name: string;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
  photoUrl: string | null;
}

export interface FlightAircraft {
  registration: string;
  typeIcao: string;
  typeName: string;
  variant: string | null;
  photoUrl: string | null;
  photoCredit: string | null;
  isCargo: boolean;
}

export interface FlightDetail {
  id: string;
  callsign: string;
  status: PirepStatus;
  source: 'Acars' | 'Manual';
  bookingType: BookingType;
  network: FlightNetwork;
  simulator: string | null;
  departure: FlightAirport;
  arrival: FlightAirport;
  plannedArrivalIcao: string;
  captain: FlightCrewMember;
  firstOfficer: FlightCrewMember;
  aircraft: FlightAircraft;
  blockOffAt: string;
  takeoffAt: string | null;
  landingAt: string | null;
  blockOnAt: string;
  blockMinutes: number;
  airMinutes: number | null;
  landingRateFpm: number | null;
  fuelUsedKg: number | null;
  fuelAtPushbackKg: number | null;
  distanceNm: number | null;
  flightRoute: string | null;
  cruiseAltitudeFt: number | null;
  onlinePercent: number | null;
  scorePoints: number;
  conformity: number;
  hasRejection: boolean;
  submittedAt: string;
  reviewedAt: string | null;
  comments: string | null;
  reviewNote: string | null;
  canSeePrivate: boolean;
  items: PirepScoreItem[];
  events: PirepEvent[];
  track: TrackPoint[];
}

export interface RecentFlight {
  id: string;
  callsign: string;
  departureIcao: string;
  arrivalIcao: string;
  departureCountry: string;
  registration: string;
  typeName: string;
  variant: string | null;
  aircraftPhotoUrl: string | null;
  isCargo: boolean;
  blockMinutes: number;
  network: FlightNetwork;
  status: PirepStatus;
  pilotNumber: number;
  pilotName: string;
  rankCode: string;
  pilotPhotoUrl: string | null;
  submittedAt: string;
}

export interface RecentFlights {
  monthTotal: number;
  flights: RecentFlight[];
}

export const BOOKING_TYPE_LABEL: Record<BookingType, string> = {
  Random: 'Random flight',
  Custom: 'Custom flight',
  Monthly: 'Monthly roster',
  Special: 'Special flight',
  Tour: 'Tour leg'
};

export interface LandingGrade {
  grade: string;
  label: string;
  color: string;
}

export function landingGrade(fpm: number | null): LandingGrade | null {
  if (fpm === null) return null;
  const rate = Math.abs(fpm);
  if (rate <= 100) return { grade: 'A', label: 'Smooth', color: '#2EC4B6' };
  if (rate <= 200) return { grade: 'B', label: 'Good', color: '#22A699' };
  if (rate <= 300) return { grade: 'C', label: 'Firm', color: '#F5B83D' };
  if (rate <= 400) return { grade: 'D', label: 'Hard', color: '#E09B1A' };
  if (rate <= 500) return { grade: 'E', label: 'Very hard', color: '#EF476F' };
  return { grade: 'F', label: 'Unacceptable', color: '#EF476F' };
}

export function hhmm(minutes: number): string {
  return `${Math.floor(minutes / 60).toString().padStart(2, '0')}:${(minutes % 60).toString().padStart(2, '0')}`;
}