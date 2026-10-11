export interface AttendanceLogDto {
  id: string; employeeId: string; employeeName: string; timeIn: string; timeOut?: string; breakMinutes: number; source: string; hoursWorked?: number;
}
export interface ClockInDto { employeeId: string; source?: string; }
export interface ClockOutDto { employeeId: string; }
export interface LogShiftDto { employeeId: string; date: string; startTime: string; endTime: string; breakMinutes: number; }

/** One day of a week being saved together. Times are "HH:mm:ss", matching TimeSpan. */
export interface LogWeekDayDto { date: string; startTime: string; endTime: string; breakMinutes: number; }

/** A week saved in one request, so it cannot be left half written. */
export interface LogWeekDto { employeeId: string; days: LogWeekDayDto[]; }
export interface OtComputationResultDto { matched: boolean; otHours: number; payableHours: number; message: string; }