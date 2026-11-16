export type ScoringOutcome = 'Points' | 'Reject';
export type ScoringMode = 'Once' | 'PerOccurrence' | 'PerSecond' | 'PerDay';

export interface ScoringRule {
  id: string;
  category: string;
  check: string;
  description: string;
  outcome: ScoringOutcome;
  points: number;
  mode: ScoringMode;
  min: number | null;
  max: number | null;
  text: string | null;
  aircraftTypes: string | null;
  graceSeconds: number;
  order: number;
  isActive: boolean;
}

export interface ScoringSection {
  category: string;
  title: string;
  subtitle: string | null;
  rules: ScoringRule[];
}

export interface ScoringCategoryInfo {
  category: string;
  title: string;
  subtitle: string | null;
}

export interface ScoringCheckInfo {
  check: string;
  label: string;
  source: 'Acars' | 'Review' | 'Flight' | 'Pilot';
  modes: ScoringMode[];
  minLabel: string | null;
  maxLabel: string | null;
  textLabel: string | null;
}

export interface ScoringCatalog {
  categories: ScoringCategoryInfo[];
  checks: ScoringCheckInfo[];
}

export type ScoringRulePayload = Omit<ScoringRule, 'id'>;

const MODE_SUFFIX: Record<ScoringMode, string> = {
  Once: '',
  PerOccurrence: ' each',
  PerSecond: '/s',
  PerDay: '/day'
};

export function ruleValue(rule: Pick<ScoringRule, 'outcome' | 'points' | 'mode'>): string {
  if (rule.outcome === 'Reject') return 'Rejection';
  const sign = rule.points > 0 ? '+' : rule.points < 0 ? '−' : '';
  return `${sign}${Math.abs(rule.points)} pts${MODE_SUFFIX[rule.mode]}`;
}

export function ruleTone(rule: Pick<ScoringRule, 'outcome' | 'points'>): 'reject' | 'bonus' | 'penalty' | 'neutral' {
  if (rule.outcome === 'Reject') return 'reject';
  if (rule.points > 0) return 'bonus';
  if (rule.points < 0) return 'penalty';
  return 'neutral';
}

export const RULE_TONE_CLASS = {
  reject: 'text-danger',
  penalty: 'text-danger',
  bonus: 'text-onda-500',
  neutral: 'text-primary'
} as const;

export const RULE_TONE_ICON = {
  reject: 'ti-circle-x',
  penalty: 'ti-circle-minus',
  bonus: 'ti-circle-plus',
  neutral: 'ti-circle-plus'
} as const;