export interface NameFormatOption {
  value: string;
  example: string;
}

export interface SimAircraftOption {
  icaoCode: string;
  name: string;
}

export interface PilotSettings {
  firstName: string;
  lastName: string;
  nameFormat: string;
  nameFormats: NameFormatOption[];
  email: string;
  birthDate: string | null;
  countryCode: string | null;
  state: string | null;
  city: string | null;
  vatsimId: string | null;
  ivaoId: string | null;
  discordId: string | null;
  simBriefUsername: string | null;
  simAircraft: string[];
  aircraftTypes: SimAircraftOption[];
  photoUrl: string | null;
  coverUrl: string | null;
}

export interface UpdatePersonalPayload {
  firstName: string;
  lastName: string;
  nameFormat: string;
  birthDate: string | null;
  countryCode: string | null;
  state: string | null;
  city: string | null;
  vatsimId: string | null;
  ivaoId: string | null;
  discordId: string | null;
}

export interface UpdateConfigPayload {
  simBriefUsername: string | null;
  simAircraft: string[];
}

const COUNTRY_CODES = [
  'AR', 'AW', 'BS', 'BB', 'BZ', 'BO', 'BR', 'CA', 'CL', 'CO', 'CR', 'CU', 'CW', 'DO', 'EC', 'SV', 'GF', 'GT', 'GY', 'HT',
  'HN', 'JM', 'MX', 'NI', 'PA', 'PY', 'PE', 'PR', 'SR', 'TT', 'US', 'UY', 'VE',
  'AU', 'AT', 'BE', 'CN', 'DK', 'FI', 'FR', 'DE', 'GR', 'IE', 'IL', 'IT', 'JP', 'KR', 'NL', 'NZ', 'NO', 'PL', 'PT',
  'RU', 'ZA', 'ES', 'SE', 'CH', 'TR', 'AE', 'GB'
];

const regionNames = new Intl.DisplayNames(['en'], { type: 'region' });

export const COUNTRIES = COUNTRY_CODES
  .map(code => ({ code, name: regionNames.of(code) ?? code }))
  .sort((a, b) => a.name.localeCompare(b.name));

export const PASSWORD_RULES: { label: string; test: (value: string) => boolean }[] = [
  { label: 'At least 8 characters', test: v => v.length >= 8 },
  { label: 'One lowercase letter', test: v => /[a-z]/.test(v) },
  { label: 'One uppercase letter', test: v => /[A-Z]/.test(v) },
  { label: 'One number', test: v => /\d/.test(v) }
];