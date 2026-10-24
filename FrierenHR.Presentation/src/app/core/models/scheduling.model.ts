// Mirrors FrierenHR.Application.Common.DTOs.SchedulingDtos.

export type HolidayType = 'Regular' | 'SpecialNonWorking' | 'CompanySpecific';
export type AuditAction = 'Created' | 'Updated' | 'Deleted';

export const HOLIDAY_TYPES: { label: string; value: HolidayType }[] = [
  { label: 'Regular holiday', value: 'Regular' },
  { label: 'Special non-working', value: 'SpecialNonWorking' },
  { label: 'Company-specific', value: 'CompanySpecific' },
];

/** Matches the [Flags] WeekDays enum, so the API sends and receives a single number. */
export const WEEK_DAYS: { label: string; short: string; value: number }[] = [
  { label: 'Monday', short: 'Mon', value: 1 },
  { label: 'Tuesday', short: 'Tue', value: 2 },
  { label: 'Wednesday', short: 'Wed', value: 4 },
  { label: 'Thursday', short: 'Thu', value: 8 },
  { label: 'Friday', short: 'Fri', value: 16 },
  { label: 'Saturday', short: 'Sat', value: 32 },
  { label: 'Sunday', short: 'Sun', value: 64 },
];

export function weekDaysToList(flags: number): string[] {
  return WEEK_DAYS.filter(d => (flags & d.value) !== 0).map(d => d.short);
}

export function toggleWeekDay(flags: number, day: number): number {
  return (flags & day) !== 0 ? flags & ~day : flags | day;
}

// --- Holidays ---

export interface HolidayDto {
  id: string;
  companyId: string;
  date: string;
  name: string;
  type: HolidayType;
  departmentId?: string | null;
  departmentName?: string | null;
}

export interface SaveHolidayDto {
  id?: string;
  companyId: string;
  date: string;
  name: string;
  type: HolidayType;
  departmentId?: string | null;
}

export interface HolidayImportResultDto {
  added: number;
  skipped: number;
  messages: string[];
}

// --- Shifts ---

export interface ShiftDto {
  id: string;
  companyId: string;
  name: string;
  startTime: string;
  endTime: string;
  breakMinutes: number;
  restDays: number;
  isDefault: boolean;
  crossesMidnight: boolean;
  assignedEmployeeCount: number;
}

export interface SaveShiftDto {
  id?: string;
  companyId: string;
  name: string;
  startTime: string;
  endTime: string;
  breakMinutes: number;
  restDays: number;
  isDefault: boolean;
}

export interface ResolvedShiftDto { shift: ShiftDto | null; source: string; }

// --- Working days ---

/** A holiday fetched for review. Nothing is saved until the list is imported. */
export interface HolidaySuggestionDto {
  date: string;
  name: string;
  suggestedType: HolidayType;
  /** The company already has a company-wide holiday on this date. */
  alreadyRecorded: boolean;
}

export interface HolidaySuggestionsDto {
  year: number;
  countryCode: string;
  source: string;
  /** Why the list still needs reading before it is saved. */
  caveat: string;
  holidays: HolidaySuggestionDto[];
}

export interface NonWorkingDayDto { date: string; reason: string; }

export interface WorkingDaysDto {
  from: string;
  to: string;
  calendarDays: number;
  workingDays: number;
  nonWorkingDays: NonWorkingDayDto[];
}

// --- Attendance corrections ---

export type CorrectionStatus = 'Pending' | 'Approved' | 'Rejected' | 'Escalated';

export interface AttendanceCorrectionDto {
  id: string;
  employeeId: string;
  employeeName: string;
  date: string;
  requestedTimeIn: string;
  requestedTimeOut: string;
  requestedBreakMinutes: number;
  reason: string;
  status: CorrectionStatus;
  requestedAt: string;
  decidedByName?: string | null;
  decidedAt?: string | null;
  decisionComment?: string | null;
}

export interface CreateAttendanceCorrectionDto {
  employeeId: string;
  date: string;
  requestedTimeIn: string;
  requestedTimeOut: string;
  requestedBreakMinutes: number;
  reason: string;
}

export interface DecideAttendanceCorrectionDto {
  decidedByEmployeeId: string;
  approve: boolean;
  comment?: string | null;
}

// --- Audit trail ---

export interface AuditEntryDto {
  id: string;
  entityName: string;
  entityId: string;
  action: AuditAction;
  changedByEmployeeId?: string | null;
  changedByName?: string | null;
  changedAt: string;
  /** JSON document, shape differs per entity. Parsed for display, never queried. */
  changes: string;
}

export interface AuditPageDto {
  entries: AuditEntryDto[];
  total: number;
  skip: number;
  take: number;
}
