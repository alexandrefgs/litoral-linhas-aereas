export type AwardKind = 'Manual' | 'Tour';

export interface PilotAward {
  awardId: string;
  name: string;
  description: string;
  kind: AwardKind;
  imageUrl: string | null;
  note: string | null;
  grantedAt: string;
}

export interface AdminAward {
  id: string;
  name: string;
  description: string;
  kind: AwardKind;
  imageUrl: string | null;
  isActive: boolean;
  holders: number;
  createdAt: string;
}

export interface AwardHolder {
  pilotId: string;
  pilotNumber: number;
  displayId: string;
  name: string;
  photoUrl: string | null;
  note: string | null;
  grantedBy: string;
  grantedAt: string;
}

export interface AwardPilotOption {
  pilotId: string;
  pilotNumber: number;
  displayId: string;
  name: string;
  photoUrl: string | null;
}

export interface AwardPayload {
  name: string;
  description: string;
  isActive: boolean;
}