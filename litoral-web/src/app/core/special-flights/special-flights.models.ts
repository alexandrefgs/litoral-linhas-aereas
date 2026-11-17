export type SpecialFlightType = 'Charter' | 'Ferry' | 'Redelivery' | 'MaintenanceFerry';
export type SpecialFlightState = 'ComingSoon' | 'Available' | 'Taken' | 'Completed' | 'Expired' | 'Cancelled';

export interface SpecialFlightLeg {
  order: number;
  callsign: string;
  departureIcao: string;
  departureCity: string;
  arrivalIcao: string;
  arrivalCity: string;
  distanceNm: number;
  estimatedMinutes: number;
  isInternational: boolean;
  bookingStatus: string | null;
  completedAt: string | null;
}

export interface SpecialFlight {
  id: string;
  title: string;
  description: string | null;
  type: SpecialFlightType;
  difficulty: number;
  aircraftId: string;
  registration: string;
  aircraftTypeIcao: string;
  aircraftTypeName: string;
  aircraftPhotoUrl: string | null;
  minimumRankCode: string;
  minimumRankName: string;
  startsAt: string;
  endsAt: string;
  status: string;
  state: SpecialFlightState;
  pilotNumber: number | null;
  pilotDisplayId: string | null;
  pilotName: string | null;
  photoUrl: string | null;
  photoCredit: string | null;
  legs: SpecialFlightLeg[];
  createdAt: string;
}

export const SPECIAL_FLIGHT_TYPES: { value: SpecialFlightType; label: string; icon: string }[] = [
  { value: 'Charter', label: 'Charter', icon: 'ti-briefcase' },
  { value: 'Ferry', label: 'Ferry', icon: 'ti-plane-tilt' },
  { value: 'Redelivery', label: 'Redelivery', icon: 'ti-building-factory-2' },
  { value: 'MaintenanceFerry', label: 'Maintenance ferry', icon: 'ti-tool' }
];

export const SPECIAL_FLIGHT_TYPE_LABEL: Record<SpecialFlightType, string> = {
  Charter: 'Charter',
  Ferry: 'Ferry',
  Redelivery: 'Redelivery',
  MaintenanceFerry: 'Maintenance ferry'
};

export const SPECIAL_FLIGHT_STATE: Record<SpecialFlightState, { label: string; class: string }> = {
  ComingSoon: { label: 'Coming soon', class: 'bg-highlight text-on-highlight' },
  Available: { label: 'Available', class: 'bg-onda-500 text-white' },
  Taken: { label: 'Taken', class: 'bg-primary text-on-primary' },
  Completed: { label: 'Finished', class: 'bg-surface-alt text-ink' },
  Expired: { label: 'Expired', class: 'bg-danger text-white' },
  Cancelled: { label: 'Cancelled', class: 'bg-danger/80 text-white' }
};

export function specialFlightRoute(flight: SpecialFlight): string {
  if (!flight.legs.length) return '';
  return [flight.legs[0].departureIcao, ...flight.legs.map(l => l.arrivalIcao)].join(' → ');
}

export interface PilotSpecialFlight {
  flight: SpecialFlight;
  isMine: boolean;
  canTake: boolean;
  blockedReason: string | null;
  nextLegOrder: number | null;
  canBookNextLeg: boolean;
}