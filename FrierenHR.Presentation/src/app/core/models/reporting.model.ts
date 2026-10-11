/** Mirrors FrierenHR.Application.Common.DTOs.ReportingDtos. */

export type DayAvailability = 'Working' | 'OnLeave' | 'RestDay' | 'Holiday';

export interface AvailabilityDayDto {
  date: string;
  status: DayAvailability;
  detail: string | null;
}

export interface AvailabilityRowDto {
  employeeId: string;
  employeeName: string;
  departmentName: string | null;
  days: AvailabilityDayDto[];
}

export interface CoverageDto {
  date: string;
  departmentName: string;
  working: number;
  total: number;
  percentWorking: number;
}

export interface AvailabilityDto {
  from: string;
  to: string;
  rows: AvailabilityRowDto[];
  thinnestDays: CoverageDto[];
}

export interface LeaveProjectionDto {
  leaveType: string;
  currentBalance: number;
  monthlyAccrual: number;
  cap: number | null;
  monthsProjected: number;
  projectedBalance: number;
  forfeited: number;
  forfeitsFrom: string | null;
  note: string | null;
}

export interface LeaveForecastDto {
  employeeId: string;
  horizon: string;
  projections: LeaveProjectionDto[];
}

export interface PayrollRowDto {
  employeeId: string;
  employeeName: string;
  departmentName: string | null;
  regularHours: number;
  overtimeHours: number;
  overtimeMultiplier: number;
  payableOvertimeHours: number;
  lateMinutes: number;
  leaveDaysTaken: number;
  daysWorked: number;
  holidaysWorked: number;
}

export interface PayrollPeriodDto {
  from: string;
  to: string;
  rows: PayrollRowDto[];
}

export interface LiabilityRowDto {
  employeeId: string;
  employeeName: string;
  departmentName: string | null;
  leaveType: string;
  days: number;
  dailyRate: number;
  value: number;
}

export interface LiabilityByDepartmentDto {
  departmentName: string;
  days: number;
  value: number;
}

export interface LiabilityDto {
  dailyRate: number;
  totalDays: number;
  totalValue: number;
  rows: LiabilityRowDto[];
  byDepartment: LiabilityByDepartmentDto[];
}

/** Colour and label for an availability cell, kept next to the type it describes. */
export const AVAILABILITY_LEGEND: { value: DayAvailability; label: string }[] = [
  { value: 'Working', label: 'In' },
  { value: 'OnLeave', label: 'On leave' },
  { value: 'RestDay', label: 'Rest day' },
  { value: 'Holiday', label: 'Holiday' },
];
