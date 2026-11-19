export interface Hub {
  icao: string;
  name: string;
  city: string;
  passenger: boolean;
  cargo: boolean;
}

export interface RankInfo {
  code: string;
  name: string;
  level: number;
  minFlightHours: number;
  minPoints: number;
}

export interface Airport {
  icao: string;
  name: string;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
  isHub: boolean;
  aircraftOnGround: number;
  departures: number;
}

export interface PublicStaffMember {
  pilotNumber: number;
  displayId: string;
  name: string;
  photoUrl: string | null;
  rank: string;
}

export interface PublicStaffPosition {
  code: string;
  name: string;
  team: 'Executive' | 'Operations' | 'Safety';
  holders: PublicStaffMember[];
}

export interface CareerRank {
  code: string;
  name: string;
  level: number;
  minFlightHours: number;
  minPoints: number;
  pilots: number;
  airports: number;
}