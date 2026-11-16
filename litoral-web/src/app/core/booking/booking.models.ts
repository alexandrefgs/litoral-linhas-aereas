export interface BookingAirport {
  icao: string;
  name: string;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
}

export interface BookingAircraft {
  id: string;
  registration: string;
  typeId: string;
  typeIcao: string;
  typeName: string;
  variant: string | null;
  photoUrl: string | null;
  photoCredit: string | null;
  lastFlightAt: string | null;
}

export interface Crew {
  displayId: string;
  name: string;
  rankCode: string;
}

export interface Booking {
  id: string;
  callsign: string;
  networkCallsign: string;
  flightNumber: number;
  type: 'Random' | 'Custom' | 'Monthly' | 'Special';
  status: 'Booked' | 'InProgress' | 'Completed' | 'Cancelled' | 'Expired';
  departure: BookingAirport;
  arrival: BookingAirport;
  distanceNm: number;
  estimatedMinutes: number;
  isCargo: boolean;
  aircraft: BookingAircraft;
  crew: Crew | null;
  pointsCost: number;
  createdAt: string;
  expiresAt: string;
}

export interface Destination {
  routeId: string;
  flightNumber: number;
  callsign: string;
  arrival: BookingAirport;
  distanceNm: number;
  estimatedMinutes: number;
  isCargo: boolean;
  aircraftTypes: string[];
  availableAircraft: number;
  cost: number;
}

export interface BookingOverview {
  locationIcao: string;
  locationCity: string;
  baseIcao: string;
  points: number;
  customCost: number;
  current: Booking | null;
}

export interface AircraftOptions {
  routeId: string;
  callsign: string;
  validityHours: number;
  aircraft: BookingAircraft[];
}

export interface RandomProposal {
  route: Destination;
  aircraft: BookingAircraft;
}

export type FlightDuration = 'Any' | 'Short' | 'Medium' | 'Long';

export type RosterLegState = 'Completed' | 'Booked' | 'Next' | 'Locked';

export interface RosterLeg {
  id: string;
  order: number;
  sequence: number;
  routeId: string;
  callsign: string;
  departureIcao: string;
  departureCity: string;
  arrivalIcao: string;
  arrivalCity: string;
  isCargo: boolean;
  distanceNm: number;
  estimatedMinutes: number;
  state: RosterLegState;
}

export interface Roster {
  id: string;
  year: number;
  month: number;
  flightsPerDay: number;
  baseIcao: string;
  totalFlights: number;
  completedFlights: number;
  bonus: number;
  completedAt: string | null;
  legs: RosterLeg[];
}

export interface RosterStatus {
  roster: Roster | null;
  remainingDays: number;
  canCreate: boolean;
  locationIcao: string;
  hasOtherOpenBooking: boolean;
}

export interface FleetAircraft {
  id: string;
  registration: string;
  typeIcao: string;
  typeName: string;
  isCargo: boolean;
  variant: string | null;
  photoUrl: string | null;
  photoCredit: string | null;
  locationIcao: string;
  locationCity: string;
  status: 'Available' | 'Reserved' | 'InFlight' | 'Maintenance';
  totalMinutes: number;
  lastFlightAt: string | null;
}

export interface TransferQuote {
  aircraftId: string;
  registration: string;
  fromIcao: string;
  toIcao: string;
  distanceNm: number;
  cost: number;
  free: boolean;
  points: number;
  blockedReason: string | null;
}

export interface Metar {
  icao: string;
  raw: string | null;
  observedAt: string | null;
}

export interface Briefing {
  booking: Booking;
  simBriefUsername: string | null;
  simBriefDispatchUrl: string;
  metars: Metar[];
}

export interface SimBriefOfp {
  origin: string;
  destination: string;
  matchesBooking: boolean;
  route: string | null;
  cruiseAltitude: number | null;
  alternate: string | null;
  blockFuel: number | null;
  fuelUnits: string | null;
  enrouteMinutes: number | null;
  pdfUrl: string | null;
  vatsimPrefileUrl: string | null;
  ivaoPrefileUrl: string | null;
  generatedAt: string | null;
}