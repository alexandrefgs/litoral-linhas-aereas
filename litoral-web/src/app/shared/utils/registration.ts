const RESERVED = ['SOS', 'XXX', 'PAN', 'TTT', 'VFR', 'IFR', 'VMC', 'IMC'];

export function prohibitedReason(marks: string): string | null {
  if (marks.startsWith('Q')) return 'combinations starting with Q are not allowed';
  if (marks[1] === 'W') return 'W cannot be the second letter';
  if (RESERVED.includes(marks)) return `${marks} is a reserved combination`;
  return null;
}