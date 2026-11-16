const EARTH_RADIUS_NM = 3440.065;

export const FLIGHT_BLOCK_LABELS: Record<string, string> = {
  Domestic: 'Domestic',
  SouthAmerica: 'South America',
  CentralNorthAmerica: 'Central & North America',
  Seasonal: 'Seasonal',
  EuropeAndOthers: 'Europe & others',
  FerryAndCargo: 'Ferry & cargo'
};

export function distanceNm(lat1: number, lon1: number, lat2: number, lon2: number): number {
  const toRad = (deg: number) => (deg * Math.PI) / 180;
  const dLat = toRad(lat2 - lat1);
  const dLon = toRad(lon2 - lon1);
  const a = Math.sin(dLat / 2) ** 2 + Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.sin(dLon / 2) ** 2;
  return Math.round(2 * EARTH_RADIUS_NM * Math.asin(Math.sqrt(a)));
}

export function estimateBlockMinutes(distance: number): number {
  return Math.round((distance / 430) * 60 + 25);
}

export function blockFromNumber(flightNumber: number | null | undefined): string | null {
  if (flightNumber == null) return null;
  if (flightNumber >= 1000 && flightNumber <= 4999) return 'Domestic';
  if (flightNumber >= 5000 && flightNumber <= 5999) return 'SouthAmerica';
  if (flightNumber >= 6000 && flightNumber <= 6999) return 'CentralNorthAmerica';
  if (flightNumber >= 7000 && flightNumber <= 7999) return 'Seasonal';
  if (flightNumber >= 8000 && flightNumber <= 8999) return 'EuropeAndOthers';
  if (flightNumber >= 9000 && flightNumber <= 9999) return 'FerryAndCargo';
  return null;
}

export function formatMinutes(minutes: number): string {
  return `${Math.floor(minutes / 60)}h${(minutes % 60).toString().padStart(2, '0')}`;
}