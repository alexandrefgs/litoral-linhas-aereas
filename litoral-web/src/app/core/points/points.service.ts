import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export type PointsGroup = 'Flights' | 'Bonuses' | 'Spending' | 'Penalties' | 'Adjustments';

export interface PointTransaction {
  id: string;
  at: string;
  type: string;
  group: PointsGroup;
  description: string;
  amount: number;
  balanceAfter: number | null;
}

export interface PointsStatement {
  summary: { balance: number; monthEarned: number; monthSpent: number; totalEarned: number; totalSpent: number };
  items: PointTransaction[];
  total: number;
  page: number;
  pageSize: number;
}

export const POINT_TYPE_LABELS: Record<string, string> = {
  FlightScore: 'Flight',
  CrewDuty: 'Crew duty',
  SpecialSchedule: 'Special flight',
  MonthlyBonus: 'Monthly roster',
  PilotOfTheMonth: 'Pilot of the month',
  BookingCost: 'Custom flight',
  Jumpseat: 'Jumpseat',
  AircraftTransfer: 'Aircraft transfer',
  Penalty: 'Penalty',
  ManualAdjustment: 'Adjustment',
  TourBonus: 'Tour completed',
  EventBonus: 'Event flight'
};

export const POINT_TYPE_ICONS: Record<string, string> = {
  FlightScore: 'ti-plane-arrival',
  CrewDuty: 'ti-users-group',
  SpecialSchedule: 'ti-calendar-star',
  MonthlyBonus: 'ti-calendar-check',
  PilotOfTheMonth: 'ti-trophy',
  BookingCost: 'ti-ticket',
  Jumpseat: 'ti-armchair',
  AircraftTransfer: 'ti-truck-delivery',
  Penalty: 'ti-alert-triangle',
  ManualAdjustment: 'ti-adjustments',
  TourBonus: 'ti-route',
  EventBonus: 'ti-flag'
};

@Injectable({ providedIn: 'root' })
export class PointsService {
  private readonly http = inject(HttpClient);

  statement(group: PointsGroup | null, page: number, pageSize: number): Promise<PointsStatement> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (group) params = params.set('group', group);
    return firstValueFrom(this.http.get<PointsStatement>('/api/pilots/me/points', { params }));
  }
}