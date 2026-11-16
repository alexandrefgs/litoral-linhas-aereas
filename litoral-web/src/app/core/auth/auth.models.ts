export interface PilotSummary {
  pilotNumber: number;
  displayId: string;
  firstName: string;
  lastName: string;
  email: string;
  rankCode: string;
  rankName: string;
  currentLocationIcao: string;
  totalFlightMinutes: number;
  points: number;
  simBriefUsername: string | null;
  staffPositions: string[];
  displayName: string;
  photoUrl: string | null;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  pilot: PilotSummary;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  hubIcao: string | null;
}