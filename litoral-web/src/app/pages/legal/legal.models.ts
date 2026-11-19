export type LegalLanguage = 'en' | 'pt';

export interface LegalTable {
  head: string[];
  rows: string[][];
}

export interface LegalSection {
  id: string;
  title: string;
  paragraphs?: string[];
  items?: string[];
  table?: LegalTable;
  note?: string;
}

export interface LegalDocument {
  title: string;
  subtitle: string;
  updatedLabel: string;
  sections: LegalSection[];
}

export const LEGAL_UPDATED_AT = '2026-11-18';
export const LEGAL_CONTACT_EMAIL = 'contact@litoral (to be defined)';