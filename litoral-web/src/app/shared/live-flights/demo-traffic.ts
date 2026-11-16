import { LiveFlight } from '../../core/live/live-flights.service';

interface DemoRoute {
  callsign: string;
  pilotName: string;
  pilotId: string;
  registration: string;
  type: string;
  network: LiveFlight['network'];
  from: [string, number, number];
  to: [string, number, number];
  minutes: number;
  offset: number;
}

const ROUTES: DemoRoute[] = [
  { callsign: 'LTL1203', pilotName: 'Marina Costa', pilotId: 'CMD1002', registration: 'PS-LAC', type: 'B738', network: 'Vatsim', from: ['SBFL', -27.67, -48.55], to: ['SBGR', -23.43, -46.47], minutes: 70, offset: 0.1 },
  { callsign: 'LTL1450', pilotName: 'Pedro Lima', pilotId: 'FO1007', registration: 'PS-LEA', type: 'B38M', network: 'Ivao', from: ['SBGR', -23.43, -46.47], to: ['SBSV', -12.91, -38.32], minutes: 140, offset: 0.35 },
  { callsign: 'LTL1712', pilotName: 'Juliana Rocha', pilotId: 'CMD1011', registration: 'PS-LDA', type: 'B739', network: 'Vatsim', from: ['SBBR', -15.87, -47.92], to: ['SBEG', -3.04, -60.05], minutes: 170, offset: 0.6 },
  { callsign: 'LTL5101', pilotName: 'Rafael Souza', pilotId: 'FO1015', registration: 'PS-LAF', type: 'B738', network: 'Ivao', from: ['SBGR', -23.43, -46.47], to: ['SAEZ', -34.82, -58.54], minutes: 165, offset: 0.8 },
  { callsign: 'LTL1088', pilotName: 'Camila Nunes', pilotId: 'FO1020', registration: 'PS-LCA', type: 'B737', network: 'Vatsim', from: ['SBRF', -8.13, -34.92], to: ['SBFZ', -3.78, -38.53], minutes: 55, offset: 0.25 }
];

function bearing(lat1: number, lon1: number, lat2: number, lon2: number): number {
  const toRad = (d: number) => (d * Math.PI) / 180;
  const y = Math.sin(toRad(lon2 - lon1)) * Math.cos(toRad(lat2));
  const x = Math.cos(toRad(lat1)) * Math.sin(toRad(lat2)) - Math.sin(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.cos(toRad(lon2 - lon1));
  return Math.round(((Math.atan2(y, x) * 180) / Math.PI + 360) % 360);
}

export function demoTraffic(now: number): LiveFlight[] {
  return ROUTES.map((r, i) => {
    const cycle = r.minutes * 60_000;
    const progress = ((now % cycle) / cycle + r.offset) % 1;
    const [, lat1, lon1] = r.from;
    const [, lat2, lon2] = r.to;
    const climb = Math.min(1, progress / 0.15);
    const descent = Math.min(1, (1 - progress) / 0.2);
    const altitude = Math.round((36000 * Math.min(climb, descent)) / 100) * 100;
    const phase = climb < 1 ? 'Climb' : descent < 1 ? 'Descent' : 'Cruise';

    return {
      id: `demo-${i}`,
      callsign: r.callsign,
      pilotId: r.pilotId,
      pilotName: r.pilotName,
      registration: r.registration,
      aircraftTypeIcao: r.type,
      departureIcao: r.from[0],
      arrivalIcao: r.to[0],
      network: r.network,
      latitude: lat1 + (lat2 - lat1) * progress,
      longitude: lon1 + (lon2 - lon1) * progress,
      altitudeFt: Math.max(1500, altitude),
      groundSpeedKt: phase === 'Cruise' ? 450 : 290,
      heading: bearing(lat1, lon1, lat2, lon2),
      phase,
      lastReportAt: new Date(now).toISOString()
    };
  });
}