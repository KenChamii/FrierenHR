import { EmployeeRole } from './enums.model';

export interface EmployeeDto {
  id: string; companyId: string; departmentId?: string; departmentName?: string;
  managerId?: string; managerName?: string; firstName: string; lastName: string; email: string;
  hireDate: string; tenureMonths: number; role: EmployeeRole; isActive: boolean;
  /** Null when no pay has been recorded. Payslip generation skips such an employee by name. */
  monthlyRate?: number | null;
  /** Hours a full month is treated as, used to derive the hourly rate. */
  standardMonthlyHours?: number;
}
export interface CreateEmployeeDto {
  companyId: string; departmentId?: string; managerId?: string;
  firstName: string; lastName: string; email: string; password: string; hireDate: string; role: EmployeeRole;
  monthlyRate?: number | null; standardMonthlyHours?: number;
}
export interface UpdateEmployeeDto {
  departmentId?: string; managerId?: string; firstName: string; lastName: string; role: EmployeeRole; isActive: boolean;
  monthlyRate?: number | null; standardMonthlyHours?: number;
}
export interface ChangePasswordDto { currentPassword: string; newPassword: string; }
