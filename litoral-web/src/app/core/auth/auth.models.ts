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

export interface ApplicationStatus {
  firstName: string;
  submittedAt: string;
}

export interface LoginResponse {
  session: AuthResponse | null;
  challengeToken: string | null;
  application: ApplicationStatus | null;
}

export interface RegisterResponse {
  session: AuthResponse | null;
  application: ApplicationStatus | null;
}

export type LoginResult =
  | { kind: 'session' }
  | { kind: 'two-factor'; challengeToken: string }
  | { kind: 'application'; application: ApplicationStatus };

export type RegisterResult = { kind: 'session' } | { kind: 'application'; application: ApplicationStatus };

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  hubIcao: string | null;
  acceptedTerms: boolean;
  vatsimId: string | null;
  ivaoId: string | null;
  simulator: string;
  addOns: string[];
  motivation: string;
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