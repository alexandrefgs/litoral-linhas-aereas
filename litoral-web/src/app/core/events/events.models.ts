export type EventState = 'Draft' | 'Upcoming' | 'Live' | 'Ended';

export interface PilotEvent {
  id: string;
  title: string;
  description: string;
  imageUrl: string | null;
  startsAt: string;
  endsAt: string;
  state: EventState;
  airports: string[];
  bonusPoints: number;
  flights: number;
  pilots: number;
  myFlights: number;
}

export interface AdminEvent {
  id: string;
  title: string;
  description: string;
  imageUrl: string | null;
  startsAt: string;
  endsAt: string;
  state: EventState;
  isPublished: boolean;
  airports: string[];
  bonusPoints: number;
  flights: number;
  pilots: number;
}

export interface EventPayload {
  title: string;
  description: string;
  startsAt: string;
  endsAt: string;
  airports: string[];
  bonusPoints: number;
  isPublished: boolean;
}

export const EVENT_STATE: Record<EventState, { label: string; class: string }> = {
  Draft: { label: 'Draft', class: 'bg-surface-alt text-ink-muted' },
  Upcoming: { label: 'Upcoming', class: 'bg-highlight text-on-highlight' },
  Live: { label: 'Live now', class: 'bg-danger text-white' },
  Ended: { label: 'Ended', class: 'bg-ink-muted text-white' }
};