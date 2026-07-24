export type TimesheetStatus = 'Draft' | 'Submitted' | 'Approved' | 'Rejected';

export interface TimesheetDto {
  id: string; employeeId: string; employeeName: string; weekStartDate: string;
  status: TimesheetStatus; totalHours: number; submittedAt?: string;
  decidedAt?: string; decidedByName?: string; comment?: string;
}

export interface SubmitTimesheetDto { employeeId: string; weekStartDate: string; }
export interface DecideTimesheetDto { decidedByEmployeeId: string; approve: boolean; comment?: string; }
