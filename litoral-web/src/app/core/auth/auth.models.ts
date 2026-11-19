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
  twoFactorEnabled: boolean;
  twoFactorRequired: boolean;
  termsAccepted: boolean;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  pilot: PilotSummary;
  trustedDevice: string | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  session: AuthResponse | null;
  challengeToken: string | null;
}

export type LoginResult = { done: true } | { done: false; challengeToken: string };

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  hubIcao: string | null;
  acceptedTerms: boolean;
}

export interface TwoFactorStatus {
  enabled: boolean;
  required: boolean;
  recoveryCodesLeft: number;
}

export interface TwoFactorSetup {
  sharedKey: string;
  authenticatorUri: string;
}

export interface RecoveryCodes {
  recoveryCodes: string[];
}