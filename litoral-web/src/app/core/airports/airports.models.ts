export interface AirportPilot {
  pilotNumber: number;
  displayId: string;
  name: string;
  firstName: string;
  lastName: string;
  photoUrl: string | null;
}

export interface DirectoryAirport {
  icao: string;
  iata: string | null;
  name: string;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
  photoUrl: string | null;
  isBase: boolean;
  passenger: boolean;
  cargo: boolean;
  minimumRankLevel: number;
  minimumRankName: string;
  minimumRankCode: string;
  aircraftOnGround: number;
  departures: number;
  jumpseatCost: number;
  pilotCount: number;
  pilots: AirportPilot[];
}

export interface AirportDirectory {
  currentIcao: string;
  points: number;
  rankName: string;
  jumpseatBlockedReason: string | null;
  airports: DirectoryAirport[];
}

export interface AirportAircraft {
  registration: string;
  typeIcao: string;
  typeName: string;
  photoUrl: string | null;
  status: string;
  lastCallsign: string | null;
  lastFlightAt: string | null;
}

export interface AirportDeparture {
  routeId: string;
  callsign: string;
  arrivalIcao: string;
  arrivalIata: string | null;
  arrivalName: string;
  arrivalCity: string;
  arrivalCountry: string;
  isCargo: boolean;
  distanceNm: number;
  estimatedMinutes: number;
  aircraftTypes: string[];
  hasAircraft: boolean;
}

export interface JumpseatResult {
  fromIcao: string;
  toIcao: string;
  cost: number;
  points: number;
}

export type AirportFilter = 'all' | 'bases' | 'passenger' | 'cargo';

export const AIRCRAFT_STATUS_CLASS: Record<string, string> = {
  Available: 'bg-onda-500/15 text-onda-500',
  Reserved: 'bg-highlight/20 text-ink',
  Maintenance: 'bg-danger/10 text-danger'
};