import { LeaveType, LeaveStatus } from './enums.model';

export interface LeaveRequestDto {
  id: string; employeeId: string; employeeName: string;
  leaveType: LeaveType; startDate: string; endDate: string; days: number;
  reason?: string; status: LeaveStatus;
  requiresApproval: boolean; requestedAt: string; decidedAt?: string;
}
export interface CreateLeaveRequestDto { employeeId: string; leaveType: LeaveType; startDate: string; endDate: string; reason?: string; }
export interface DecideLeaveRequestDto { decidedByEmployeeId: string; approve: boolean; comment?: string; }
export interface LeaveBalanceDto { leaveType: LeaveType; balance: number; lastAccrualDate?: string; }