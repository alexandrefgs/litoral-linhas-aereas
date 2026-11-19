export const STAFF_POSITION_NAMES: Record<string, string> = {
  CEO_FOUNDER: 'CEO & Founder',
  CHAIRMAN: 'Chairman',
  VICE_CHAIRMAN: 'Vice Chairman',
  GENERAL_DIRECTOR: 'General Director',
  HEAD_OF_HR: 'Head of HR',
  DIRECTOR_OF_OPERATIONS: 'Director of Operations',
  FLEET_MANAGER: 'Fleet Manager',
  ROUTE_MANAGER: 'Route Manager',
  SAFETY_MANAGER: 'Safety Manager'
};

export type StaffArea = 'routes' | 'fleet' | 'pilots' | 'staff' | 'operations' | 'pireps' | 'reports' | 'downloads';

const FULL_ACCESS = 'CEO_FOUNDER';

const STAFF_AREA_POSITIONS: Record<StaffArea, string[]> = {
  routes: ['ROUTE_MANAGER'],
  fleet: ['FLEET_MANAGER'],
  pilots: ['HEAD_OF_HR'],
  staff: ['CHAIRMAN', 'VICE_CHAIRMAN'],
  operations: ['DIRECTOR_OF_OPERATIONS'],
  pireps: ['SAFETY_MANAGER'],
  reports: ['GENERAL_DIRECTOR'],
  downloads: ['FLEET_MANAGER', 'DIRECTOR_OF_OPERATIONS']
};

export function hasStaffArea(positions: readonly string[], area: StaffArea): boolean {
  return positions.includes(FULL_ACCESS) || STAFF_AREA_POSITIONS[area].some(p => positions.includes(p));
}