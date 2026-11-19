export type MelKind = 'Mel' | 'Cdl';
export type MelCategory = 'A' | 'B' | 'C' | 'D';
export type MelVerification = 'Acars' | 'Safety' | 'Info';
export type MelAcarsCheck = 'None' | 'MaxAltitude' | 'RemarksContain' | 'ApuNotUsed' | 'AutobrakeOff' | 'SpeedbrakeNotArmed'
  | 'AutothrottleOff' | 'CenterTankEmpty' | 'TaxiLightOff' | 'LogoLightOff';

export interface MelItem {
  id: string;
  code: string;
  kind: MelKind;
  title: string;
  category: MelCategory;
  categoryADays: number | null;
  days: number;
  restriction: string;
  procedure: string;
  verification: MelVerification;
  acarsCheck: MelAcarsCheck;
  checkValue: number | null;
  checkText: string | null;
  fuelPenaltyPercent: number | null;
  aircraftTypes: string[];
  autoEligible: boolean;
  isActive: boolean;
  timesOpened: number;
}

export interface MelItemPayload {
  code: string;
  kind: MelKind;
  title: string;
  category: MelCategory;
  categoryADays: number | null;
  restriction: string;
  procedure: string;
  verification: MelVerification;
  acarsCheck: MelAcarsCheck;
  checkValue: number | null;
  checkText: string | null;
  fuelPenaltyPercent: number | null;
  aircraftTypes: string[];
  autoEligible: boolean;
  isActive: boolean;
}

export interface OpenMelItem {
  id: string;
  aircraftId: string;
  registration: string;
  aircraftTypeIcao: string;
  airportIcao: string;
  code: string;
  kind: MelKind;
  title: string;
  category: MelCategory;
  restriction: string;
  procedure: string;
  verification: MelVerification;
  fuelPenaltyPercent: number | null;
  source: 'System' | 'Staff';
  openedBy: string;
  note: string | null;
  openedAt: string;
  dueAt: string;
  expired: boolean;
}

export interface MelOverview {
  open: OpenMelItem[];
  aircraft: { id: string; registration: string; aircraftTypeIcao: string; airportIcao: string; status: string }[];
  aircraftTypes: string[];
}

export interface PilotMelItem {
  code: string;
  kind: MelKind;
  title: string;
  category: MelCategory;
  restriction: string;
  procedure: string;
  verification: MelVerification;
  fuelPenaltyPercent: number | null;
  dueAt: string;
}

export interface AircraftMel {
  aircraftId: string;
  items: PilotMelItem[];
}

export interface FlightMelCheck {
  id: string;
  code: string;
  title: string;
  restriction: string;
  procedure: string;
  verification: MelVerification;
  result: 'Pending' | 'Complied' | 'Violated';
  detail: string | null;
}

export const MEL_ACARS_CHECKS: { value: MelAcarsCheck; label: string; live: boolean }[] = [
  { value: 'MaxAltitude', label: 'Maximum altitude', live: true },
  { value: 'RemarksContain', label: 'Flight plan remarks contain', live: false },
  { value: 'ApuNotUsed', label: 'APU not used', live: false },
  { value: 'AutobrakeOff', label: 'Autobrake OFF', live: false },
  { value: 'SpeedbrakeNotArmed', label: 'Speedbrake not armed', live: false },
  { value: 'AutothrottleOff', label: 'Autothrottle OFF', live: false },
  { value: 'CenterTankEmpty', label: 'Center tank empty', live: false },
  { value: 'TaxiLightOff', label: 'Taxi light OFF', live: false },
  { value: 'LogoLightOff', label: 'Logo light OFF', live: false }
];

export const MEL_VERIFICATION_LABEL: Record<MelVerification, string> = {
  Acars: 'ACARS',
  Safety: 'Safety review',
  Info: 'Informative'
};

export function melLabel(item: { kind: MelKind; code: string }): string {
  return `${item.kind === 'Cdl' ? 'CDL' : 'MEL'} ${item.code}`;
}