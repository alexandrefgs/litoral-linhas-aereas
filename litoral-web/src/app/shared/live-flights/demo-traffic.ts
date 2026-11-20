import { LiveAirport, LiveFlight } from '../../core/live/live-flights.service';
import { TrackPoint } from '../../core/pirep/pirep.models';
import { distanceNm } from '../utils/flight';

interface DemoRoute {
  callsign: string;
  pilotName: string;
  pilotId: string;
  registration: string;
  type: string;
  typeName: string;
  network: LiveFlight['network'];
  from: LiveAirport;
  to: LiveAirport;
  minutes: number;
  offset: number;
}

const airport = (icao: string, city: string, country: string, latitude: number, longitude: number): LiveAirport =>
  ({ icao, city, country, latitude, longitude });

const SBFL = airport('SBFL', 'Florianópolis', 'Brazil', -27.67, -48.55);
const SBGR = airport('SBGR', 'São Paulo', 'Brazil', -23.43, -46.47);
const SBSV = airport('SBSV', 'Salvador', 'Brazil', -12.91, -38.32);
const SBBR = airport('SBBR', 'Brasília', 'Brazil', -15.87, -47.92);
const SBEG = airport('SBEG', 'Manaus', 'Brazil', -3.04, -60.05);
const SAEZ = airport('SAEZ', 'Buenos Aires', 'Argentina', -34.82, -58.54);
const SBRF = airport('SBRF', 'Recife', 'Brazil', -8.13, -34.92);
const SBFZ = airport('SBFZ', 'Fortaleza', 'Brazil', -3.78, -38.53);

const ROUTES: DemoRoute[] = [
  { callsign: 'LTL1203', pilotName: 'Marina Costa', pilotId: 'CMD1002', registration: 'PS-LAC', type: 'B738', typeName: 'Boeing 737-800', network: 'Vatsim', from: SBFL, to: SBGR, minutes: 70, offset: 0.1 },
  { callsign: 'LTL1450', pilotName: 'Pedro Lima', pilotId: 'FO1007', registration: 'PS-LEA', type: 'B38M', typeName: 'Boeing 737 MAX 8', network: 'Ivao', from: SBGR, to: SBSV, minutes: 140, offset: 0.35 },
  { callsign: 'LTL1712', pilotName: 'Juliana Rocha', pilotId: 'CMD1011', registration: 'PS-LDA', type: 'B739', typeName: 'Boeing 737-900ER', network: 'Vatsim', from: SBBR, to: SBEG, minutes: 170, offset: 0.6 },
  { callsign: 'LTL5101', pilotName: 'Rafael Souza', pilotId: 'FO1015', registration: 'PS-LAF', type: 'B738', typeName: 'Boeing 737-800', network: 'Ivao', from: SBGR, to: SAEZ, minutes: 165, offset: 0.8 },
  { callsign: 'LTL1088', pilotName: 'Camila Nunes', pilotId: 'FO1020', registration: 'PS-LCA', type: 'B737', typeName: 'Boeing 737-700', network: 'Offline', from: SBRF, to: SBFZ, minutes: 55, offset: 0.25 }
];

function bearing(lat1: number, lon1: number, lat2: number, lon2: number): number {
  const toRad = (d: number) => (d * Math.PI) / 180;
  const y = Math.sin(toRad(lon2 - lon1)) * Math.cos(toRad(lat2));
  const x = Math.cos(toRad(lat1)) * Math.sin(toRad(lat2)) - Math.sin(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.cos(toRad(lon2 - lon1));
  return Math.round(((Math.atan2(y, x) * 180) / Math.PI + 360) % 360);
}

function progressOf(route: DemoRoute, now: number): number {
  const cycle = route.minutes * 60_000;
  return ((now % cycle) / cycle + route.offset) % 1;
}

function pointAt(route: DemoRoute, t: number): [number, number] {
  const wobble = Math.sin(t * Math.PI) * 0.6 * Math.sin(t * Math.PI * 3);
  const lat = route.from.latitude + (route.to.latitude - route.from.latitude) * t + wobble * 0.3;
  const lon = route.from.longitude + (route.to.longitude - route.from.longitude) * t + wobble * 0.3;
  return [lat, lon];
}

export function demoTraffic(now: number): LiveFlight[] {
  return ROUTES.map((r, i) => {
    const progress = progressOf(r, now);
    const [latitude, longitude] = pointAt(r, progress);
    const climb = Math.min(1, progress / 0.15);
    const descent = Math.min(1, (1 - progress) / 0.2);
    const altitude = Math.round((36000 * Math.min(climb, descent)) / 100) * 100;
    const phase = climb < 1 ? 'Climb' : descent < 1 ? (progress > 0.95 ? 'Approach' : 'Descent') : 'Cruise';
    const total = distanceNm(r.from.latitude, r.from.longitude, r.to.latitude, r.to.longitude);
    const remaining = distanceNm(latitude, longitude, r.to.latitude, r.to.longitude);
    const groundSpeed = phase === 'Cruise' ? 452 : 290;
    const [nextLat, nextLon] = pointAt(r, Math.min(1, progress + 0.01));

    return {
      id: `demo-${i}`,
      callsign: r.callsign,
      pilotId: r.pilotId,
      pilotName: r.pilotName,
      pilotPhotoUrl: null,
      registration: r.registration,
      aircraftTypeIcao: r.type,
      aircraftTypeName: r.typeName,
      photoUrl: null,
      photoCredit: null,
      isCargo: false,
      departure: r.from,
      arrival: r.to,
      network: r.network,
      simulator: 'MSFS 2024',
      latitude,
      longitude,
      altitudeFt: Math.max(1500, altitude),
      groundSpeedKt: groundSpeed,
      heading: bearing(latitude, longitude, nextLat, nextLon),
      phase,
      distanceNm: total,
      remainingNm: remaining,
      progress: Math.round(progress * 100),
      minutesToArrival: Math.round((remaining / groundSpeed) * 60),
      lastReportAt: new Date(now).toISOString()
    };
  });
}

export function demoTrack(id: string, now: number): TrackPoint[] {
  const route = ROUTES[Number(id.replace('demo-', ''))];
  if (!route) return [];

  const progress = progressOf(route, now);
  const steps = Math.max(2, Math.round(progress * 60));

  return Array.from({ length: steps + 1 }, (_, i) => {
    const [latitude, longitude] = pointAt(route, (progress * i) / steps);
    return { at: new Date(now).toISOString(), latitude, longitude, altitudeFt: 0, groundSpeedKt: 0 };
  });
}