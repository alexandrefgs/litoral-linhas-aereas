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
}