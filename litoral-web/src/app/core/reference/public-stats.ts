export interface PublicStats {
  activePilots: number;
  completedFlights: number;
  aircraftTypes: number;
  destinations: number;
  hubs: number;
}

export const EMPTY_STATS: PublicStats = {
  activePilots: 0,
  completedFlights: 0,
  aircraftTypes: 0,
  destinations: 0,
  hubs: 0
};