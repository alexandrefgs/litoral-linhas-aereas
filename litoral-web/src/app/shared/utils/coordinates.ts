import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

const LATITUDE = [
  /^(\d{1,2})\s+(\d{1,2})\s+(\d{1,2}(?:[.,]\d+)?)\s*([NS])$/i,
  /^(\d{2})(\d{2})(\d{2}(?:[.,]\d+)?)\s*([NS])$/i
];
const LONGITUDE = [
  /^(\d{1,3})\s+(\d{1,2})\s+(\d{1,2}(?:[.,]\d+)?)\s*([EW])$/i,
  /^(\d{3})(\d{2})(\d{2}(?:[.,]\d+)?)\s*([EW])$/i
];

export function parseLatitude(value: string): number | null {
  return parsePart(value, LATITUDE, 90);
}

export function parseLongitude(value: string): number | null {
  return parsePart(value, LONGITUDE, 180);
}

export function formatLatitude(value: number): string {
  return toDms(value, 2, 'N', 'S');
}

export function formatLongitude(value: number): string {
  return toDms(value, 3, 'E', 'W');
}

export const latitudeValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  !control.value || parseLatitude(control.value) !== null ? null : { latitude: true };

export const longitudeValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  !control.value || parseLongitude(control.value) !== null ? null : { longitude: true };

function parsePart(value: string, patterns: RegExp[], maxDegrees: number): number | null {
  const text = (value ?? '').trim();

  for (const pattern of patterns) {
    const match = pattern.exec(text);
    if (match) return toDecimal(match[1], match[2], match[3], match[4], maxDegrees);
  }

  return null;
}

function toDecimal(deg: string, min: string, sec: string, hemisphere: string, maxDegrees: number): number | null {
  const d = Number(deg);
  const m = Number(min);
  const s = Number(sec.replace(',', '.'));

  if (m >= 60 || s >= 60) return null;

  const value = d + m / 60 + s / 3600;
  if (value > maxDegrees) return null;

  const signed = /[SW]/i.test(hemisphere) ? -value : value;
  return Math.round(signed * 1_000_000) / 1_000_000;
}

function toDms(value: number, degreeDigits: number, positive: string, negative: string): string {
  const abs = Math.abs(value);
  let deg = Math.floor(abs);
  let min = Math.floor((abs - deg) * 60);
  let sec = Math.round(((abs - deg) * 60 - min) * 60);

  if (sec === 60) {
    sec = 0;
    min++;
  }
  if (min === 60) {
    min = 0;
    deg++;
  }

  const pad = (n: number, size: number) => n.toString().padStart(size, '0');
  return `${pad(deg, degreeDigits)} ${pad(min, 2)} ${pad(sec, 2)}${value < 0 ? negative : positive}`;
}