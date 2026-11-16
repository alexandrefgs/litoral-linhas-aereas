export interface ProfileAircraft {
  icaoCode: string;
  name: string;
  fleetCount: number;
}

export interface ProfilePrivate {
  email: string;
  birthDate: string | null;
  discordId: string | null;
}

export interface ActivityMonth {
  month: number;
  flights: number;
  minutes: number;
}

export interface ActivityYear {
  year: number;
  flights: number;
  minutes: number;
  months: ActivityMonth[];
}

export interface ProfileAirport {
  icao: string;
  name: string;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
}

export interface TopRoute {
  departure: ProfileAirport;
  arrival: ProfileAirport;
  flights: number;
}

export interface PilotProfile {
  pilotNumber: number;
  displayId: string;
  firstName: string;
  lastName: string;
  displayName: string;
  rankCode: string;
  rankName: string;
  isActive: boolean;
  baseIcao: string;
  currentLocationIcao: string;
  countryCode: string | null;
  state: string | null;
  city: string | null;
  vatsimId: string | null;
  ivaoId: string | null;
  age: number | null;
  joinedAt: string;
  totalFlightMinutes: number;
  totalFlights: number;
  points: number;
  isRejected: boolean;
  photoUrl: string | null;
  coverUrl: string | null;
  isOwner: boolean;
  private: ProfilePrivate | null;
  simAircraft: ProfileAircraft[];
  activity: ActivityYear[];
  topRoutes: TopRoute[];
}

export interface FlightMapRoute {
  departureIcao: string;
  arrivalIcao: string;
  flights: number;
}

export interface FlightMap {
  airports: ProfileAirport[];
  routes: FlightMapRoute[];
}

export interface ProfileFlight {
  id: string;
  callsign: string;
  departureIcao: string;
  arrivalIcao: string;
  aircraftTypeIcao: string;
  registration: string;
  blockMinutes: number;
  points: number;
  isRejected: boolean;
  date: string;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ProfileFlightsQuery {
  page: number;
  pageSize: number;
  search: string;
  sort: 'date' | 'time';
}

export const DEFAULT_COVER_URL = '/profile/cover-default.jpg';

export function countryFlagByCode(code: string | null | undefined): string | null {
  return code && /^[A-Za-z]{2}$/.test(code) ? `https://flagcdn.com/w40/${code.toLowerCase()}.png` : null;
}

export function timeSince(iso: string, now = new Date()): string {
  const start = new Date(iso);
  let years = now.getFullYear() - start.getFullYear();
  let months = now.getMonth() - start.getMonth();
  let days = now.getDate() - start.getDate();

  if (days < 0) {
    months--;
    days += new Date(now.getFullYear(), now.getMonth(), 0).getDate();
  }
  if (months < 0) {
    years--;
    months += 12;
  }

  const parts: string[] = [];
  if (years > 0) parts.push(`${years} ${years === 1 ? 'year' : 'years'}`);
  if (months > 0) parts.push(`${months} ${months === 1 ? 'month' : 'months'}`);
  if (days > 0 || parts.length === 0) parts.push(`${days} ${days === 1 ? 'day' : 'days'}`);
  return parts.join(', ');
}

export function ordinal(position: number): string {
  return `${position}º`;
}