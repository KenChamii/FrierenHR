export type EmployeeRole = 'Employee' | 'Manager' | 'HRAdmin';
export type LeaveStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled' | 'Escalated';
export type LeaveType = 'Vacation' | 'Sick' | 'Emergency' | 'Unpaid' | 'Maternity' | 'Paternity';
export type RuleType = 'LeaveAccrual' | 'LeaveApproval' | 'OTMultiplier' | 'LateGracePeriod' | 'ApprovalEscalation';
export type ConversationType = 'Direct' | 'Group' | 'Broadcast';
export type ApprovalStatus = 'Pending' | 'Approved' | 'Rejected' | 'Escalated';

export const LEAVE_TYPES: LeaveType[] = ['Vacation', 'Sick', 'Emergency', 'Unpaid', 'Maternity', 'Paternity'];
export const RULE_TYPES: RuleType[] = ['LeaveAccrual', 'LeaveApproval', 'OTMultiplier', 'LateGracePeriod', 'ApprovalEscalation'];
export const EMPLOYEE_ROLES: EmployeeRole[] = ['Employee', 'Manager', 'HRAdmin'];
export const RULE_OPERATORS = ['>=', '<=', '==', '!=', 'in'] as const;