export interface RankedPilot {
  position: number;
  pilotNumber: number;
  displayId: string;
  name: string;
  rankCode: string;
  photoUrl: string | null;
  value: number;
  isMe: boolean;
}

export interface CountItem {
  key: string;
  label: string;
  flights: number;
  minutes: number;
}

export interface PeriodPoint {
  label: string;
  start: string;
  flights: number;
  minutes: number;
}

export interface AwardWinner {
  category: 'Hours' | 'Flights';
  position: number;
  pilotNumber: number;
  name: string;
  photoUrl: string | null;
  value: number;
  points: number;
}

export interface MonthAwards {
  year: number;
  month: number;
  winners: AwardWinner[];
}

export interface Statistics {
  totals: { pilots: number; flights: number; minutes: number; distanceNm: number; averageLandingRate: number | null; aircraft: number; airports: number };
  month: { year: number; month: number; flights: number; minutes: number; activePilots: number };
  monthly: PeriodPoint[];
  monthHours: RankedPilot[];
  monthFlights: RankedPilot[];
  lastAwards: MonthAwards | null;
  bonus: number[];
  topHours: RankedPilot[];
  topFlights: RankedPilot[];
  topPoints: RankedPilot[];
  topRoutes: CountItem[];
  topAirports: CountItem[];
  fleet: CountItem[];
  networks: CountItem[];
  me: {
    flights: number;
    minutes: number;
    points: number;
    averageLandingRate: number | null;
    monthFlights: number;
    monthMinutes: number;
    hoursPosition: number | null;
    flightsPosition: number | null;
    pointsPosition: number | null;
    monthHoursPosition: number | null;
    monthFlightsPosition: number | null;
  };
}

export interface Report {
  from: string;
  to: string;
  flights: { flights: number; minutes: number; distanceNm: number; averageLandingRate: number | null; activePilots: number };
  pireps: {
    pendingNow: number;
    oldestPendingHours: number | null;
    approved: number;
    rejected: number;
    rejectionPercent: number;
    averageConformity: number | null;
    averageReviewHours: number | null;
    manual: number;
  };
  pilots: { total: number; active: number; inactive: number; newSignups: number; idle30Days: number; neverFlown: number };
  timeline: PeriodPoint[];
  topPilots: RankedPilot[];
  networks: CountItem[];
  bookingTypes: CountItem[];
  topRoutes: CountItem[];
}