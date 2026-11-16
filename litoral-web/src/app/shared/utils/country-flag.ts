const COUNTRY_CODES: Record<string, string> = {
  argentina: 'ar', aruba: 'aw', bahamas: 'bs', barbados: 'bb', bolivia: 'bo', brasil: 'br', brazil: 'br',
  canada: 'ca', chile: 'cl', colombia: 'co', 'costa rica': 'cr', cuba: 'cu', curacao: 'cw', 'curaçao': 'cw',
  'dominican republic': 'do', 'república dominicana': 'do', ecuador: 'ec', 'el salvador': 'sv', france: 'fr',
  'french guiana': 'gf', germany: 'de', guatemala: 'gt', guyana: 'gy', honduras: 'hn', italy: 'it', jamaica: 'jm',
  mexico: 'mx', 'méxico': 'mx', netherlands: 'nl', nicaragua: 'ni', panama: 'pa', 'panamá': 'pa', paraguay: 'py',
  peru: 'pe', 'perú': 'pe', portugal: 'pt', 'puerto rico': 'pr', spain: 'es', suriname: 'sr',
  'trinidad and tobago': 'tt', 'united kingdom': 'gb', 'united states': 'us', 'estados unidos': 'us', usa: 'us',
  uruguay: 'uy', venezuela: 've'
};

export function countryFlagUrl(country: string | null | undefined): string | null {
  const code = COUNTRY_CODES[(country ?? '').trim().toLowerCase()];
  return code ? `https://flagcdn.com/w40/${code}.png` : null;
}