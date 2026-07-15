export interface AttendanceLogDto {
  id: string; employeeId: string; employeeName: string; timeIn: string; timeOut?: string; source: string; hoursWorked?: number;
}
export interface ClockInDto { employeeId: string; source?: string; }
export interface ClockOutDto { employeeId: string; }
export interface OtComputationResultDto { matched: boolean; otHours: number; payableHours: number; message: string; }