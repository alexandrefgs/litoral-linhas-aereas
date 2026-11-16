export function altitudeFt(ft: number): string {
  return `${ft.toLocaleString('en-US')} ft`;
}

export function flightLevel(ft: number): string {
  return ft >= 10000 ? `FL${Math.round(ft / 100).toString().padStart(3, '0')}` : altitudeFt(ft);
}

export function minutesLabel(minutes: number | null): string {
  if (minutes === null) return '—';
  return `${Math.floor(minutes / 60)}:${(minutes % 60).toString().padStart(2, '0')}`;
}

export function initials(name: string): string {
  return name.split(/\s+/).filter(Boolean).slice(0, 2).map(p => p[0]!.toUpperCase()).join('');
}

export function unwrapLongitudes(points: [number, number][]): [number, number][] {
  const result: [number, number][] = [];

  for (const [lat, lon] of points) {
    const previous = result[result.length - 1];
    if (!previous) {
      result.push([lat, lon]);
      continue;
    }

    let adjusted = lon;
    while (adjusted - previous[1] > 180) adjusted -= 360;
    while (adjusted - previous[1] < -180) adjusted += 360;
    result.push([lat, adjusted]);
  }

  return result;
}